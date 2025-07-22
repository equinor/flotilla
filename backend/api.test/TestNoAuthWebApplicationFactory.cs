using System.IO;
using Api.Database.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Test
{
    public class UnauthenticatedWebApplicationFactory<TProgram>(
        string? sqLiteDatabaseName = null,
        string? postgresConnectionString = null
    ) : WebApplicationFactory<Program>
        where TProgram : class
    {
        public IConfiguration? Configuration;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            string projectDir = Directory.GetCurrentDirectory();
            string configPath = Path.Combine(projectDir, "appsettings.Test.json");
            Configuration = new ConfigurationBuilder().AddJsonFile(configPath).Build();
            builder.UseEnvironment("Test");
            builder.ConfigureAppConfiguration(
                (context, config) =>
                {
                    config.AddJsonFile(configPath).AddEnvironmentVariables();
                }
            );
            builder.ConfigureTestServices(services =>
            {
                if (sqLiteDatabaseName != null)
                {
                    string sqlLiteConnectionString = new SqliteConnectionStringBuilder
                    {
                        DataSource = $"file:{sqLiteDatabaseName}?mode=memory",
                        Cache = SqliteCacheMode.Shared,
                    }.ToString();

                    services.AddDbContext<FlotillaDbContext>(options =>
                        options.UseSqlite(
                            sqlLiteConnectionString,
                            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)
                        )
                    );
                }
                else if (postgresConnectionString != null)
                {
                    services.AddDbContext<FlotillaDbContext>(
                        options =>
                            options.UseNpgsql(
                                postgresConnectionString,
                                o =>
                                {
                                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                                    o.EnableRetryOnFailure();
                                }
                            ),
                        ServiceLifetime.Transient
                    );
                }
            });
        }
    }
}
