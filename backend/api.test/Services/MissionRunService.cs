using System;
using System.Threading.Tasks;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Services
{
    public class MissionServiceTest : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required PostgreSqlContainer Container;
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
            MissionRunService = serviceProvider.GetRequiredService<IMissionRunService>();
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        [Fact]
        public async Task CheckThatReadByIdWithUnknownIdFails()
        {
            var missionRun = await MissionRunService.ReadById("IDoNotExist", readOnly: true);
            Assert.Null(missionRun);
        }
    }
}
