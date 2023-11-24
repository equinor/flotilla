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
    }
}
