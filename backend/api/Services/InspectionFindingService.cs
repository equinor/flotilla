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
                    .Include(missionRun => missionRun.Area).ThenInclude(area => area != null ? area.Plant : null)
                    .Include(missionRun => missionRun.Robot)
                    .Where(missionRun => missionRun.Tasks.Any(missionTask => missionTask.Inspections.Any(inspection => inspection.IsarStepId == inspectionFinding.IsarStepId)))
                    .FirstOrDefaultAsync();
        }

        public async Task<MissionTask?> GetMissionTaskByIsarStepId(InspectionFinding inspectionFinding)
        {
            return await context.MissionTasks
                .Where(missionTask => missionTask.Inspections.Any(inspection => inspection.IsarStepId == inspectionFinding.IsarStepId))
                .FirstOrDefaultAsync();
        }
    }
}
