using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public class InspectionFindingService(FlotillaDbContext context, IAccessRoleService accessRoleService)
    {
        public async Task<List<InspectionFinding>> RetrieveInspectionFindings(DateTime lastReportingTime, bool readOnly = true)
        {
            var inspectionFindingsQuery = readOnly ? context.InspectionFindings.AsNoTracking() : context.InspectionFindings.AsTracking();
            return await inspectionFindingsQuery.Where(f => f.InspectionDate > lastReportingTime).ToListAsync();
        }

        public async Task<MissionRun?> GetMissionRunByIsarInspectionId(string isarTaskId, bool readOnly = true)
        {
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var query = readOnly ? context.MissionRuns.AsNoTracking() : context.MissionRuns.AsTracking();

#pragma warning disable CA1304
            return await query.Include(missionRun => missionRun.Area).ThenInclude(area => area != null ? area.Plant : null)
                    .Include(missionRun => missionRun.Robot)
                    .Include(missionRun => missionRun.Tasks).ThenInclude(task => task.Inspection)
                    .Where(missionRun => missionRun.Tasks.Any(missionTask => missionTask.Inspection.Id == isarTaskId))
                    .Where((m) => m.Area == null || accessibleInstallationCodes.Result.Contains(m.Area.Installation.InstallationCode.ToUpper()))
                    .FirstOrDefaultAsync();
#pragma warning restore CA1304
        }

        public async Task<MissionTask?> GetMissionTaskByIsarInspectionId(string isarTaskId, bool readOnly = true)
        {
            var missionRun = await GetMissionRunByIsarInspectionId(isarTaskId, readOnly: readOnly);
            return missionRun?.Tasks.Where(missionTask => missionTask.Inspection.Id == isarTaskId).FirstOrDefault();
        }
    }
}
