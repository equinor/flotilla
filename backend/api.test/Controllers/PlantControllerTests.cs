using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Test.Controllers
{
    public class PlantControllerTests : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required HttpClient Client;

        public required IPlantService PlantService;

        public async Task InitializeAsync()
        {
            string databaseName = Guid.NewGuid().ToString();
            (string connectionString, var connection) =
                await TestSetupHelpers.ConfigureSqLiteDatabase(databaseName);
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(databaseName);
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);

            Client = TestSetupHelpers.ConfigureHttpClient(factory);
            DatabaseUtilities = new DatabaseUtilities(
                TestSetupHelpers.ConfigureSqLiteContext(connectionString)
            );

            PlantService = serviceProvider.GetRequiredService<IPlantService>();
        }

        public Task DisposeAsync() => Task.CompletedTask;

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
            var response = await Client.PostAsync(Url, content);

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
