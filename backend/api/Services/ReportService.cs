using Api.Context;
using Api.Database.Models;
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
            return await _context.Reports
                .Include(report => report.Robot)
                .Include(report => report.Tasks)
                .ThenInclude(task => task.Steps)
                .ToListAsync();
        }

        public async Task<Report?> Read(string id)
        {
            return await _context.Reports.FirstOrDefaultAsync(
                report => report.Id.Equals(id, StringComparison.Ordinal)
            );
        }

        public async Task<Report?> ReadByIsarMissionId(string isarMissionId)
        {
            return await _context.Reports.FirstOrDefaultAsync(
                report => report.IsarMissionId.Equals(isarMissionId, StringComparison.Ordinal)
            );
        }

        public async Task<IsarTask?> ReadIsarTaskById(string isarTaskId)
        {
            return await _context.Tasks.FirstOrDefaultAsync(
                task => task.IsarTaskId.Equals(isarTaskId, StringComparison.Ordinal)
            );
        }

        public async Task<IsarStep?> ReadIsarStepById(string isarStepId)
        {
            return await _context.Steps.FirstOrDefaultAsync(
                step => step.IsarStepId.Equals(isarStepId, StringComparison.Ordinal)
            );
        }

        public async Task<bool> UpdateMissionStatus(string isarMissionId, ReportStatus reportStatus)
        {
            var report = await ReadByIsarMissionId(isarMissionId);
            if (report is null)
            {
                _logger.LogWarning(
                    "Could not update mission status for ISAR mission with id: {id} as the report was not found",
                    isarMissionId
                );
                return false;
            }

            report.ReportStatus = reportStatus;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateTaskStatus(string isarTaskId, IsarTaskStatus taskStatus)
        {
            var task = await ReadIsarTaskById(isarTaskId);
            if (task is null)
            {
                _logger.LogWarning(
                    "Could not update task status for ISAR task with id: {id} as the task was not found",
                    isarTaskId
                );
                return false;
            }

            task.TaskStatus = taskStatus;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateStepStatus(string isarStepId, IsarStepStatus stepStatus)
        {
            var step = await ReadIsarStepById(isarStepId);
            if (step is null)
            {
                _logger.LogWarning(
                    "Could not update step status for ISAR step with id: {id} as the step was not found",
                    isarStepId
                );
                return false;
            }

            step.StepStatus = stepStatus;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
