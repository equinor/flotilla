using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public class InspectionFindingService(FlotillaDbContext context, IAccessRoleService accessRoleService)
    {
        public async Task<List<InspectionFinding>> RetrieveInspectionFindings(DateTime lastReportingTime)
        {
            var inspectionFindings = await context.InspectionFindings
                                        .Where(f => f.InspectionDate > lastReportingTime)
                                        .ToListAsync();
            return inspectionFindings;
        }

        public async Task<MissionRun?> GetMissionRunByIsarStepId(string isarStepId)
        {
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
#pragma warning disable CA1304
            return await context.MissionRuns
                    .Include(missionRun => missionRun.Area).ThenInclude(area => area != null ? area.Plant : null)
                    .Include(missionRun => missionRun.Robot)
                    .Where(missionRun => missionRun.Tasks.Any(missionTask => missionTask.Inspections.Any(inspection => inspection.IsarStepId == isarStepId)))
                    .Where((m) => m.Area != null && accessibleInstallationCodes.Result.Contains(m.Area.Installation.InstallationCode.ToUpper()))
                    .FirstOrDefaultAsync();
#pragma warning restore CA1304
        }

        public async Task<MissionTask?> GetMissionTaskByIsarStepId(string isarStepId)
        {
            var missionRun = await GetMissionRunByIsarStepId(isarStepId);
            return missionRun?.Tasks.Where(missionTask => missionTask.Inspections.Any(inspection => inspection.IsarStepId == isarStepId)).FirstOrDefault();
        }
    }
}
