using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public class InspectionFindingService(FlotillaDbContext context)
    {
        public async Task<List<InspectionFinding>> RetrieveInspectionFindings(DateTime lastReportingTime)
        {
            var inspectionFindings = await context.InspectionFindings
                                        .Where(f => f.InspectionDate > lastReportingTime)
                                        .ToListAsync();
            return inspectionFindings;
        }

        public async Task<MissionRun?> GetMissionRunByIsarStepId(InspectionFinding inspectionFinding)
        {
            return await context.MissionRuns
                    .Include(mr => mr.Area)
                    .Include(mr => mr.Robot)
                    .Where(mr => mr.Tasks.Any(mt => mt.Inspections.Any(i => i.IsarStepId == inspectionFinding.IsarStepId)))
                    .FirstOrDefaultAsync()
                    ?? null;
        }

        public async Task<MissionTask?> GetMissionTaskByIsarStepId(InspectionFinding inspectionFinding)
        {
            return await context.MissionTasks
                .Where(mt => mt.Inspections.Any(i => i.IsarStepId == inspectionFinding.IsarStepId))
                .FirstOrDefaultAsync()
                ?? null;
        }

    }

}
