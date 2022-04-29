using Api.Context;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public class ReportService
    {
        private readonly FlotillaDbContext _context;

        public ReportService(FlotillaDbContext context)
        {
            _context = context;
        }

        public async Task<Report> Create(Report report)
        {
            await _context.Reports.AddAsync(report);
            await _context.SaveChangesAsync();
            return report;
        }

        public async Task<Report> Create(string isarMissionId, string echoMissionId, string log, ReportStatus status)
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
            return await _context.Reports.FirstOrDefaultAsync(report => report.Id.Equals(id, StringComparison.Ordinal));
        }
    }
}
