using Api.Database.Models;
using Api.Services;
using Api.Services.Events;
using Api.Utilities;

namespace Api.EventHandlers
{
    public class MissionEventHandler : EventHandlerBase
    {
        private readonly ILogger<MissionEventHandler> _logger;

        private readonly IServiceScopeFactory _scopeFactory;
        private EventAggregatorSingletonService _eventAggregatorSingletonService;

        public MissionEventHandler(
            ILogger<MissionEventHandler> logger,
            IServiceScopeFactory scopeFactory,
            EventAggregatorSingletonService eventAggregatorSingletonService
        )
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _eventAggregatorSingletonService = eventAggregatorSingletonService;

            Subscribe();
        }

        private IMissionSchedulingService MissionScheduling =>
            _scopeFactory
                .CreateScope()
                .ServiceProvider.GetRequiredService<IMissionSchedulingService>();

        private IIsarService IsarService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IIsarService>();

        public override void Subscribe()
        {
            _eventAggregatorSingletonService.Subscribe<MissionRunCreatedEventArgs>(
                OnMissionRunCreated
            );
            _eventAggregatorSingletonService.Subscribe<RobotReadyForMissionsEventArgs>(
                OnRobotReadyForMissions
            );
            _eventAggregatorSingletonService.Subscribe<RobotEmergencyEventArgs>(
                OnLockdownRobotTriggered
            );
            _eventAggregatorSingletonService.Subscribe<RobotEmergencyEventArgs>(
                OnReleaseRobotFromLockdownTriggered
            );
        }

        public override void Unsubscribe() { }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await stoppingToken;
        }

        private async void OnMissionRunCreated(MissionRunCreatedEventArgs e)
        {
            MissionRun missionRun = e.MissionRun;

            _logger.LogInformation(
                "Triggered MissionRunCreated event for mission run ID: {MissionRunId}",
                missionRun.Id
            );

            try
            {
                await MissionScheduling.StartNextMissionRunIfSystemIsAvailable(missionRun.Robot);
            }
            catch (MissionRunNotFoundException)
            {
                _logger.LogWarning(
                    "Mission run not found for robot ID: {RobotId} when exceuting OnMissionRunCreated",
                    missionRun.Robot.Id
                );
            }
        }

        private async void OnRobotReadyForMissions(RobotReadyForMissionsEventArgs e)
        {
            Robot robot = e.Robot;
            try
            {
                await MissionScheduling.StartNextMissionRunIfSystemIsAvailable(robot);
            }
            catch (MissionRunNotFoundException)
            {
                _logger.LogWarning(
                    "Mission run not found for robot ID: {RobotId} when excecuting OnRobotReadyForMissions",
                    robot.Id
                );
            }
        }

        private async void OnLockdownRobotTriggered(RobotEmergencyEventArgs e)
        {
            Robot robot = e.Robot;

            _logger.LogInformation(
                "Triggered RobotEmergencyEvent for robot ID: {RobotId}",
                robot.Id
            );

            try
            {
                await IsarService.SendToLockdown(robot.IsarUri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send robot {RobotId} to lockdown", robot.Id);
                return;
            }
        }

        private async void OnReleaseRobotFromLockdownTriggered(RobotEmergencyEventArgs e)
        {
            Robot robot = e.Robot;

            _logger.LogInformation(
                "Triggered release robot from lockdown event for robot ID: {RobotId}",
                e.Robot.Id
            );

            try
            {
                await IsarService.ReleaseFromLockdown(robot.IsarUri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to release robot {RobotId} from lockdown", robot.Id);
                return;
            }
        }
    }
}
