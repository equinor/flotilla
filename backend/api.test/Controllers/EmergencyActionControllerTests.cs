using System.Net.Http;
using System.Threading.Tasks;
using Api.Test.Database;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Controllers
{
    public class EmergencyActionControllerTests : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required PostgreSqlContainer Container;
        public required HttpClient Client;

        public async Task InitializeAsync()
        {
            (Container, string connectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );
            Client = TestSetupHelpers.ConfigureHttpClient(factory);

            DatabaseUtilities = new DatabaseUtilities(
                TestSetupHelpers.ConfigurePostgreSqlContext(connectionString)
            );
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task EmergencyDockTest()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            string installationCode = installation.InstallationCode;

            // Act
            string goToDockingPositionUrl =
                $"/emergency-action/{installationCode}/abort-current-missions-and-send-all-robots-to-safe-zone";
            var missionResponse = await Client.PostAsync(goToDockingPositionUrl, null);

            // Assert
            Assert.True(missionResponse.IsSuccessStatusCode);
        }
    }
}
