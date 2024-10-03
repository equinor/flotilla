using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;
namespace Api.Services
{
    public interface IInspectionService
    {
        public Task<Inspection> UpdateInspectionStatus(string isarTaskId, IsarTaskStatus isarTaskStatus);
        public Task<Inspection?> ReadByIsarInspectionId(string id, bool readOnly = true);
        public Task<Inspection?> AddFinding(InspectionFindingQuery inspectionFindingsQuery, string isarTaskId);

    }

    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class InspectionService(FlotillaDbContext context, ILogger<InspectionService> logger, IAccessRoleService accessRoleService) : IInspectionService
    {
        public async Task<Inspection> UpdateInspectionStatus(string isarInspectionId, IsarTaskStatus isarTaskStatus)
        {
            var inspection = await ReadByIsarInspectionId(isarInspectionId, readOnly: false);
            if (inspection is null)
            {
                string errorMessage = $"Inspection with ID {isarInspectionId} could not be found";
                logger.LogError("{Message}", errorMessage);
                throw new InspectionNotFoundException(errorMessage);
            }

            inspection.UpdateStatus(isarTaskStatus);
            inspection = await Update(inspection);
            return inspection;
        }

        private async Task ApplyDatabaseUpdate(Installation? installation)
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes();
            if (installation == null || accessibleInstallationCodes.Contains(installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)))
                await context.SaveChangesAsync();
            else
                throw new UnauthorizedAccessException($"User does not have permission to update area in installation {installation.Name}");
        }

        private async Task<Inspection> Update(Inspection inspection)
        {
            var entry = context.Update(inspection);

            var missionRun = await context.MissionRuns
                    .Include(missionRun => missionRun.Area).ThenInclude(area => area != null ? area.Installation : null)
                    .Include(missionRun => missionRun.Robot)
                    .Where(missionRun => missionRun.Tasks.Any(missionTask => missionTask.Inspection.Id == inspection.Id)).AsNoTracking()
                    .FirstOrDefaultAsync();
            var installation = missionRun?.Area?.Installation;

            await ApplyDatabaseUpdate(installation);

            return entry.Entity;
        }

        public async Task<Inspection?> ReadByIsarInspectionId(string id, bool readOnly = true)
        {
            return await GetInspections(readOnly: readOnly).FirstOrDefaultAsync(inspection => inspection.IsarInspectionId != null && inspection.IsarInspectionId.Equals(id));
        }

        private IQueryable<Inspection> GetInspections(bool readOnly = true)
        {
            if (accessRoleService.IsUserAdmin() || !accessRoleService.IsAuthenticationAvailable())
                return (readOnly ? context.Inspections.AsNoTracking() : context.Inspections.AsTracking()).Include(inspection => inspection.InspectionFindings);
            else
                throw new UnauthorizedAccessException($"User does not have permission to view inspections");
        }

        public async Task<Inspection?> AddFinding(InspectionFindingQuery inspectionFindingQuery, string isarTaskId)
        {
            var inspection = await ReadByIsarInspectionId(isarTaskId, readOnly: false);

            if (inspection is null)
            {
                return null;
            }

            var inspectionFinding = new InspectionFinding
            {
                InspectionDate = inspectionFindingQuery.InspectionDate.ToUniversalTime(),
                Finding = inspectionFindingQuery.Finding,
                IsarTaskId = isarTaskId,
            };

            inspection.InspectionFindings.Add(inspectionFinding);
            inspection = await Update(inspection);
            return inspection;
        }
    }
}
