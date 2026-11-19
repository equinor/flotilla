using Api.Services;

namespace Api.HostedServices
{
    public class AutoSchedulingBackgroundService(
        ILogger<AutoSchedulingBackgroundService> logger,
        IServiceScopeFactory scopeFactory
    ) : BackgroundService
    {
        private static readonly TimeSpan ReconciliationInterval = TimeSpan.FromHours(1);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(ReconciliationInterval);
            do
            {
                try
                {
                    await ReconcileSchedules(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Auto schedule reconciliation failed");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        public async Task ReconcileSchedules(CancellationToken cancellationToken = default)
        {
            using var scope = scopeFactory.CreateScope();
            var definitions = await scope
                .ServiceProvider.GetRequiredService<IMissionDefinitionService>()
                .ReadByHasAutoScheduleFrequency();
            foreach (var definition in definitions ?? [])
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Isolate failures and EF tracking between mission definitions.
                using var missionScope = scopeFactory.CreateScope();
                try
                {
                    await missionScope
                        .ServiceProvider.GetRequiredService<IAutoScheduleService>()
                        .Reconcile(definition);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Could not reconcile mission definition {Id}",
                        definition.Id
                    );
                }
            }
        }
    }
}
