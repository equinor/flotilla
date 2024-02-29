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
        private readonly Semaphore _startMissionSemaphore = new(1, 1);
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
        private IMissionRunService MissionService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionRunService>();

        private IRobotService RobotService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IRobotService>();

        private ILocalizationService LocalizationService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILocalizationService>();

        private IAreaService AreaService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IAreaService>();

        private IMissionSchedulingService MissionScheduling => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionSchedulingService>();

        private ISignalRService SignalRService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ISignalRService>();

        public override void Subscribe()
        {
            MissionRunService.MissionRunCreated += OnMissionRunCreated;
            MissionSchedulingService.RobotAvailable += OnRobotAvailable;
            MissionSchedulingService.MissionCompleted += OnMissionCompleted;
            EmergencyActionService.EmergencyButtonPressedForRobot += OnEmergencyButtonPressedForRobot;
            EmergencyActionService.EmergencyButtonDepressedForRobot += OnEmergencyButtonDepressedForRobot;
        }

        public override void Unsubscribe()
        {
            MissionRunService.MissionRunCreated -= OnMissionRunCreated;
            MissionSchedulingService.RobotAvailable -= OnRobotAvailable;
            MissionSchedulingService.MissionCompleted -= OnMissionCompleted;
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


            if (!await LocalizationService.RobotIsLocalized(missionRun.Robot.Id))
            {
                _scheduleLocalizationSemaphore.WaitOne();
                if (await MissionService.PendingLocalizationMissionRunExists(missionRun.Robot.Id))
                {
                    _scheduleLocalizationSemaphore.Release();
                    return;
                }
                try
                {
                    var localizationMissionRun = await LocalizationService.CreateLocalizationMissionInArea(missionRun.Robot.Id, missionRun.Area.Id);
                    _logger.LogInformation("{Message}", $"Created localization mission run with ID {localizationMissionRun.Id}");
                }
                catch (Exception ex) when (
                    ex is AreaNotFoundException
                    or DeckNotFoundException
                    or RobotNotAvailableException
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
            }

            _startMissionSemaphore.WaitOne();
            try { await MissionScheduling.StartNextMissionRunIfSystemIsAvailable(missionRun.Robot.Id); }
            catch (MissionRunNotFoundException) { return; }
            finally { _startMissionSemaphore.Release(); }
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

            _startMissionSemaphore.WaitOne();
            try { await MissionScheduling.StartNextMissionRunIfSystemIsAvailable(robot.Id); }
            catch (MissionRunNotFoundException) { return; }
            finally { _startMissionSemaphore.Release(); }
        }

        private async void OnMissionCompleted(object? sender, MissionCompletedEventArgs e)
        {
            _logger.LogInformation("Triggered MissionCompleted event for robot ID: {RobotId}", e.RobotId);
            var robot = await RobotService.ReadById(e.RobotId);
            if (robot == null)
            {
                _logger.LogError("Robot with ID: {RobotId} was not found in the database", e.RobotId);
                return;
            }

            _startMissionSemaphore.WaitOne();
            try { await MissionScheduling.StartNextMissionRunIfSystemIsAvailable(robot.Id); }
            catch (MissionRunNotFoundException) { return; }
            finally { _startMissionSemaphore.Release(); }
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
                SignalRService.ReportFailureToSignalR(robot, $"Robot {robot.Name} was not correctly localised. Could not find area {robot.CurrentArea.Name}");
                return;
            }

            try { await MissionScheduling.FreezeMissionRunQueueForRobot(e.RobotId); }
            catch (RobotNotFoundException) { return; }

            try { await MissionScheduling.ScheduleMissionToReturnToSafePosition(e.RobotId, area.Id); }
            catch (SafeZoneException ex)
            {
                _logger.LogError(ex, "Failed to schedule return to safe zone mission on robot {RobotName} because: {ErrorMessage}", robot.Name, ex.Message);
                SignalRService.ReportFailureToSignalR(robot, $"Failed to send {robot.Name} to a safe zone");
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
                    SignalRService.ReportFailureToSignalR(robot, $"Failed to stop current mission for robot {robot.Name}");
                    return;
                }
            }
            catch (Exception ex)
            {
                const string Message = "Error in ISAR while stopping current mission, cannot drive to safe position";
                SignalRService.ReportFailureToSignalR(robot, $"Robot {robot.Name} failed to drive to safe position");
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
            if (area == null)
            {
                _logger.LogError("Could not find area with ID {AreaId}", robot.CurrentArea!.Id);
                SignalRService.ReportFailureToSignalR(robot, $"Robot {robot.Name} could not be sent from safe zone as it is not correctly localised");
            }

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
