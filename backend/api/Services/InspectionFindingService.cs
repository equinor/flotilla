using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public class InspectionFindingService(
        FlotillaDbContext context,
        IAccessRoleService accessRoleService
    )
    {
        public async Task<List<InspectionFinding>> RetrieveInspectionFindings(
            DateTime lastReportingTime,
            bool readOnly = true
        )
        {
            var inspectionFindingsQuery = readOnly
                ? context.InspectionFindings.AsNoTracking()
                : context.InspectionFindings.AsTracking();
            return await inspectionFindingsQuery
                .Where(f => f.InspectionDate > lastReportingTime)
                .ToListAsync();
        }

        public async Task<MissionRun?> GetMissionRunByIsarInspectionId(
            string isarTaskId,
            bool readOnly = true
        )
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes();
            var query = readOnly
                ? context.MissionRuns.AsNoTracking()
                : context.MissionRuns.AsTracking();

            return await query
                .Include(missionRun => missionRun.InspectionGroups)
                .ThenInclude(group => group != null ? group.Installation : null)
                .Include(missionRun => missionRun.Robot)
                .Include(missionRun => missionRun.Tasks)
                .ThenInclude(task => task.Inspection)
                .Where(missionRun =>
                    missionRun.Tasks.Any(missionTask =>
                        missionTask.Inspection != null && missionTask.Inspection.Id == isarTaskId
                    )
                )
                .Where(
                    (m) =>
                        m
                            .InspectionGroups.Select(group =>
                                group.Installation.InstallationCode.Trim()
                            )
                            .Any(installationCode =>
                                accessibleInstallationCodes
                                    .Select(code => code.Trim())
                                    .Contains(installationCode)
                            )
                )
                .FirstOrDefaultAsync();

            // && missionDefinition
            //             .InspectionGroups.Select(group => group.Name.Trim().ToLower())
            //             .Any(groupName =>
            //                 parameters
            //                     .InspectionGroups.Select(paramGroup =>
            //                         paramGroup.Trim().ToLower()
            //                     )
            //                     .Contains(groupName)
            //             );
        }

        public async Task<MissionTask?> GetMissionTaskByIsarInspectionId(
            string isarTaskId,
            bool readOnly = true
        )
        {
            var missionRun = await GetMissionRunByIsarInspectionId(isarTaskId, readOnly: readOnly);
            return missionRun
                ?.Tasks.Where(missionTask =>
                    missionTask.Inspection != null && missionTask.Inspection.Id == isarTaskId
                )
                .FirstOrDefault();
        }
    }
}
