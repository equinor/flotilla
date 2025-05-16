using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Controllers
{
    public class MissionDefinitionControllerTests : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required PostgreSqlContainer Container;
        public required HttpClient Client;
        public required JsonSerializerOptions SerializerOptions;

        public required IMissionDefinitionService MissionDefinitionService;
        public required ISourceService SourceService;

        public async Task InitializeAsync()
        {
            (Container, string connectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);

            Client = TestSetupHelpers.ConfigureHttpClient(factory);
            SerializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();

            DatabaseUtilities = new DatabaseUtilities(
                TestSetupHelpers.ConfigurePostgreSqlContext(connectionString)
            );

            MissionDefinitionService =
                serviceProvider.GetRequiredService<IMissionDefinitionService>();
            SourceService = serviceProvider.GetRequiredService<ISourceService>();
        }

        public async Task DisposeAsync() => await Task.CompletedTask;

        [Fact]
        public async Task CheckThatListAllMissionDefinitionsEndpointReturnsSuccess()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );

            var source = await SourceService.CreateSourceIfDoesNotExist([]);

            var missionDefinition = new MissionDefinition
            {
                Source = source,
                InstallationCode = installation.InstallationCode,
                Name = "Test Mission Definition",
                InspectionArea = inspectionArea,
                IsDeprecated = false,
            };

            _ = await MissionDefinitionService.Create(missionDefinition);

            // Act
            var response = await Client.GetAsync("missions/definitions");

            // Assert
            var missionDefinitions = await response.Content.ReadFromJsonAsync<
                List<MissionDefinitionResponse>
            >(SerializerOptions);

            Assert.Single(missionDefinitions!);
        }

        [Fact]
        public async Task CheckThatListAllMissionDefinitionsSucceedWhenThereAreNoMissionDefinitions()
        {
            var response = await Client.GetAsync("missions/definitions");

            var missionDefinitions = await response.Content.ReadFromJsonAsync<
                List<MissionDefinitionResponse>
            >(SerializerOptions);

            Assert.Empty(missionDefinitions!);
        }
    }
}
