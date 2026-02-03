using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Api.Database.Context
{
    /// <summary>
    /// This class is not called by anything explicitly, but is used by EF core when adding migrations and updating database.
    /// </summary>
    public class DesignTimeContextFactory : IDesignTimeDbContextFactory<FlotillaDbContext>
    {
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

            string? keyVaultUri =
                config.GetSection("KeyVault")["VaultUri"]
                ?? throw new KeyNotFoundException("No key vault in config");

            // Connect to keyvault
            var keyVault = new SecretClient(
                new Uri(keyVaultUri),
                new DefaultAzureCredential(new DefaultAzureCredentialOptions())
            );

            // Get connection string
            string? connectionString = keyVault
                .GetSecret("Database--PostgreSqlConnectionString")
                .Value.Value;

            var optionsBuilder = new DbContextOptionsBuilder<FlotillaDbContext>();

            // Setting splitting behavior explicitly to avoid warning
            optionsBuilder.UseNpgsql(
                connectionString,
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)
            );

            return new FlotillaDbContext(optionsBuilder.Options);
        }
    }
}
