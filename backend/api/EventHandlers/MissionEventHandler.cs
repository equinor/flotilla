﻿using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.Events;
using Api.Utilities;
namespace Api.EventHandlers
{

    public class MissionEventHandler : EventHandlerBase
    {
        private readonly ILogger<MissionEventHandler> _logger;

        // The mutex is used to ensure multiple missions aren't attempted scheduled simultaneously whenever multiple mission runs are created
        private readonly Semaphore _scheduleMissionSemaphore = new(1, 1);
        private readonly Semaphore _scheduleLocalizationSemaphore = new(1, 1);

        private readonly IServiceScopeFactory _scopeFactory;

        public MissionEventHandler(
            ILogger<MissionEventHandler> logger,
            IServiceScopeFactory scopeFactory
        )
        {
            _logger = logger;
            _scopeFactory = scopeFactory;

            Subscribe();
        }
        private IMissionRunService MissionService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionRunService>();

        private IRobotService RobotService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IRobotService>();

        private IReturnToHomeService ReturnToHomeService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IReturnToHomeService>();

        private ILocalizationService LocalizationService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILocalizationService>();

        private IAreaService AreaService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IAreaService>();

        private IMissionSchedulingService MissionScheduling => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionSchedulingService>();

        private ISignalRService SignalRService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ISignalRService>();

        public override void Subscribe()
        {
            MissionRunService.MissionRunCreated += OnMissionRunCreated;
            MissionSchedulingService.RobotAvailable += OnRobotAvailable;
            EmergencyActionService.EmergencyButtonPressedForRobot += OnEmergencyButtonPressedForRobot;
            EmergencyActionService.EmergencyButtonDepressedForRobot += OnEmergencyButtonDepressedForRobot;
        }

        public override void Unsubscribe()
        {
            MissionRunService.MissionRunCreated -= OnMissionRunCreated;
            MissionSchedulingService.RobotAvailable -= OnRobotAvailable;
            EmergencyActionService.EmergencyButtonPressedForRobot -= OnEmergencyButtonPressedForRobot;
            EmergencyActionService.EmergencyButtonDepressedForRobot -= OnEmergencyButtonDepressedForRobot;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await stoppingToken;
        }

        private async void OnMissionRunCreated(object? sender, MissionRunCreatedEventArgs e)
        {
            _logger.LogInformation("Triggered MissionRunCreated event for mission run ID: {MissionRunId}", e.MissionRunId);

            var missionRun = await MissionService.ReadById(e.MissionRunId);
            if (missionRun == null)
            {
                _logger.LogError("Mission run with ID: {MissionRunId} was not found in the database", e.MissionRunId);
                return;
            }


            _scheduleLocalizationSemaphore.WaitOne();

            string? localizationMissionRunId = null;
            try { localizationMissionRunId = await LocalizationService.CreateLocalizationMissionIfRobotIsNotLocalized(missionRun.Robot, missionRun); }
            catch (Exception ex) when (
                ex is AreaNotFoundException
                or DeckNotFoundException
                or RobotNotAvailableException
                or RobotLocalizationException
                or RobotNotFoundException
                or IsarCommunicationException
            )
            {
                _logger.LogError("Mission run {MissionRunId} will be cancelled as robot {RobotId} was not correctly localized", missionRun.Id, missionRun.Robot.Id);
                missionRun.Status = MissionStatus.Cancelled;
                await MissionService.Update(missionRun);
                return;
            }
            finally { _scheduleLocalizationSemaphore.Release(); }

            string missionRunIdToStart = missionRun.Id;
            if (localizationMissionRunId is not null) missionRunIdToStart = localizationMissionRunId;

            if (MissionScheduling.MissionRunQueueIsEmpty(await MissionService.ReadMissionRunQueue(missionRun.Robot.Id)))
            {
                _logger.LogInformation("Mission run {MissionRunId} was not started as there are no mission runs on the queue", e.MissionRunId);
                return;
            }

            _scheduleMissionSemaphore.WaitOne();
            try { await MissionScheduling.StartMissionRunIfSystemIsAvailable(missionRunIdToStart); }
            catch (MissionRunNotFoundException) { }
            finally { _scheduleMissionSemaphore.Release(); }
        }

