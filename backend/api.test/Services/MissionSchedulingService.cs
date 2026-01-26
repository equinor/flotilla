using System;
using System.Threading.Tasks;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Services
{
    public class MissionSchedulingServiceTest : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required PostgreSqlContainer Container;
        public required IMissionSchedulingService MissionSchedulingService;

        public required IMissionRunService MissionRunService;

        public async ValueTask InitializeAsync()
        {
            (Container, string connectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);

            DatabaseUtilities = new DatabaseUtilities(
                TestSetupHelpers.ConfigurePostgreSqlContext(connectionString)
            );
            MissionSchedulingService =
                serviceProvider.GetRequiredService<IMissionSchedulingService>();
            MissionRunService = serviceProvider.GetRequiredService<IMissionRunService>();
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }
    }
}
