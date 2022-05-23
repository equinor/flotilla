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
    public class MqttEventHandler : BackgroundService
    {
        private readonly ILogger<MqttEventHandler> _logger;
        private readonly ReportService _reportService;

        public MqttEventHandler(ReportService reportService, ILogger<MqttEventHandler> logger)
        {
            _logger = logger;
            _reportService = reportService;

            MqttService.MqttIsarMissionReceived += OnMissionUpdate;
            MqttService.MqttIsarTaskReceived += OnTaskUpdate;
            MqttService.MqttIsarStepReceived += OnStepUpdate;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await stoppingToken;
        }

        private void OnMissionUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var mission = (IsarMission)mqttArgs.Message;
            _logger.LogInformation(
                "{time} - Mission {id} updated",
                mission.Timestamp,
                mission.MissionId
            );
        }

        private void OnTaskUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var task = (IsarTask)mqttArgs.Message;
            _logger.LogInformation("{time} - Task {id} updated", task.Timestamp, task.TaskId);
        }

        private void OnStepUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var step = (IsarStep)mqttArgs.Message;
            _logger.LogInformation("{time} - Step {id} updated", step.Timestamp, step.StepId);
        }
    }
}
