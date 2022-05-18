using Api.Context;
using Api.Models;
using Api.Mqtt;
using Api.Mqtt.Events;
using Api.Mqtt.MessageModels;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public class ReportService
    {
        private readonly FlotillaDbContext _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(FlotillaDbContext context, ILogger<ReportService> logger)
        {
            _context = context;
            _logger = logger;
            var mqtt = MqttService.Instance;

            if (mqtt is not null)
            {
                mqtt.MqttIsarMissionReceived += OnMissionUpdate;
                mqtt.MqttIsarTaskReceived += OnTaskUpdate;
                mqtt.MqttIsarStepReceived += OnStepUpdate;
            }
            else
                _logger.LogWarning(
                    "Mqtt service not instantiated - Can't subscribe to events in service '{service}'",
                    nameof(ReportService)
                );
        }

        public async Task<Report> Create(Report report)
        {
            await _context.Reports.AddAsync(report);
            await _context.SaveChangesAsync();
            return report;
        }

        public async Task<Report> Create(
            string isarMissionId,
            string echoMissionId,
            string log,
            ReportStatus status
        )
        {
            var report = new Report
            {
                IsarMissionId = isarMissionId,
                EchoMissionId = echoMissionId,
                Log = log,
                ReportStatus = status,
                StartTime = DateTimeOffset.UtcNow,
            };
            await Create(report);
            return report;
        }

        public async Task<IEnumerable<Report>> ReadAll()
        {
            return await _context.Reports.ToListAsync();
        }

        public async Task<Report?> Read(string id)
        {
            return await _context.Reports.FirstOrDefaultAsync(
                report => report.Id.Equals(id, StringComparison.Ordinal)
            );
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
