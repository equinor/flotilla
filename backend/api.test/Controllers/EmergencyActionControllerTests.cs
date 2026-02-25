using System;
using System.Net.Http;
using System.Threading.Tasks;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Controllers
{
    public class EmergencyActionControllerTests : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required PostgreSqlContainer Container;
        public required HttpClient Client;

        public async ValueTask InitializeAsync()
        {
            (Container, string connectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );
            Client = TestSetupHelpers.ConfigureHttpClient(factory);

            DatabaseUtilities = factory.Services.GetRequiredService<DatabaseUtilities>();
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        [Fact]
        public async Task EmergencyDockTest()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            string installationCode = installation.InstallationCode;

            // Act
            string goToDockingPositionUrl =
                $"/emergency-action/{installationCode}/abort-current-missions-and-send-all-robots-to-safe-zone";
            var missionResponse = await Client.PostAsync(
                goToDockingPositionUrl,
                null,
                TestContext.Current.CancellationToken
            );

            // Assert
            Assert.True(missionResponse.IsSuccessStatusCode);
        }
    }
}
