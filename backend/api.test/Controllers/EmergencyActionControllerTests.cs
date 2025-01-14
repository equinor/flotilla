using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Test.Controllers
{
    public class EmergencyActionControllerTests : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required HttpClient Client;

        public async Task InitializeAsync()
        {
            string databaseName = Guid.NewGuid().ToString();
            (string connectionString, var connection) = await TestSetupHelpers.ConfigureDatabase(
                databaseName
            );
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(databaseName);
            Client = TestSetupHelpers.ConfigureHttpClient(factory);

            DatabaseUtilities = new DatabaseUtilities(
                TestSetupHelpers.ConfigureFlotillaDbContext(connectionString)
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
