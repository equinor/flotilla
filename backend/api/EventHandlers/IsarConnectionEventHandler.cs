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
            var robot = await RobotService.ReadByName(isarRobotStatus.RobotName);

            if (robot == null)
            {
                _logger.LogInformation(
                    "Received message from unknown ISAR instance with robot name {name}.",
                    isarRobotStatus.RobotName
                );
                return;
            }

            if (_isarConnectionTimers.ContainsKey(isarRobotStatus.RobotName))
            {
                _logger.LogInformation(
                    "Reset connection timer for robot {name}",
                    isarRobotStatus.RobotName
                );
                _isarConnectionTimers[isarRobotStatus.RobotName].Reset();
            }
            else
            {
                var timer = new System.Timers.Timer(_isarConnectionTimeout * 1000);
                timer.Elapsed += (_, _) => OnTimeoutEvent(isarRobotStatus.RobotName);
                timer.Start();
                _isarConnectionTimers.Add(isarRobotStatus.RobotName, timer);
                _logger.LogInformation(
                    "Added new timer for robot {name}",
                    isarRobotStatus.RobotName
                );
            }
        }

        private async void OnTimeoutEvent(string robotName)
        {
            var robot = await RobotService.ReadByName(robotName);
            if (robot is null)
            {
                _logger.LogError(
                    "An event was received for a robot timer timing out but the robot {robotName} could not be found in the database.",
                    robotName
                );
                return;
            }

            _logger.LogWarning(
                "Connection to robot {name} timed out and it will be set to offline",
                robotName
            );
            robot.Status = RobotStatus.Offline;

            _ = await RobotService.Update(robot);

            _isarConnectionTimers[robotName].Close();
            _isarConnectionTimers.Remove(robotName);
        }
    }
}
