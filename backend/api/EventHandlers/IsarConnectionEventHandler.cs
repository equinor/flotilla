using Api.Database.Models;
using Api.Mqtt;
using Api.Mqtt.Events;
using Api.Mqtt.MessageModels;
using Api.Services;
using Api.Utilities;

namespace Api.EventHandlers
{
    /// <summary>
    /// A background service which listens to events and performs callback functions.
    /// </summary>
    public class IsarConnectionEventHandler : EventHandlerBase
    {
        private readonly ILogger<IsarConnectionEventHandler> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private IRobotService RobotService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IRobotService>();

        private readonly Dictionary<string, System.Timers.Timer> _isarConnectionTimers = new();

        private readonly int _isarConnectionTimeout;

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

        public override void Subscribe()
        {
            MqttService.MqttIsarRobotStatusReceived += OnIsarRobotStatus;
        }

        public override void Unsubscribe()
        {
            MqttService.MqttIsarRobotStatusReceived -= OnIsarRobotStatus;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await stoppingToken;
        }

        private async void OnIsarRobotStatus(object? sender, MqttReceivedArgs mqttArgs)
        {
            var isarRobotStatus = (IsarRobotStatusMessage)mqttArgs.Message;
            var robot = await RobotService.ReadByIsarId(isarRobotStatus.IsarId);

            if (robot == null)
            {
                _logger.LogInformation(
                    "Received message from unknown ISAR '{isarId}' ('{robotName}')",
                    isarRobotStatus.IsarId,
                    isarRobotStatus.RobotName
                );
                return;
            }

            if (_isarConnectionTimers.ContainsKey(isarRobotStatus.IsarId))
            {
                _logger.LogDebug(
                    "Reset connection timer for ISAR '{isarId}' ('{robotName}')",
                    isarRobotStatus.IsarId,
                    isarRobotStatus.RobotName
                );
                _isarConnectionTimers[isarRobotStatus.IsarId].Reset();
            }
            else
            {
                var timer = new System.Timers.Timer(_isarConnectionTimeout * 1000);
                timer.Elapsed += (_, _) => OnTimeoutEvent(isarRobotStatus);
                timer.Start();
                _isarConnectionTimers.Add(isarRobotStatus.IsarId, timer);
                _logger.LogInformation(
                    "Added new timer for ISAR '{isarId}' ('{robotName}')",
                    isarRobotStatus.IsarId,
                    isarRobotStatus.RobotName
                );
            }
        }

        private async void OnTimeoutEvent(IsarRobotStatusMessage robotStatusMessage)
        {
            var robot = await RobotService.ReadByIsarId(robotStatusMessage.IsarId);
            if (robot is null)
            {
                _logger.LogError(
                    "Connection to ISAR instance '{id}' ('{robotName}') timed out but the corresponding robot could not be found in the database.",
                    robotStatusMessage.IsarId,
                    robotStatusMessage.IsarId
                );
            }
            else
            {
                _logger.LogWarning(
                    "Connection to ISAR instance '{id}' timed out - It will be set to offline",
                    robotStatusMessage.IsarId
                );
                robot.Status = RobotStatus.Offline;

                await RobotService.Update(robot);
            }

            if (_isarConnectionTimers.TryGetValue(robotStatusMessage.IsarId, out var timer))
            {
                timer.Close();
                _isarConnectionTimers.Remove(robotStatusMessage.IsarId);
            }
        }
    }
}
