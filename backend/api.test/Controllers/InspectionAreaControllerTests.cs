using System;
using System.Collections.Generic;
using System.Linq;
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

            InspectionAreaService = serviceProvider.GetRequiredService<IInspectionAreaService>();
            MissionRunService = serviceProvider.GetRequiredService<IMissionRunService>();
            MissionDefinitionService =
                serviceProvider.GetRequiredService<IMissionDefinitionService>();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task CheckThatInspectionAreaIsCorrectlyCreatedThroughEndpoint()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);

            var query = new CreateInspectionAreaQuery()
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
            var response = await Client.PostAsync(Url, content);

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
        public async Task CheckThatMissionDefinitionIsCreatedInInspectionAreaWhenSchedulingACustomMissionRun()
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

            var inspection = new CustomInspectionQuery
            {
                AnalysisType = AnalysisType.CarSeal,
                InspectionTarget = new Position(),
                InspectionType = InspectionType.Image,
            };
            var tasks = new List<CustomTaskQuery>
            {
                new()
                {
                    Inspection = inspection,
                    TagId = "test",
                    RobotPose = new Pose(),
                    TaskOrder = 0,
                },
            };
            var missionQuery = new CustomMissionQuery
            {
                RobotId = robot.Id,
                DesiredStartTime = DateTime.UtcNow,
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
            var missionResponse = await Client.PostAsync("/missions/custom", missionContent);
            var userMissionResponse = await missionResponse.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );
            var inspectionAreaMissionResponse = await Client.GetAsync(
                $"/inspectionAreas/{inspectionArea.Id}/mission-definitions"
            );

            // Assert
            var mission = await MissionRunService.ReadById(userMissionResponse!.Id);
            var missionDefinitions = await MissionDefinitionService.ReadByInspectionAreaId(
                inspectionArea.Id
            );

            Assert.True(missionResponse.IsSuccessStatusCode);
            Assert.True(inspectionAreaMissionResponse.IsSuccessStatusCode);
            Assert.Single(
                missionDefinitions.Where(m =>
                    m.Id.Equals(mission!.MissionId, StringComparison.Ordinal)
                )
            );
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

            var expectedJsonString = await content.ReadAsStringAsync();
            expectedJsonString = expectedJsonString.Replace("\n", "").Replace(" ", "");
            expectedJsonString = expectedJsonString.Replace("\r", "").Replace(" ", "");

            // Act
            var response = await Client.PatchAsync(
                $"/inspectionAreas/{inspectionArea.Id}/area-polygon",
                content
            );
            var inspectionAreaResponse = await response.Content.ReadFromJsonAsync<InspectionArea>(
                SerializerOptions
            );

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(expectedJsonString, inspectionAreaResponse!.AreaPolygonJson!);
        }

        [Fact]
        public async Task ScheduleMissionOutsideInspectionAreaPolygonFails()
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
                content
            );

            Assert.True(response.IsSuccessStatusCode);

            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);

            var inspection = new CustomInspectionQuery
            {
                AnalysisType = AnalysisType.CarSeal,
                InspectionTarget = new Position(),
                InspectionType = InspectionType.Image,
            };
            var tasks = new List<CustomTaskQuery>
            {
                new()
                {
                    Inspection = inspection,
                    TagId = "test",
                    RobotPose = new Pose(11, 11, 11, 0, 0, 0, 1), // Position outside polygon
                    TaskOrder = 0,
                },
            };
            var missionQuery = new CustomMissionQuery
            {
                RobotId = robot.Id,
                DesiredStartTime = DateTime.UtcNow,
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
            var missionResponse = await Client.PostAsync("/missions/custom", missionContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, missionResponse.StatusCode);
        }
    }
}
