using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Test.Database;
using Xunit;

namespace Api.Test.Client
{
    public class AreaTests : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required HttpClient Client;
        public required JsonSerializerOptions SerializerOptions;

        public async Task InitializeAsync()
        {
            string databaseName = Guid.NewGuid().ToString();
            (string connectionString, var connection) = await TestSetupHelpers.ConfigureDatabase(
                databaseName
            );
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(databaseName);

            Client = TestSetupHelpers.ConfigureHttpClient(factory);
            SerializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();

            DatabaseUtilities = new DatabaseUtilities(
                TestSetupHelpers.ConfigureFlotillaDbContext(connectionString)
            );
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task AreaTest()
        {
            // Arrange
            var testPose = new Pose
            {
                Position = new Position
                {
                    X = 1,
                    Y = 2,
                    Z = 2,
                },
                Orientation = new Orientation
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                    W = 1,
                },
            };

            string testInstallation = "TestInstallationAreaTest";
            var installationQuery = new CreateInstallationQuery
            {
                InstallationCode = testInstallation,
                Name = testInstallation,
            };

            string testPlant = "TestPlantAreaTest";
            var plantQuery = new CreatePlantQuery
            {
                InstallationCode = testInstallation,
                PlantCode = testPlant,
                Name = testPlant,
            };

            string testInspectionArea = "testInspectionAreaAreaTest";
            var inspectionAreaQuery = new CreateInspectionAreaQuery
            {
                InstallationCode = testInstallation,
                PlantCode = testPlant,
                Name = testInspectionArea,
            };

            string testArea = "testAreaAreaTest";
            var areaQuery = new CreateAreaQuery
            {
                InstallationCode = testInstallation,
                PlantCode = testPlant,
                InspectionAreaName = testInspectionArea,
                AreaName = testArea,
                DefaultLocalizationPose = testPose,
            };

            var installationContent = new StringContent(
                JsonSerializer.Serialize(installationQuery),
                null,
                "application/json"
            );

            var plantContent = new StringContent(
                JsonSerializer.Serialize(plantQuery),
                null,
                "application/json"
            );

            var inspectionAreaContent = new StringContent(
                JsonSerializer.Serialize(inspectionAreaQuery),
                null,
                "application/json"
            );

            var areaContent = new StringContent(
                JsonSerializer.Serialize(areaQuery),
                null,
                "application/json"
            );

            // Act
            string installationUrl = "/installations";
            var installationResponse = await Client.PostAsync(installationUrl, installationContent);
            string plantUrl = "/plants";
            var plantResponse = await Client.PostAsync(plantUrl, plantContent);
            string inspectionAreaUrl = "/inspectionAreas";
            var inspectionAreaResponse = await Client.PostAsync(
                inspectionAreaUrl,
                inspectionAreaContent
            );
            string areaUrl = "/areas";
            var areaResponse = await Client.PostAsync(areaUrl, areaContent);

            // Assert
            Assert.True(installationResponse.IsSuccessStatusCode);
            Assert.True(plantResponse.IsSuccessStatusCode);
            Assert.True(inspectionAreaResponse.IsSuccessStatusCode);
            Assert.True(areaResponse.IsSuccessStatusCode);
            var area = await areaResponse.Content.ReadFromJsonAsync<AreaResponse>(
                SerializerOptions
            );
            Assert.NotNull(area);
        }

        [Fact]
        public async Task MissionIsCreatedInInspectionArea()
        {
            // Arrange - Initialise area
            var installation = await DatabaseUtilities.ReadOrNewInstallation();
            var plant = await DatabaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.ReadOrNewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );

            // Arrange - Robot
            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);
            string robotId = robot.Id;

            string testMissionName = "testMissionInInspectionAreaTest";

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
                RobotId = robotId,
                DesiredStartTime = DateTime.UtcNow,
                InstallationCode = installation.InstallationCode,
                InspectionAreaName = inspectionArea.Name,
                Name = testMissionName,
                Tasks = tasks,
            };

            var missionContent = new StringContent(
                JsonSerializer.Serialize(missionQuery),
                null,
                "application/json"
            );

            // Act
            string missionUrl = "/missions/custom";
            var missionResponse = await Client.PostAsync(missionUrl, missionContent);

            Assert.True(missionResponse.IsSuccessStatusCode);
            var mission = await missionResponse.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );
            Assert.NotNull(mission);
            Assert.NotNull(mission.MissionId);
            string inspectionAreaUrl = "/inspectionAreas";
            var inspectionareaMissionsResponse = await Client.GetAsync(
                inspectionAreaUrl + $"/{inspectionArea.Id}/mission-definitions"
            );

            // Assert
            Assert.True(inspectionareaMissionsResponse.IsSuccessStatusCode);
            var missions = await inspectionareaMissionsResponse.Content.ReadFromJsonAsync<
                IList<MissionDefinitionResponse>
            >(SerializerOptions);
            Assert.NotNull(missions);
            Assert.Single(
                missions.Where(m => m.Id.Equals(mission.MissionId, StringComparison.Ordinal))
            );
        }

        [Fact]
        public async Task EmergencyDockTest()
        {
            // Arrange
            var installation = await DatabaseUtilities.ReadOrNewInstallation();
            string installationCode = installation.InstallationCode;

            // Act
            string goToDockingPositionUrl =
                $"/emergency-action/{installationCode}/abort-current-missions-and-send-all-robots-to-safe-zone";
            var missionResponse = await Client.PostAsync(goToDockingPositionUrl, null);

            // Assert
            Assert.True(missionResponse.IsSuccessStatusCode);

            // The endpoint posted to above triggers an event and returns a successful response.
            // The test finishes and disposes of objects, but the operations of that event handler are still running, leading to a crash.
            await Task.Delay(5000);
        }

        [Fact]
        public async Task UpdateDefaultLocalizationPoseOnInspectionArea()
        {
            // Arrange
            var installation = await DatabaseUtilities.ReadOrNewInstallation();
            var plant = await DatabaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.ReadOrNewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );

            string inspectionAreaId = inspectionArea.Id;

            string url = $"/inspectionAreas/{inspectionAreaId}/update-default-localization-pose";
            var query = new CreateDefaultLocalizationPose
            {
                Pose = new Pose
                {
                    Position = new Position
                    {
                        X = 1,
                        Y = 2,
                        Z = 3,
                    },
                    Orientation = new Orientation
                    {
                        X = 0,
                        Y = 0,
                        Z = 0,
                        W = 1,
                    },
                },
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            var putResponse = await Client.PutAsync(url, content);
            Assert.True(putResponse.IsSuccessStatusCode);
            var putInspectionArea =
                await putResponse.Content.ReadFromJsonAsync<InspectionAreaResponse>(
                    SerializerOptions
                );

            // Assert
            Assert.NotNull(putInspectionArea);
            Assert.NotNull(putInspectionArea.DefaultLocalizationPose);
            Assert.True(
                putInspectionArea.DefaultLocalizationPose.Position.Z.Equals(query.Pose.Position.Z)
            );
            Assert.True(
                putInspectionArea.DefaultLocalizationPose.Orientation.W.Equals(
                    query.Pose.Orientation.W
                )
            );
        }
    }
}