        private async void OnRobotAvailable(object? sender, RobotAvailableEventArgs e)
        {
            _logger.LogInformation("Triggered RobotAvailable event for robot ID: {RobotId}", e.RobotId);
            var robot = await RobotService.ReadById(e.RobotId);
            if (robot == null)
            {
                _logger.LogError("Robot with ID: {RobotId} was not found in the database", e.RobotId);
                return;
            }

            try { await LocalizationService.EnsureRobotWasCorrectlyLocalizedInPreviousMissionRun(robot.Id); }
            catch (Exception ex) when (ex is LocalizationFailedException or RobotNotFoundException or MissionNotFoundException or OngoingMissionNotLocalizationException or TimeoutException)
            {
                _logger.LogError("Could not confirm that the robot {RobotId} was correctly localized and the scheduled missions for the deck will be cancelled", robot.Id);
                try { await MissionScheduling.CancelAllScheduledMissions(robot.Id); }
                catch (RobotNotFoundException) { _logger.LogError("Failed to cancel scheduled missions for robot {RobotId}", robot.Id); }
                return;
            }

            if (MissionScheduling.MissionRunQueueIsEmpty(await MissionService.ReadMissionRunQueue(robot.Id)))
            {
                _logger.LogInformation("The robot was changed to available but there are no mission runs in the queue to be scheduled");

                if (await ReturnToHomeService.IsRobotHome(robot.Id))
                {
                    await RobotService.UpdateCurrentArea(robot.Id, null);
                    return;
                }

                try { await ReturnToHomeService.ScheduleReturnToHomeMissionRun(robot.Id); }
                catch (Exception ex) when (ex is RobotNotFoundException or AreaNotFoundException or DeckNotFoundException or PoseNotFoundException)
                {
                    var installation = robot.CurrentInstallation;
                    if (installation != null)
                        _ = SignalRService.SendMessageAsync(
                            "Alert",
                            installation,
                            new AlertResponse("safezoneFailure", "Safezone failure", $"Failed to send {robot.Name} to a safezone", installation.InstallationCode, robot.Id));
                    await RobotService.UpdateCurrentArea(robot.Id, null);
                    return;
                }
                return;
            }

            MissionRun? missionRun;
            if (robot.MissionQueueFrozen) { missionRun = await MissionService.ReadNextScheduledEmergencyMissionRun(robot.Id); }
            else { missionRun = await MissionService.ReadNextScheduledMissionRun(robot.Id); }

            if (missionRun == null)
            {
                _logger.LogInformation("The robot was changed to available but no mission is scheduled");
                return;
            }

            _scheduleMissionSemaphore.WaitOne();
            try { await MissionScheduling.StartMissionRunIfSystemIsAvailable(missionRun.Id); }
            catch (MissionRunNotFoundException) { return; }
            _scheduleMissionSemaphore.Release();
        }

        private async void OnEmergencyButtonPressedForRobot(object? sender, EmergencyButtonPressedForRobotEventArgs e)
        {
            _logger.LogInformation("Triggered EmergencyButtonPressed event for robot ID: {RobotId}", e.RobotId);
            var robot = await RobotService.ReadById(e.RobotId);
            if (robot == null)
            {
                _logger.LogError("Robot with ID: {RobotId} was not found in the database", e.RobotId);
                return;
            }

            var area = await AreaService.ReadById(robot.CurrentArea!.Id);
            if (area == null)
            {
                _logger.LogError("Could not find area with ID {AreaId}", robot.CurrentArea!.Id);
                return;
            }

            try { await MissionScheduling.FreezeMissionRunQueueForRobot(e.RobotId); }
            catch (RobotNotFoundException) { return; }

            try { await MissionScheduling.ScheduleMissionToReturnToSafePosition(e.RobotId, area.Id); }
            catch (SafeZoneException ex)
            {
                _logger.LogError(ex, "Failed to schedule return to safe zone mission on robot {RobotName} because: {ErrorMessage}", robot.Name, ex.Message);
                try { await MissionScheduling.UnfreezeMissionRunQueueForRobot(e.RobotId); }
                catch (RobotNotFoundException) { return; }
            }

            try { await MissionScheduling.StopCurrentMissionRun(e.RobotId); }
            catch (RobotNotFoundException) { return; }
            catch (MissionRunNotFoundException)
            {
                /* Allow robot to return to safe position if there is no ongoing mission */
            }
            catch (MissionException ex)
            {
                // We want to continue driving to a safe position if the isar state is idle
                if (ex.IsarStatusCode != StatusCodes.Status409Conflict)
                {
                    _logger.LogError(ex, "Failed to stop the current mission on robot {RobotName} because: {ErrorMessage}", robot.Name, ex.Message);
                    return;
                }
            }
            catch (Exception ex)
            {
                const string Message = "Error in ISAR while stopping current mission, cannot drive to safe position";
                _logger.LogError(ex, "{Message}", Message);
                return;
            }

            MissionScheduling.TriggerRobotAvailable(new RobotAvailableEventArgs(robot.Id));
        }

        private async void OnEmergencyButtonDepressedForRobot(object? sender, EmergencyButtonPressedForRobotEventArgs e)
        {
            _logger.LogInformation("Triggered EmergencyButtonPressed event for robot ID: {RobotId}", e.RobotId);
            var robot = await RobotService.ReadById(e.RobotId);
            if (robot == null)
            {
                _logger.LogError("Robot with ID: {RobotId} was not found in the database", e.RobotId);
                return;
            }

            var area = await AreaService.ReadById(robot.CurrentArea!.Id);
            if (area == null) { _logger.LogError("Could not find area with ID {AreaId}", robot.CurrentArea!.Id); }

            try { await MissionScheduling.UnfreezeMissionRunQueueForRobot(e.RobotId); }
            catch (RobotNotFoundException) { return; }

            if (await MissionScheduling.OngoingMission(robot.Id))
            {
                _logger.LogInformation("Robot {RobotName} was unfrozen but the mission to return to safe zone will be completed before further missions are started", robot.Id);
            }

            MissionScheduling.TriggerRobotAvailable(new RobotAvailableEventArgs(robot.Id));
        }
    }
}
