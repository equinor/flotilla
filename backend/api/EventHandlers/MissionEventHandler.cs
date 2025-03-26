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

        private IRobotService RobotService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IRobotService>();

        private IMissionSchedulingService MissionScheduling =>
            _scopeFactory
                .CreateScope()
                .ServiceProvider.GetRequiredService<IMissionSchedulingService>();

        private ISignalRService SignalRService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ISignalRService>();

        public override void Subscribe()
        {
            MissionRunService.MissionRunCreated += OnMissionRunCreated;
            MissionSchedulingService.RobotReadyForMissions += OnRobotReadyForMissions;
            EmergencyActionService.SendRobotToDockTriggered += OnSendRobotToDockTriggered;
            EmergencyActionService.ReleaseRobotFromDockTriggered += OnReleaseRobotFromDockTriggered;
        }

        public override void Unsubscribe()
        {
            MissionRunService.MissionRunCreated -= OnMissionRunCreated;
            MissionSchedulingService.RobotReadyForMissions -= OnRobotReadyForMissions;
            EmergencyActionService.SendRobotToDockTriggered -= OnSendRobotToDockTriggered;
            EmergencyActionService.ReleaseRobotFromDockTriggered -= OnReleaseRobotFromDockTriggered;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await stoppingToken;
        }

        private async void OnMissionRunCreated(object? sender, MissionRunCreatedEventArgs e)
        {
            var missionRun = e.MissionRun;

            _logger.LogInformation(
                "Triggered MissionRunCreated event for mission run ID: {MissionRunId}",
                missionRun.Id
            );

            _startMissionSemaphore.WaitOne();

            try
            {
                await MissionScheduling.StartNextMissionRunIfSystemIsAvailable(missionRun.Robot);
            }
            catch (MissionRunNotFoundException)
            {
                return;
            }
            finally
            {
                _startMissionSemaphore.Release();
            }
        }

        private async void OnRobotReadyForMissions(object? sender, RobotReadyForMissionsEventArgs e)
        {
            if (!e.Robot.IsRobotReadyToStartMissions())
            {
                _logger.LogWarning("OnRobotReadyForMissions was not ready to start the mission");
                return;
            }
            _startMissionSemaphore.WaitOne();
            try
            {
                await MissionScheduling.StartNextMissionRunIfSystemIsAvailable(e.Robot);
            }
            catch (MissionRunNotFoundException)
            {
                return;
            }
            finally
            {
                _startMissionSemaphore.Release();
            }
        }

        private async void OnSendRobotToDockTriggered(object? sender, RobotEmergencyEventArgs e)
        {
            var robot = e.Robot;

            _logger.LogInformation(
                "Triggered EmergencyButtonPressed event for robot ID: {RobotId}",
                robot.Id
            );

            try
            {
                await MissionScheduling.FreezeMissionRunQueueForRobot(robot.Id);
            }
            catch (RobotNotFoundException)
            {
                return;
            }

            if (robot.FlotillaStatus == e.RobotFlotillaStatus)
            {
                _logger.LogInformation(
                    "Did not send robot to Dock since robot {RobotId} was already in the correct state",
                    robot.Id
                );
                return;
            }

            try
            {
                await RobotService.UpdateFlotillaStatus(robot.Id, e.RobotFlotillaStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Was not able to update Robot Flotilla status for robot {RobotId}, {ErrorMessage}",
                    robot.Id,
                    ex.Message
                );
                return;
            }

            try
            {
                await MissionScheduling.ScheduleMissionToDriveToDockPosition(robot.Id);
            }
            catch (Exception ex) when (ex is DockException || ex is InspectionAreaNotFoundException)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to schedule return to dock mission on robot {RobotName} because: {ErrorMessage}",
                    robot.Name,
                    ex.Message
                );
                SignalRService.ReportDockFailureToSignalR(
                    robot,
                    $"Failed to send {robot.Name} to a dock"
                );
            }

            try
            {
                await MissionScheduling.StopCurrentMissionRun(robot.Id);
            }
            catch (RobotNotFoundException)
            {
                return;
            }
            catch (MissionRunNotFoundException)
            {
                /* Allow robot to return to dock if there is no ongoing mission */
            }
            catch (MissionException ex)
            {
                // We want to continue driving to the dock if the isar state is idle
                if (ex.IsarStatusCode != StatusCodes.Status409Conflict)
                {
                    _logger.LogError(
                        ex,
                        "Failed to stop the current mission on robot {RobotName} because: {ErrorMessage}",
                        robot.Name,
                        ex.Message
                    );
                    SignalRService.ReportDockFailureToSignalR(
                        robot,
                        $"Failed to stop current mission for robot {robot.Name}"
                    );
                    return;
                }
            }
            catch (Exception ex)
            {
                const string Message =
                    "Error in ISAR while stopping current mission, cannot drive to docking station.";
                SignalRService.ReportDockFailureToSignalR(
                    robot,
                    $"Robot {robot.Name} failed to drive to docking station."
                );
                _logger.LogError(ex, "{Message}", Message);
                return;
            }

            _startMissionSemaphore.WaitOne();
            try
            {
                await MissionScheduling.StartNextMissionRunIfSystemIsAvailable(robot);
            }
            catch (MissionRunNotFoundException)
            {
                return;
            }
            finally
            {
                _startMissionSemaphore.Release();
            }
        }

        private async void OnReleaseRobotFromDockTriggered(
            object? sender,
            RobotEmergencyEventArgs e
        )
        {
            var robot = e.Robot;

            _logger.LogInformation(
                "Triggered EmergencyButtonPressed event for robot ID: {RobotId}",
                e.Robot.Id
            );

            try
            {
                await MissionScheduling.UnfreezeMissionRunQueueForRobot(robot.Id);
            }
            catch (RobotNotFoundException)
            {
                return;
            }

            robot.MissionQueueFrozen = false;

            try
            {
                await RobotService.UpdateFlotillaStatus(robot.Id, e.RobotFlotillaStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Was not able to update Robot Flotilla status for robot {RobotId}, {ErrorMessage}",
                    robot.Id,
                    ex.Message
                );
                return;
            }

            _startMissionSemaphore.WaitOne();
            try
            {
                await MissionScheduling.StartNextMissionRunIfSystemIsAvailable(robot);
            }
            catch (MissionRunNotFoundException)
            {
                return;
            }
            finally
            {
                _startMissionSemaphore.Release();
            }
        }
    }
}
