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

        private IIsarService IsarService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IIsarService>();

        private ISignalRService SignalRService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ISignalRService>();

        public override void Subscribe()
        {
            MissionRunService.MissionRunCreated += OnMissionRunCreated;
            MissionSchedulingService.RobotReadyForMissions += OnRobotReadyForMissions;
            EmergencyActionService.LockdownRobotTriggered += OnLockdownRobotTriggered;
            EmergencyActionService.ReleaseRobotFromLockdownTriggered +=
                OnReleaseRobotFromLockdownTriggered;
        }

        public override void Unsubscribe()
        {
            MissionRunService.MissionRunCreated -= OnMissionRunCreated;
            MissionSchedulingService.RobotReadyForMissions -= OnRobotReadyForMissions;
            EmergencyActionService.LockdownRobotTriggered -= OnLockdownRobotTriggered;
            EmergencyActionService.ReleaseRobotFromLockdownTriggered -=
                OnReleaseRobotFromLockdownTriggered;
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
                _logger.LogWarning(
                    "Mission run not found for robot ID: {RobotId} when exceuting OnMissionRunCreated",
                    missionRun.Robot.Id
                );
            }
            finally
            {
                _startMissionSemaphore.Release();
            }
        }

        private async void OnRobotReadyForMissions(object? sender, RobotReadyForMissionsEventArgs e)
        {
            _startMissionSemaphore.WaitOne();
            try
            {
                await MissionScheduling.StartNextMissionRunIfSystemIsAvailable(e.Robot);
            }
            catch (MissionRunNotFoundException)
            {
                _logger.LogWarning(
                    "Mission run not found for robot ID: {RobotId} when excecuting OnRobotReadyForMissions",
                    e.Robot.Id
                );
            }
            finally
            {
                _startMissionSemaphore.Release();
            }
        }

        private async void OnLockdownRobotTriggered(object? sender, RobotEmergencyEventArgs e)
        {
            var robot = e.Robot;

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

        private async void OnReleaseRobotFromLockdownTriggered(
            object? sender,
            RobotEmergencyEventArgs e
        )
        {
            var robot = e.Robot;

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
