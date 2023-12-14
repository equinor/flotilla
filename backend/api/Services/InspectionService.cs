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
        public Task<Inspection> UpdateInspectionStatus(string isarStepId, IsarStepStatus isarStepStatus);
        public Task<Inspection?> ReadByIsarStepId(string id);
        public Task<Inspection?> AddFinding(InspectionFindingQuery inspectionFindingsQuery, string isarStepId);

    }

    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class InspectionService(FlotillaDbContext context, ILogger<InspectionService> logger, IAccessRoleService accessRoleService) : IInspectionService
    {
        public async Task<Inspection> UpdateInspectionStatus(string isarStepId, IsarStepStatus isarStepStatus)
        {
            var inspection = await ReadByIsarStepId(isarStepId);
            if (inspection is null)
            {
                string errorMessage = $"Inspection with ID {isarStepId} could not be found";
                logger.LogError("{Message}", errorMessage);
                throw new InspectionNotFoundException(errorMessage);
            }

            inspection.UpdateStatus(isarStepStatus);
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
                    .Where(missionRun => missionRun.Tasks.Any(missionTask => missionTask.Inspections.Any(i => i.Id == inspection.Id)))
                    .FirstOrDefaultAsync();
            var installation = missionRun?.Area?.Installation;

            await ApplyDatabaseUpdate(installation);

            return entry.Entity;
        }

        public async Task<Inspection?> ReadByIsarStepId(string id)
        {
            return await GetInspections().FirstOrDefaultAsync(inspection => inspection.IsarStepId != null && inspection.IsarStepId.Equals(id));
        }

        private IQueryable<Inspection> GetInspections()
        {
            if (accessRoleService.IsUserAdmin() || !accessRoleService.IsAuthenticationAvailable())
                return context.Inspections.Include(inspection => inspection.InspectionFindings);
            else
                throw new UnauthorizedAccessException($"User does not have permission to view inspections");
        }

        public async Task<Inspection?> AddFinding(InspectionFindingQuery inspectionFindingQuery, string isarStepId)
        {
            var inspection = await ReadByIsarStepId(isarStepId);

            if (inspection is null)
            {
                return null;
            }

            var inspectionFinding = new InspectionFinding
            {
                InspectionDate = inspectionFindingQuery.InspectionDate,
                Finding = inspectionFindingQuery.Finding,
                IsarStepId = isarStepId,
            };

            inspection.InspectionFindings.Add(inspectionFinding);
            inspection = await Update(inspection);
            return inspection;
        }
    }
}
