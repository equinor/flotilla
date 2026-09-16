using Azure.Core;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Api.Database.Context
{
    /// <summary>
    /// This class is not called by anything explicitly, but is used by EF core when adding migrations and updating database.
    /// </summary>
    public class DesignTimeContextFactory : IDesignTimeDbContextFactory<FlotillaDbContext>
    {
        private static readonly TimeSpan TokenTimeout = TimeSpan.FromSeconds(30);
        private const string PostgresScope = "https://ossrdbms-aad.database.windows.net/.default";

        // We cannot use dependency injection directly in this class, hence the "manual" extraction of the config variables
        // Followed this tutorial: https://blog.tonysneed.com/2018/12/20/idesigntimedbcontextfactory-and-dependency-injection-a-love-story/
        public FlotillaDbContext CreateDbContext(string[] args)
        {
            // Get environment
            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!;

            string projectPath = Path.Combine(
                Directory.GetParent(Directory.GetCurrentDirectory())!.FullName,
                "api"
            );

            // Build config
            var config = new ConfigurationBuilder()
                .SetBasePath(projectPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            string? mode = config["Migrations:AuthenticationMode"];
            if (mode is null || string.Equals(mode, "AzureCli", StringComparison.OrdinalIgnoreCase))
                return CreateAzureCliContext(config);

            if (!string.Equals(mode, "LocalConnectionString", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Migrations:AuthenticationMode must be AzureCli or LocalConnectionString when specified."
                );

            if (
                !new[] { "Local", "Development", "IntegrationTest", "Test" }.Contains(
                    environment,
                    StringComparer.OrdinalIgnoreCase
                )
            )
                throw new InvalidOperationException(
                    "LocalConnectionString migration authentication is only allowed in Local, Development, IntegrationTest or Test."
                );

            string connectionString = RequiredValue(config, "Database:postgresConnectionString");

            var optionsBuilder = new DbContextOptionsBuilder<FlotillaDbContext>();

            // Setting splitting behavior explicitly to avoid warning
            optionsBuilder.UseNpgsql(
                connectionString,
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)
            );

            return new FlotillaDbContext(optionsBuilder.Options);
        }

        private static FlotillaDbContext CreateAzureCliContext(IConfiguration config)
        {
            string host = RequiredValue(config, "Migrations:Postgres:Host");
            if (Uri.CheckHostName(host) == UriHostNameType.Unknown)
                throw new InvalidOperationException(
                    "Migrations:Postgres:Host must be a single hostname or IP address without a port."
                );

            string database = RequiredValue(config, "Migrations:Postgres:Database");
            string username = RequiredValue(config, "Migrations:Postgres:Username");
            string tenant = RequiredValue(config, "AZURE_TENANT_ID");
            if (!Guid.TryParseExact(tenant, "D", out var tenantId) || tenantId == Guid.Empty)
                throw new InvalidOperationException(
                    "AZURE_TENANT_ID must be a nonempty tenant GUID."
                );

            var credentialOptions = new AzureCliCredentialOptions
            {
                TenantId = tenant,
                ProcessTimeout = TokenTimeout,
            };
            var credential = new AzureCliCredential(credentialOptions);
            var connectionString = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Database = database,
                Username = username,
                SslMode = SslMode.VerifyFull,
            };
            var builder = new NpgsqlDataSourceBuilder(connectionString.ConnectionString);
            builder.UsePasswordProvider(
                _ => GetPassword(credential),
                (_, cancellationToken) => GetPasswordAsync(credential, cancellationToken)
            );
            var dataSource = builder.Build();
            try
            {
                var options = new DbContextOptionsBuilder<FlotillaDbContext>().UseNpgsql(
                    dataSource,
                    o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)
                );
                return new FlotillaDbContext(options.Options, dataSource);
            }
            catch
            {
                dataSource.Dispose();
                throw;
            }
        }

        private static string RequiredValue(IConfiguration config, string key)
        {
            string? value = config[key];
            if (
                string.IsNullOrWhiteSpace(value)
                || value != value.Trim()
                || value.Any(char.IsControl)
            )
                throw new InvalidOperationException(
                    $"{key} must be nonempty with no surrounding whitespace or control characters."
                );
            return value;
        }

        private static string GetPassword(AzureCliCredential credential)
        {
            using var cancellation = new CancellationTokenSource(TokenTimeout);
            try
            {
                return RequireToken(
                    credential.GetToken(
                        new TokenRequestContext([PostgresScope]),
                        cancellation.Token
                    )
                );
            }
            catch (AuthenticationFailedException)
            {
                // Azure CLI output can appear in credential exceptions that Npgsql logs.
                throw new AuthenticationFailedException(
                    "Azure CLI migration token acquisition failed. Check the dedicated identity login and tenant."
                );
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException(
                    "Azure CLI migration token acquisition was cancelled or timed out.",
                    cancellation.Token
                );
            }
        }

        private static async ValueTask<string> GetPasswordAsync(
            AzureCliCredential credential,
            CancellationToken cancellationToken
        )
        {
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken
            );
            cancellation.CancelAfter(TokenTimeout);
            try
            {
                return RequireToken(
                    await credential
                        .GetTokenAsync(new TokenRequestContext([PostgresScope]), cancellation.Token)
                        .ConfigureAwait(false)
                );
            }
            catch (AuthenticationFailedException)
            {
                throw new AuthenticationFailedException(
                    "Azure CLI migration token acquisition failed. Check the dedicated identity login and tenant."
                );
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException(
                    cancellationToken.IsCancellationRequested
                        ? "Azure CLI migration token acquisition was cancelled."
                        : "Azure CLI migration token acquisition was cancelled or timed out.",
                    cancellationToken.IsCancellationRequested
                        ? cancellationToken
                        : cancellation.Token
                );
            }
        }

        private static string RequireToken(AccessToken token)
        {
            if (string.IsNullOrWhiteSpace(token.Token))
                throw new InvalidOperationException(
                    "Azure CLI returned an empty PostgreSQL migration token."
                );
            return token.Token;
        }
    }
}
