using Api.Database.Context;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public class InspectionFindingService(
        ILogger<InspectionFindingService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
    {
        private readonly ILogger<InspectionFindingService> _logger = logger;
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        private readonly IHostApplicationLifetime _hostApplicationLifetime = hostApplicationLifetime;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<FlotillaDbContext>();

                var last24Hours = DateTime.UtcNow.AddDays(-1);

                var inspectionFindings = await context.InspectionFindings
                                            .Where(f => f.InspectionDate > last24Hours)
                                            .ToListAsync(stoppingToken);

                _logger.LogInformation("Found {count} inspection findings in the last 24 hours.", inspectionFindings.Count);

                foreach (var finding in inspectionFindings)
                {
                    _logger.LogInformation("- Id: {id}", finding.Id);
                    _logger.LogInformation("  InspectionDate: {date}", finding.InspectionDate);
                    _logger.LogInformation("  IsarStepId: {id}", finding.IsarStepId);
                    _logger.LogInformation("  Findings: {findings}", finding.Findings);
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

                // check if the application is stopping, and exit the loop if necessary
                if (_hostApplicationLifetime.ApplicationStopping.IsCancellationRequested)
                {
                    _logger.LogInformation("Application is stopping. Terminating InspectionFindingService.");

                    break;
                }
            }
        }
    }
}
