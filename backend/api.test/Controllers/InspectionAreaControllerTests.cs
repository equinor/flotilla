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
                CreationTime = DateTime.UtcNow,
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
                "/missions/custom",
                missionContent,
                TestContext.Current.CancellationToken
            );
            var userMissionResponse = await missionResponse.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions,
                cancellationToken: TestContext.Current.CancellationToken
            );
            var inspectionAreaMissionResponse = await Client.GetAsync(
                $"/inspectionAreas/{inspectionArea.Id}/mission-definitions",
                TestContext.Current.CancellationToken
            );

            // Assert
            var mission = await MissionRunService.ReadById(userMissionResponse!.Id);
            var missionDefinitions = await MissionDefinitionService.ReadByInspectionAreaId(
                inspectionArea.Id
            );

            Assert.True(missionResponse.IsSuccessStatusCode);
            Assert.True(inspectionAreaMissionResponse.IsSuccessStatusCode);
            Assert.Single(
                missionDefinitions,
                m => m.Id.Equals(mission!.MissionId, StringComparison.Ordinal)
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
                content,
                TestContext.Current.CancellationToken
            );

            Assert.True(response.IsSuccessStatusCode);

            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);

            var inspection = new CustomInspectionQuery
            {
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
                CreationTime = DateTime.UtcNow,
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
                "/missions/custom",
                missionContent,
                TestContext.Current.CancellationToken
            );

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, missionResponse.StatusCode);
        }
    }
}
