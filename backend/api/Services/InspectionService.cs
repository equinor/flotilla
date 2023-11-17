using System.Diagnostics.CodeAnalysis;
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
        public Task<Inspection?> AddFindings(InspectionFindingsQuery inspectionFindingsQuery, string isarStepId);

    }

    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class InspectionService(FlotillaDbContext context, ILogger<InspectionService> logger, ISignalRService signalRService) : IInspectionService
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
            _ = signalRService.SendMessageAsync("Inspection updated", inspection);
            return inspection;
        }

        private async Task<Inspection> Update(Inspection inspection)
        {
            var entry = context.Update(inspection);
            await context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<Inspection?> ReadByIsarStepId(string id)
        {
            return await GetInspections().FirstOrDefaultAsync(inspection => inspection.IsarStepId != null && inspection.IsarStepId.Equals(id));
        }

        private IQueryable<Inspection> GetInspections()
        {
            return context.Inspections.Include(inspection => inspection.InspectionFindings);
        }

        public async Task<Inspection?> AddFindings(InspectionFindingsQuery inspectionFindingsQuery, string isarStepId)
        {

            var inspection = await ReadByIsarStepId(isarStepId);

            if (inspection is null)
            {
                return null;
            }

            var inspectionFindings = new InspectionFinding
            {
                InspectionDate = inspectionFindingsQuery.InspectionDate,
                Findings = inspectionFindingsQuery.Findings
            };

            inspection.InspectionFindings.Add(inspectionFindings);
            inspection = await Update(inspection);
            _ = signalRService.SendMessageAsync("Inspection findings added", inspection);
            return inspection;
        }
    }
}
