using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public class InspectionFindingService(FlotillaDbContext context, IAccessRoleService accessRoleService)
    {
        public async Task<List<InspectionFinding>> RetrieveInspectionFindings(DateTime lastReportingTime, bool readOnly = false)
        {
            var inspectionFindingsQuery = readOnly ? context.InspectionFindings.AsNoTracking() : context.InspectionFindings.AsTracking();
            return await inspectionFindingsQuery.Where(f => f.InspectionDate > lastReportingTime).ToListAsync();
        }

        public async Task<MissionRun?> GetMissionRunByIsarStepId(string isarStepId, bool readOnly = false)
        {
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var query = readOnly ? context.MissionRuns.AsNoTracking() : context.MissionRuns.AsTracking();

#pragma warning disable CA1304
            return await query.Include(missionRun => missionRun.Area).ThenInclude(area => area != null ? area.Plant : null)
                    .Include(missionRun => missionRun.Robot)
                    .Include(missionRun => missionRun.Tasks).ThenInclude(task => task.Inspections)
                    .Where(missionRun => missionRun.Tasks.Any(missionTask => missionTask.Inspections.Any(inspection => inspection.IsarStepId == isarStepId)))
                    .Where((m) => m.Area == null || accessibleInstallationCodes.Result.Contains(m.Area.Installation.InstallationCode.ToUpper()))
                    .FirstOrDefaultAsync();
#pragma warning restore CA1304
        }

        public async Task<MissionTask?> GetMissionTaskByIsarStepId(string isarStepId, bool readOnly = false)
        {
            var missionRun = await GetMissionRunByIsarStepId(isarStepId, readOnly: readOnly);
            return missionRun?.Tasks.Where(missionTask => missionTask.Inspections.Any(inspection => inspection.IsarStepId == isarStepId)).FirstOrDefault();
        }
    }
}
