using Api.Database.Context;
using Microsoft.EntityFrameworkCore;
using Api.Services;

public class InspectionFindingService : BackgroundService
{
    private readonly ILogger<InspectionService> _logger;
    private readonly ISignalRService _signalRService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public InspectionFindingService(
        ILogger<InspectionService> logger,
        ISignalRService signalRService,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _signalRService = signalRService;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FlotillaDbContext>();

            var last24Hours = DateTime.UtcNow.AddDays(-1);

            //var inspections = await context.Inspections
            //                .Include(i => i.InspectionFindings)
            //                .Where(i => i.InspectionDate > last24Hours)
            //                .ToListAsync(stoppingToken);

            var inspectionFindings = await context.InspectionFindings
                                        .Where(f => f.InspectionDate > last24Hours)
                                        .ToListAsync(stoppingToken);

            Console.WriteLine($"Found inspection findings in the last 24 hours:");

            foreach (var finding in inspectionFindings)
            {
                Console.WriteLine($"- Id: {finding.Id}");
                Console.WriteLine($"  InspectionDate: {finding.InspectionDate}");
                Console.WriteLine($"  IsarStepId: {finding.IsarStepId}");
                Console.WriteLine($"  Findings: {finding.Findings}");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // wait for 1 hour before checking again
        }
    }
}
