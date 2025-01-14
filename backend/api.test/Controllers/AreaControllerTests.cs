using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Test.Controllers
{
    public class AreaControllerTests : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required HttpClient Client;
        public required JsonSerializerOptions SerializerOptions;

        public required IAreaService AreaService;

        public async Task InitializeAsync()
        {
            string databaseName = Guid.NewGuid().ToString();
            (string connectionString, var connection) = await TestSetupHelpers.ConfigureDatabase(
                databaseName
            );
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(databaseName);
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);

            Client = TestSetupHelpers.ConfigureHttpClient(factory);
            SerializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();

            DatabaseUtilities = new DatabaseUtilities(
                TestSetupHelpers.ConfigureFlotillaDbContext(connectionString)
            );

            AreaService = serviceProvider.GetRequiredService<IAreaService>();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task CheckThatAreaIsCorrectlyCreatedThroughEndpoint()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );

            var query = new CreateAreaQuery
            {
                InstallationCode = installation.InstallationCode,
                PlantCode = plant.PlantCode,
                InspectionAreaName = inspectionArea.Name,
                AreaName = "TestArea",
                DefaultLocalizationPose = new Pose(),
            };

            var areaContent = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            const string AreaUrl = "/areas";
            var areaResponse = await Client.PostAsync(AreaUrl, areaContent);

            // Assert
            var area = await areaResponse.Content.ReadFromJsonAsync<AreaResponse>(
                SerializerOptions
            );

            Assert.True(areaResponse.IsSuccessStatusCode);
            Assert.Equal(query.AreaName, area!.AreaName);
        }

        [Fact]
        public async Task CheckThatAddingDuplicateAreaNameFails()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var area = await DatabaseUtilities.NewArea(
                installation.InstallationCode,
                plant.PlantCode,
                inspectionArea.Name
            );

            var query = new CreateAreaQuery
            {
                InstallationCode = installation.InstallationCode,
                PlantCode = plant.PlantCode,
                InspectionAreaName = inspectionArea.Name,
                AreaName = area.Name,
                DefaultLocalizationPose = new Pose(),
            };

            var areaContent = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            const string AreaUrl = "/areas";
            var response = await Client.PostAsync(AreaUrl, areaContent);

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task CheckThatAddingNonDuplicateAreaNameIsSuccessful()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var _ = await DatabaseUtilities.NewArea(
                installation.InstallationCode,
                plant.PlantCode,
                inspectionArea.Name,
                areaName: "TestArea"
            );

            var areaQuery = new CreateAreaQuery
            {
                InstallationCode = installation.InstallationCode,
                PlantCode = plant.PlantCode,
                InspectionAreaName = inspectionArea.Name,
                AreaName = "MyNameIsNotTestAreaIAmUnique",
                DefaultLocalizationPose = new Pose(),
            };
            var areaContent = new StringContent(
                JsonSerializer.Serialize(areaQuery),
                null,
                "application/json"
            );

            // Act
            var response = await Client.PostAsync("/areas", areaContent);

            // Assert
            var area = await AreaService.ReadByInstallationAndName(
                installation.InstallationCode,
                areaQuery.AreaName
            );

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(areaQuery.AreaName, area!.Name);
        }
    }
}
