using System.Collections.Concurrent;
using Api.Database.Models;
using Api.Mqtt;
using Api.Mqtt.Events;
using Api.Mqtt.MessageModels;
using Api.Services;
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
            var robot = await RobotService.ReadByIsarId(isarRobotHeartbeat.IsarId);

            if (robot == null)
            {
                _logger.LogInformation(
                    "Received message from unknown ISAR '{isarId}' ('{robotName}')",
                    isarRobotHeartbeat.IsarId,
                    isarRobotHeartbeat.RobotName
                );
                return;
            }

            if (!_isarConnectionTimers.ContainsKey(robot.IsarId))
            {
                var timer = new Timer(_isarConnectionTimeout * 1000);
                timer.Elapsed += (_, _) => OnTimeoutEvent(isarRobotHeartbeat);
                timer.Start();
                if (_isarConnectionTimers.TryAdd(robot.IsarId, timer))
                    _logger.LogInformation(
                        "Added new timer for ISAR '{isarId}' ('{robotName}')",
                        robot.IsarId,
                        robot.Name
                    );
            }

            _logger.LogDebug(
                "Reset connection timer for ISAR '{isarId}' ('{robotName}')",
                robot.IsarId,
                robot.Name
            );
            _isarConnectionTimers[robot.IsarId].Reset();

            if (!robot.Enabled)
            {
                robot.Enabled = true;
                await RobotService.Update(robot);
            }
        }

        private async void OnTimeoutEvent(IsarRobotHeartbeatMessage robotHeartbeatMessage)
        {
            var robot = await RobotService.ReadByIsarId(robotHeartbeatMessage.IsarId);
            if (robot is null)
            {
                _logger.LogError(
                    "Connection to ISAR instance '{id}' ('{robotName}') timed out but the corresponding robot could not be found in the database.",
                    robotHeartbeatMessage.IsarId,
                    robotHeartbeatMessage.IsarId
                );
            }
            else
            {
                _logger.LogWarning(
                    "Connection to ISAR instance '{id}' timed out - It will be disabled and active missions aborted",
                    robotHeartbeatMessage.IsarId
                );
                robot.Enabled = false;
                robot.Status = RobotStatus.Offline;
                if (robot.CurrentMissionId != null)
                {
                    var missionRun = await MissionRunService.ReadById(robot.CurrentMissionId);
                    if (missionRun != null)
                    {
                        _logger.LogError(
                            "Mission '{missionId}' ('{missionName}') failed due to ISAR timeout",
                            missionRun.Id,
                            missionRun.Name
                        );
                        missionRun.SetToFailed();
                        await MissionRunService.Update(missionRun);
                    }
                }
                robot.CurrentMissionId = null;
                await RobotService.Update(robot);
            }

            if (_isarConnectionTimers.TryGetValue(robotHeartbeatMessage.IsarId, out var timer))
            {
                timer.Close();
                _isarConnectionTimers.Remove(robotHeartbeatMessage.IsarId, out _);
            }
        }
    }
}
