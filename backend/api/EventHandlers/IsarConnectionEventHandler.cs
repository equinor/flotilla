using System.Collections.Concurrent;
using Api.Database.Models;
using Api.Mqtt;
using Api.Mqtt.Events;
using Api.Mqtt.MessageModels;
using Api.Services;
using Api.Services.Events;
using Api.Utilities;
using Timer = System.Timers.Timer;

namespace Api.EventHandlers
{
    /// <summary>
    ///     A background service which listens to events and performs callback functions.
    /// </summary>
    public class IsarConnectionEventHandler : EventHandlerBase
    {
        private readonly int _isarConnectionTimeout;

        private readonly ConcurrentDictionary<string, Timer> _isarConnectionTimers = new();
        private readonly ILogger<IsarConnectionEventHandler> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public IsarConnectionEventHandler(
            ILogger<IsarConnectionEventHandler> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration config
        )
        {
            _logger = logger;

            _isarConnectionTimeout = config.GetValue<int>("IsarConnectionTimeout");

            // Reason for using factory: https://www.thecodebuzz.com/using-dbcontext-instance-in-ihostedservice/
            _scopeFactory = scopeFactory;

            Subscribe();
        }

        private IRobotService RobotService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IRobotService>();

        private IMissionRunService MissionRunService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionRunService>();

        private IMissionSchedulingService MissionSchedulingService =>
            _scopeFactory
                .CreateScope()
                .ServiceProvider.GetRequiredService<IMissionSchedulingService>();

        public override void Subscribe()
        {
            MqttService.MqttIsarRobotHeartbeatReceived += OnIsarRobotHeartbeat;
        }

        public override void Unsubscribe()
        {
            MqttService.MqttIsarRobotHeartbeatReceived -= OnIsarRobotHeartbeat;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await stoppingToken;
        }

        private async void OnIsarRobotHeartbeat(object? sender, MqttReceivedArgs mqttArgs)
        {
            var isarRobotHeartbeat = (IsarRobotHeartbeatMessage)mqttArgs.Message;
            var robot = await RobotService.ReadByIsarId(isarRobotHeartbeat.IsarId, readOnly: true);

            if (robot == null)
            {
                _logger.LogInformation(
                    "Received message from unknown ISAR '{IsarId}' ('{RobotName}')",
                    isarRobotHeartbeat.IsarId,
                    isarRobotHeartbeat.RobotName
                );
                return;
            }

            if (!_isarConnectionTimers.ContainsKey(robot.IsarId))
            {
                AddTimerForRobot(isarRobotHeartbeat, robot);
            }

            _logger.LogDebug(
                "Reset connection timer for ISAR '{IsarId}' ('{RobotName}')",
                robot.IsarId,
                robot.Name
            );

            _isarConnectionTimers[robot.IsarId].Reset();

            if (robot.IsarConnected)
            {
                return;
            }
            try
            {
                await RobotService.UpdateRobotIsarConnected(robot.Id, true);
                await RobotService.UpdateRobotDisconnectTime(robot.Id, null);
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    "Failed to set robot to ISAR connected for ISAR ID '{IsarId}' ('{RobotName}')'. Exception: {Message} ",
                    isarRobotHeartbeat.IsarId,
                    isarRobotHeartbeat.RobotName,
                    e.Message
                );
                return;
            }

            // If the robot became available while the connection was not active, then this will not be triggered
            // It will however be triggered if the robot lost connection while restarting or while idle
            MissionSchedulingService.TriggerRobotReadyForMissions(
                new RobotReadyForMissionsEventArgs(robot)
            );
        }

        private void AddTimerForRobot(IsarRobotHeartbeatMessage isarRobotHeartbeat, Robot robot)
        {
            var timer = new Timer(_isarConnectionTimeout * 1000);
            timer.Elapsed += (_, _) => OnTimeoutEvent(isarRobotHeartbeat);
            timer.Start();

            if (_isarConnectionTimers.TryAdd(robot.IsarId, timer))
            {
                _logger.LogInformation(
                    "Added new timer for ISAR '{IsarId}' ('{RobotName}')",
                    robot.IsarId,
                    robot.Name
                );
            }
            else
            {
                _logger.LogWarning(
                    "Failed to add new timer for ISAR '{IsarId}' ('{RobotName})'",
                    robot.IsarId,
                    robot.Name
                );
                timer.Close();
            }
        }

        private async void OnTimeoutEvent(IsarRobotHeartbeatMessage robotHeartbeatMessage)
        {
            var robot = await RobotService.ReadByIsarId(
                robotHeartbeatMessage.IsarId,
                readOnly: true
            );
            if (robot is null)
            {
                _logger.LogError(
                    "Connection to ISAR instance '{Id}' ('{RobotName}') timed out but the corresponding robot could not be found in the database",
                    robotHeartbeatMessage.IsarId,
                    robotHeartbeatMessage.IsarId
                );
            }
            else if (robot.IsarConnected)
            {
                _logger.LogWarning(
                    "Connection to ISAR instance '{Id}' timed out - It will be disabled and active missions aborted",
                    robotHeartbeatMessage.IsarId
                );

                if (robot.CurrentMissionId != null)
                {
                    var missionRun = await MissionRunService.ReadById(
                        robot.CurrentMissionId,
                        readOnly: true
                    );
                    if (missionRun != null)
                    {
                        _logger.LogError(
                            "Mission '{MissionId}' ('{MissionName}') failed due to ISAR timeout",
                            missionRun.Id,
                            missionRun.Name
                        );
                        try
                        {
                            await MissionRunService.SetMissionRunToFailed(
                                missionRun.Id,
                                "Lost connection to ISAR during mission"
                            );
                        }
                        catch (MissionRunNotFoundException)
                        {
                            _logger.LogError(
                                "Mission '{MissionId}' could not be set to failed as it no longer exists",
                                missionRun.Id
                            );
                        }
                    }
                }

                try
                {
                    await RobotService.UpdateRobotIsarConnected(robot.Id, false);
                    await RobotService.UpdateRobotDisconnectTime(robot.Id, DateTime.UtcNow);
                    await RobotService.UpdateCurrentMissionId(robot.Id, null);
                }
                catch (RobotNotFoundException)
                {
                    return;
                }
            }

            if (!_isarConnectionTimers.TryGetValue(robotHeartbeatMessage.IsarId, out var timer))
            {
                return;
            }
            timer.Close();
            _isarConnectionTimers.Remove(robotHeartbeatMessage.IsarId, out _);
            _logger.LogError(
                "Removed timer for ISAR instance {RobotName} with ID '{Id}'",
                robotHeartbeatMessage.RobotName,
                robotHeartbeatMessage.IsarId
            );
        }
    }
}
