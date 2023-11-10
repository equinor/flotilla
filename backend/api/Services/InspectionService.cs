using System.Diagnostics.CodeAnalysis;
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
    }

    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class InspectionService : IInspectionService
    {
        private readonly FlotillaDbContext _context;
        private readonly ILogger<InspectionService> _logger;
        private readonly ISignalRService _signalRService;

        public InspectionService(FlotillaDbContext context, ILogger<InspectionService> logger, ISignalRService signalRService)
        {
            _context = context;
            _logger = logger;
            _signalRService = signalRService;
        }

        public async Task<Inspection> UpdateInspectionStatus(string isarStepId, IsarStepStatus isarStepStatus)
        {
            var inspection = await ReadByIsarStepId(isarStepId);
            if (inspection is null)
            {
                string errorMessage = $"Inspection with ID {isarStepId} could not be found";
                _logger.LogError("{Message}", errorMessage);
                throw new InspectionNotFoundException(errorMessage);
            }

            inspection.UpdateStatus(isarStepStatus);
            inspection = await Update(inspection);
            _ = _signalRService.SendMessageAsync("Inspection updated", inspection);
            return inspection;
        }

        private async Task<Inspection> Update(Inspection inspection)
        {
            var entry = _context.Update(inspection);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        private async Task<Inspection?> ReadByIsarStepId(string id)
        {
            return await GetInspections().FirstOrDefaultAsync(inspection => inspection.IsarStepId != null && inspection.IsarStepId.Equals(id));
        }

        private IQueryable<Inspection> GetInspections()
        {
            return _context.Inspections.Include(inspection => inspection.InspectionFindings);
        }
    }
}
