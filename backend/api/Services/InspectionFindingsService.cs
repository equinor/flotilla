using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;


namespace Api.Services
{
    public class IInspectionFindingService()
    {

        public async Task<List<InspectionFinding>> RetrieveInspectionFindings(TimeSpan timeSpan, FlotillaDbContext context)
        {
            var lastReportingTime = DateTime.UtcNow - timeSpan;
            var inspectionFindings = await context.InspectionFindings
                                        .Where(f => f.InspectionDate > lastReportingTime)
                                        .ToListAsync();
            return inspectionFindings;
        }

    }
}
