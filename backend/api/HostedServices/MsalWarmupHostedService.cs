using Microsoft.Identity.Web;

namespace Api.HostedServices
{
    /// <summary>
    /// Warms up the Microsoft.Identity.Web token-acquisition pipeline at startup by
    /// acquiring a single application (client-credentials) access token for the ISAR
    /// downstream API.
    ///
    /// Microsoft.Identity.Web builds its <c>MergedOptions</c> and the underlying MSAL
    /// confidential-client application lazily on the first token request. That first
    /// initialization is not safe against a second concurrent caller: while one thread
    /// is populating the merged options, another concurrent ISAR call can observe a
    /// half-initialized configuration and fail with <c>"No ClientId was specified."</c>.
    ///
    /// Performing one token acquisition here — before the web server starts accepting
    /// requests — ensures the options and confidential client are fully built once, so
    /// the first real mission-scheduling requests never race that initialization.
    /// </summary>
    public class MsalWarmupHostedService(
        ILogger<MsalWarmupHostedService> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration
    ) : IHostedService
    {
        private static readonly TimeSpan WarmupTimeout = TimeSpan.FromSeconds(30);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var scope = configuration["Isar:Scopes:0"];
            if (string.IsNullOrWhiteSpace(scope))
            {
                logger.LogWarning(
                    "Skipping MSAL warm-up: no ISAR scope configured under 'Isar:Scopes'."
                );
                return;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken
            );
            timeoutCts.CancelAfter(WarmupTimeout);

            try
            {
                using var serviceScope = scopeFactory.CreateScope();
                var tokenAcquisition =
                    serviceScope.ServiceProvider.GetRequiredService<ITokenAcquisition>();

                // Triggers the one-time build of MergedOptions and the MSAL confidential
                // client used for all subsequent app-token calls to ISAR.
                await tokenAcquisition.GetAccessTokenForAppAsync(scope);

                logger.LogInformation(
                    "MSAL token-acquisition pipeline warmed up for ISAR downstream API."
                );
            }
            catch (Exception e)
            {
                // A failed warm-up (e.g. no network or invalid secret in local dev) is not
                // fatal: the merged options are still initialized as a side effect, and the
                // token will be acquired lazily on first real use. Never block startup.
                logger.LogWarning(
                    e,
                    "MSAL warm-up did not complete successfully. Continuing startup; the token will be acquired on first use."
                );
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
