using Api.Services;
namespace Api.EventHandlers
{
    public class InspectionFindingEventHandler(IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<InspectionFindingEventHandler> logger) : BackgroundService
    {
        private readonly TimeSpan _interval = configuration.GetValue<TimeSpan>("InspectionFindingEventHandler:Interval");
        private InspectionFindingService InspectionFindingService => scopeFactory.CreateScope().ServiceProvider.GetRequiredService<InspectionFindingService>();
        private readonly TimeSpan _timeSpan = configuration.GetValue<TimeSpan>("InspectionFindingEventHandler:TimeSpan");

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_interval, stoppingToken);

                    var lastReportingTime = DateTime.UtcNow - _timeSpan;

                    var inspectionFindings = await InspectionFindingService.RetrieveInspectionFindings(lastReportingTime);

                    logger.LogInformation("Found {count} inspection findings in the last {interval}.", inspectionFindings.Count, _timeSpan);

                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }
        }
    }
}
