using Api.Controllers.Models;
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
        private readonly Mutex _scheduleMissionMutex = new();
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

        private IAreaService AreaService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IAreaService>();

        private IMissionScheduling MissionSchedulingService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionScheduling>();

        private IMqttEventHandler MqttEventHandlerService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMqttEventHandler>();

        private IList<MissionRun> MissionRunQueue(string robotId)
        {
            return MissionService
                .ReadAll(
                    new MissionRunQueryStringParameters
                    {
                        Statuses = new List<MissionStatus>
                        {
                            MissionStatus.Pending
                        },
                        RobotId = robotId,
                        OrderBy = "DesiredStartTime",
                        PageSize = 100
                    }
                )
                .Result;
        }

        public override void Subscribe()
        {
            MissionRunService.MissionRunCreated += OnMissionRunCreated;
            MqttEventHandler.RobotAvailable += OnRobotAvailable;
            EmergencyActionService.EmergencyButtonPressedForRobot += OnEmergencyButtonPressedForRobot;
            EmergencyActionService.EmergencyButtonDepressedForRobot += OnEmergencyButtonDepressedForRobot;
        }

        public override void Unsubscribe()
        {
            MissionRunService.MissionRunCreated -= OnMissionRunCreated;
            MqttEventHandler.RobotAvailable -= OnRobotAvailable;
            EmergencyActionService.EmergencyButtonPressedForRobot -= OnEmergencyButtonPressedForRobot;
            EmergencyActionService.EmergencyButtonDepressedForRobot -= OnEmergencyButtonDepressedForRobot;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await stoppingToken;
        }

        private void OnMissionRunCreated(object? sender, MissionRunCreatedEventArgs e)
        {
            _logger.LogInformation("Triggered MissionRunCreated event for mission run ID: {MissionRunId}", e.MissionRunId);

            var missionRun = MissionService.ReadById(e.MissionRunId).Result;

            if (missionRun == null)
            {
                _logger.LogError("Mission run with ID: {MissionRunId} was not found in the database", e.MissionRunId);
                return;
            }

            if (MissionScheduling.MissionRunQueueIsEmpty(MissionRunQueue(missionRun.Robot.Id)))
            {
                _logger.LogInformation("Mission run {MissionRunId} was not started as there are no mission runs on the queue", e.MissionRunId);
                return;
            }

            _scheduleMissionMutex.WaitOne();
            MissionSchedulingService.StartMissionRunIfSystemIsAvailable(missionRun);
            _scheduleMissionMutex.ReleaseMutex();
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

            if (MissionScheduling.MissionRunQueueIsEmpty(MissionRunQueue(robot.Id)))
            {
                _logger.LogInformation("The robot was changed to available but there are no mission runs in the queue to be scheduled");
                return;
            }

            var missionRun = MissionRunQueue(robot.Id).FirstOrDefault(missionRun => missionRun.Robot.Id == robot.Id);

            if (robot.MissionQueueFrozen == true)
            {
                missionRun = MissionRunQueue(robot.Id).FirstOrDefault(missionRun => missionRun.Robot.Id == robot.Id &&
                        missionRun.MissionRunPriority == MissionRunPriority.Emergency);

                if (missionRun == null)
                {
                    _logger.LogInformation("The robot was changed to available in emergency state and no emergency mission run is scheduled");
                    return;
                }
            }

            if (missionRun == null)
            {
                _logger.LogInformation("The robot was changed to available but no mission is scheduled");
                return;
            }

            _scheduleMissionMutex.WaitOne();
            MissionSchedulingService.StartMissionRunIfSystemIsAvailable(missionRun);
            _scheduleMissionMutex.ReleaseMutex();
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

            var area = await AreaService.ReadById(e.AreaId);
            if (area == null)
            {
                _logger.LogError("Could not find area with ID {AreaId}", e.AreaId);
                return;
            }

            await MissionSchedulingService.FreezeMissionRunQueueForRobot(e.RobotId);

            try
            {
                await MissionSchedulingService.StopCurrentMissionRun(e.RobotId);
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
                string message = "Error in ISAR while stopping current mission, cannot drive to safe position";
                _logger.LogError(ex, "{Message}", message);
                return;
            }

            await MissionSchedulingService.ScheduleMissionToReturnToSafePosition(e.RobotId, e.AreaId);
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

            var area = await AreaService.ReadById(e.AreaId);
            if (area == null)
            {
                _logger.LogError("Could not find area with ID {AreaId}", e.AreaId);
            }

            await MissionSchedulingService.UnfreezeMissionRunQueueForRobot(e.RobotId);

            if (await MissionSchedulingService.OngoingMission(robot.Id))
            {
                _logger.LogInformation("Robot {RobotName} was unfrozen but the mission to return to safe zone will be completed before further missions are started", robot.Id);
            }

            MqttEventHandlerService.TriggerRobotAvailable(new RobotAvailableEventArgs(robot.Id));
        }
    }
}
