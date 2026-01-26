using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Controllers
{
    public class PlantControllerTests : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required PostgreSqlContainer Container;
        public required HttpClient Client;

        public required IPlantService PlantService;

        public async ValueTask InitializeAsync()
        {
            (Container, string connectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);

            Client = TestSetupHelpers.ConfigureHttpClient(factory);
            DatabaseUtilities = new DatabaseUtilities(
                TestSetupHelpers.ConfigurePostgreSqlContext(connectionString)
            );

            PlantService = serviceProvider.GetRequiredService<IPlantService>();
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        [Fact]
        public async Task CheckThatPlantIsCorrectlyCreatedThroughEndpoint()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();

            var query = new CreatePlantQuery
            {
                InstallationCode = installation.InstallationCode,
                PlantCode = "plantCode",
                Name = "plant",
            };

            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            const string Url = "/plants";
            var response = await Client.PostAsync(
                Url,
                content,
                TestContext.Current.CancellationToken
            );

            // Assert
            var plant = await PlantService.ReadByInstallationAndPlantCode(
                installation,
                query.PlantCode
            );

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(query.Name, plant!.Name);
        }
    }
}
