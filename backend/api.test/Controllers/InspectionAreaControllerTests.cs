using System;
using System.Collections.Generic;
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
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Controllers
{
    public class InspectionAreaControllerTests : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required PostgreSqlContainer Container;
        public required HttpClient Client;
        public required JsonSerializerOptions SerializerOptions;

        public required IInspectionAreaService InspectionAreaService;
        public required IMissionRunService MissionRunService;
        public required IMissionDefinitionService MissionDefinitionService;

        public async ValueTask InitializeAsync()
        {
            (Container, string connectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);

            Client = TestSetupHelpers.ConfigureHttpClient(factory);
            SerializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();

            DatabaseUtilities = serviceProvider.GetRequiredService<DatabaseUtilities>();

            InspectionAreaService = serviceProvider.GetRequiredService<IInspectionAreaService>();
            MissionRunService = serviceProvider.GetRequiredService<IMissionRunService>();
            MissionDefinitionService =
                serviceProvider.GetRequiredService<IMissionDefinitionService>();
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        [Fact]
        public async Task CheckThatInspectionAreaIsCorrectlyCreatedThroughEndpoint()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);

            var query = new CreateInspectionAreaQuery
            {
                InstallationCode = installation.InstallationCode,
                PlantCode = plant.PlantCode,
                Name = "inspectionArea",
            };

            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            const string Url = "/inspectionAreas";
            var response = await Client.PostAsync(
                Url,
                content,
                TestContext.Current.CancellationToken
            );

            // Assert
            var inspectionArea = await InspectionAreaService.ReadByInstallationAndPlantAndName(
                installation,
                plant,
                query.Name
            );

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(query.Name, inspectionArea!.Name);
        }

        [Fact]
        public async Task CheckThatMissionDefinitionIsCreatedInInspectionAreaUponCreation()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var robot = await DatabaseUtilities.NewRobot(
                RobotStatus.Available,
                installation,
                inspectionArea.Id
            );

            var testName = Guid.NewGuid().ToString();

            var tasks = new List<TaskQuery>
            {
                new()
                {
                    TagId = "test",
                    RobotPose = new Pose(),
                    TargetPosition = new Position(),
                    SensorType = SensorType.Image,
                    AnalysisTypes = [AnalysisType.Fencilla],
                    Description = "Test description",
                },
            };
            var missionQuery = new CreateMissionQuery
            {
                InstallationCode = installation.InstallationCode,
                Name = testName,
                Tasks = tasks,
            };

            var missionContent = new StringContent(
                JsonSerializer.Serialize(missionQuery),
                null,
                "application/json"
            );

            // Act
            var missionResponse = await Client.PostAsync(
                "/missions/definitions",
                missionContent,
                TestContext.Current.CancellationToken
            );
            var inspectionAreaMissionResponse = await Client.GetAsync(
                $"/inspectionAreas/{inspectionArea.Id}/mission-definitions",
                TestContext.Current.CancellationToken
            );

            // Assert
            var missionDefinitions = await MissionDefinitionService.ReadByInspectionAreaId(
                inspectionArea.Id
            );

            Assert.True(missionResponse.IsSuccessStatusCode);
            Assert.NotNull(missionDefinitions.Find((m) => m.Name == testName));
            Assert.True(inspectionAreaMissionResponse.IsSuccessStatusCode);
        }

        [Fact]
        public async Task TestUpdatingInspectionAreaPolygon()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );

            var areaPolygon = new AreaPolygon
            {
                ZMin = 0,
                ZMax = 10,
                Positions =
                [
                    new PolygonPoint { X = 0, Y = 0 },
                    new PolygonPoint { X = 0, Y = 10 },
                    new PolygonPoint { X = 10, Y = 10 },
                    new PolygonPoint { X = 10, Y = 0 },
                ],
            };

            var areaPolygonJson = JsonSerializer.Serialize(areaPolygon);

            var content = new StringContent(areaPolygonJson, null, "application/json");

            // Act
            var response = await Client.PatchAsync(
                $"/inspectionAreas/{inspectionArea.Id}/area-polygon",
                content,
                TestContext.Current.CancellationToken
            );
            var inspectionAreaResponse = await response.Content.ReadFromJsonAsync<InspectionArea>(
                SerializerOptions,
                cancellationToken: TestContext.Current.CancellationToken
            );

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(
                areaPolygonJson,
                JsonSerializer.Serialize(inspectionAreaResponse!.AreaPolygon!)
            );
        }

        [Fact]
        public async Task CreateMissionDefinitionOutsideInspectionAreaPolygonFails()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var jsonString =
                @"{
                    ""zmin"": 0,
                    ""zmax"": 10,
                    ""positions"": [
                        { ""x"": 0, ""y"": 0 },
                        { ""x"": 0, ""y"": 10 },
                        { ""x"": 10, ""y"": 10 },
                        { ""x"": 10, ""y"": 0 }
                    ]
                }";

            var content = new StringContent(jsonString, null, "application/json");
            var response = await Client.PatchAsync(
                $"/inspectionAreas/{inspectionArea.Id}/area-polygon",
                content,
                TestContext.Current.CancellationToken
            );

            Assert.True(response.IsSuccessStatusCode);

            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);

            var tasks = new List<TaskQuery>
            {
                new()
                {
                    TagId = "test",
                    TargetPosition = new Position(),
                    SensorType = SensorType.Image,
                    AnalysisTypes = [AnalysisType.Fencilla],
                    RobotPose = new Pose(11, 11, 11, 0, 0, 0, 1), // Position outside polygon
                },
            };
            var missionQuery = new CreateMissionQuery
            {
                InstallationCode = installation.InstallationCode,
                Name = "TestMission",
                Tasks = tasks,
            };

            var missionContent = new StringContent(
                JsonSerializer.Serialize(missionQuery),
                null,
                "application/json"
            );

            // Act
            var missionResponse = await Client.PostAsync(
                "/missions/definitions",
                missionContent,
                TestContext.Current.CancellationToken
            );

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, missionResponse.StatusCode);
        }
    }
}
