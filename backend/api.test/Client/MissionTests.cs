using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Test.Database;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
namespace Api.Test.Client
{
    [Collection("Database collection")]
    public class MissionTests : IClassFixture<TestWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly DatabaseUtilities _databaseUtilities;
        private readonly JsonSerializerOptions _serializerOptions =
            new()
            {
                Converters =
                {
                    new JsonStringEnumConverter()
                },
                PropertyNameCaseInsensitive = true
            };

        public MissionTests(TestWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost:8000")
            });
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                TestAuthHandler.AuthenticationScheme
            );

            object? context = factory.Services.GetService(typeof(FlotillaDbContext)) as FlotillaDbContext ?? throw new ArgumentNullException(nameof(factory));
            _databaseUtilities = new DatabaseUtilities((FlotillaDbContext)context);
        }

        [Fact]
        public async Task ScheduleOneMissionTest()
        {
            // Arrange - Area
            var installation = await _databaseUtilities.ReadOrNewInstallation();

            // Arrange - Robot
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Busy, installation);
            string robotId = robot.Id;

            string missionsUrl = "/missions";
            string missionSourceId = "95";

            // Act
            var query = new ScheduledMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installation.InstallationCode,
                MissionSourceId = missionSourceId,
                DesiredStartTime = DateTime.UtcNow
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            var response = await _client.PostAsync(missionsUrl, content);

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var missionRun = await response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.NotNull(missionRun);
            Assert.NotNull(missionRun.Id);
            Assert.Equal(MissionStatus.Pending, missionRun.Status);
        }

        [Fact]
        public async Task Schedule3MissionsTest()
        {
            // Arrange - Area
            var installation = await _databaseUtilities.ReadOrNewInstallation();

            // Arrange - Robot
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Busy, installation);
            string robotId = robot.Id;

            string missionSourceId = "97";

            // Act
            var query = new ScheduledMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installation.InstallationCode,
                MissionSourceId = missionSourceId,
                DesiredStartTime = DateTime.UtcNow
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Increasing pageSize to 50 to ensure the missions we are looking for is included
            string urlMissionRuns = "/missions/runs?pageSize=50";
            var response = await _client.GetAsync(urlMissionRuns);
            var missionRuns = await response.Content.ReadFromJsonAsync<List<MissionRun>>(
                _serializerOptions
            );
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(missionRuns);
            int missionRunsBefore = missionRuns.Count;

            string missionsUrl = "/missions";
            response = await _client.PostAsync(missionsUrl, content);

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            response = await _client.PostAsync(missionsUrl, content);
            var missionRun1 = await response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(missionRun1);

            response = await _client.PostAsync(missionsUrl, content);
            var missionRun2 = await response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(missionRun2);

            response = await _client.PostAsync(missionsUrl, content);
            var missionRun3 = await response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(missionRun3);

            response = await _client.GetAsync(urlMissionRuns);
            missionRuns = await response.Content.ReadFromJsonAsync<List<MissionRun>>(
                _serializerOptions
            );

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(missionRuns);
            Assert.Single(missionRuns.Where((m) => m.Id == missionRun1.Id).ToList());
            Assert.Single(missionRuns.Where((m) => m.Id == missionRun2.Id).ToList());
            Assert.Single(missionRuns.Where((m) => m.Id == missionRun3.Id).ToList());
        }

        [Fact]
        public async Task AddNonDuplicateAreasToDb()
        {
            // Arrange
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await _databaseUtilities.ReadOrNewInspectionArea(installation.InstallationCode, plant.PlantCode);
            var _ = await _databaseUtilities.ReadOrNewArea(installation.InstallationCode, plant.PlantCode, inspectionArea.Name);

            var testPose = new Pose
            {
                Position = new Position
                {
                    X = 1,
                    Y = 2,
                    Z = 2
                },
                Orientation = new Orientation
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                    W = 1
                }
            };
            var areaQuery = new CreateAreaQuery
            {
                InstallationCode = installation.InstallationCode,
                PlantCode = plant.PlantCode,
                InspectionAreaName = inspectionArea.Name,
                AreaName = "AddNonDuplicateAreasToDb_Area",
                DefaultLocalizationPose = testPose
            };
            var areaContent = new StringContent(
                JsonSerializer.Serialize(areaQuery),
                null,
                "application/json"
            );
            string areaUrl = "/areas";
            var response = await _client.PostAsync(areaUrl, areaContent);
            Assert.True(response.IsSuccessStatusCode, $"Failed to post to {areaUrl}. Status code: {response.StatusCode}");

            Assert.True(response != null, $"Failed to post to {areaUrl}. Null returned");
            var responseObject = await response.Content.ReadFromJsonAsync<AreaResponse>(_serializerOptions);
            Assert.True(responseObject != null, $"No object returned from post to {areaUrl}");
        }

        [Fact]
        public async Task AddDuplicateAreasToDb_Fails()
        {
            // Arrange
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await _databaseUtilities.ReadOrNewInspectionArea(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.ReadOrNewArea(installation.InstallationCode, plant.PlantCode, inspectionArea.Name);

            var testPose = new Pose
            {
                Position = new Position
                {
                    X = 1,
                    Y = 2,
                    Z = 2
                },
                Orientation = new Orientation
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                    W = 1
                }
            };
            var areaQuery = new CreateAreaQuery
            {
                InstallationCode = installation.InstallationCode,
                PlantCode = plant.PlantCode,
                InspectionAreaName = inspectionArea.Name,
                AreaName = area.Name,
                DefaultLocalizationPose = testPose
            };
            var areaContent = new StringContent(
                JsonSerializer.Serialize(areaQuery),
                null,
                "application/json"
            );
            string areaUrl = "/areas";
            var response = await _client.PostAsync(areaUrl, areaContent);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task GetMissionById_ShouldReturnNotFound()
        {
            string missionId = "RandomString";
            string url = "/missions/runs/" + missionId;
            var response = await _client.GetAsync(url);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteMission_ShouldReturnNotFound()
        {
            string missionId = "RandomString";
            string url = "/missions/runs/" + missionId;
            var response = await _client.DeleteAsync(url);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ScheduleDuplicateCustomMissionDefinitions()
        {
            // Arrange
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await _databaseUtilities.ReadOrNewInspectionArea(installation.InstallationCode, plant.PlantCode);

            string testMissionName = "testMissionScheduleDuplicateCustomMissionDefinitions";

            // Arrange - Robot
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Busy, installation);
            string robotId = robot.Id;

            // Arrange - Create custom mission definition
            var query = new CustomMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installation.InstallationCode,
                InspectionAreaName = inspectionArea.Name,
                DesiredStartTime = DateTime.SpecifyKind(new DateTime(3050, 1, 1), DateTimeKind.Utc),
                InspectionFrequency = new TimeSpan(14, 0, 0, 0),
                Name = testMissionName,
                Tasks = [
                    new()
                    {
                        RobotPose = new Pose(new Position(23, 14, 4), new Orientation()),
                        Inspection = new CustomInspectionQuery
                        {
                            InspectionTarget = new Position(),
                            InspectionType = InspectionType.Image
                        },
                        TaskOrder = 0
                    },
                    new()
                    {
                        RobotPose = new Pose(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f),
                        Inspection = new CustomInspectionQuery
                        {
                            InspectionTarget = new Position(),
                            InspectionType = InspectionType.Image
                        },
                        TaskOrder = 1
                    }
                ]
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            string customMissionsUrl = "/missions/custom";
            var response1 = await _client.PostAsync(customMissionsUrl, content);
            var response2 = await _client.PostAsync(customMissionsUrl, content);

            // Assert
            Assert.True(response1.IsSuccessStatusCode);
            Assert.True(response2.IsSuccessStatusCode);
            var missionRun1 = await response1.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            var missionRun2 = await response2.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.NotNull(missionRun1);
            Assert.NotNull(missionRun2);
            string? missionId1 = missionRun1.MissionId;
            string? missionId2 = missionRun2.MissionId;
            Assert.Equal(missionId1, missionId2);
            // Increasing pageSize to 50 to ensure the missions we are looking for is included
            string missionDefinitionsUrl = "/missions/definitions?pageSize=50";
            var missionDefinitionsResponse = await _client.GetAsync(missionDefinitionsUrl);
            var missionDefinitions = await missionDefinitionsResponse.Content.ReadFromJsonAsync<List<MissionDefinition>>(_serializerOptions);
            Assert.NotNull(missionDefinitions);
            Assert.Single(missionDefinitions.Where(m => m.Id == missionId1));
        }

        [Fact]
        public async Task GetNextRun()
        {
            // Arrange - Initialise area
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await _databaseUtilities.ReadOrNewInspectionArea(installation.InstallationCode, plant.PlantCode);

            // Arrange - Robot
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation);
            string robotId = robot.Id;

            // Arrange - Schedule custom mission - create mission definition
            string testMissionName = "testMissionNextRun";
            var query = new CustomMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installation.InstallationCode,
                InspectionAreaName = inspectionArea.Name,
                DesiredStartTime = DateTime.SpecifyKind(new DateTime(3050, 1, 1), DateTimeKind.Utc),
                InspectionFrequency = new TimeSpan(14, 0, 0, 0),
                Name = testMissionName,
                Tasks = [
                    new()
                    {
                        RobotPose = new Pose(),
                        Inspection = new CustomInspectionQuery
                        {
                            InspectionTarget = new Position(),
                            InspectionType = InspectionType.Image
                        },
                        TaskOrder = 0
                    }
                ]
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            string customMissionsUrl = "/missions/custom";
            var response = await _client.PostAsync(customMissionsUrl, content);
            Assert.True(response.IsSuccessStatusCode);
            var missionRun = await response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.NotNull(missionRun);
            Assert.NotNull(missionRun.MissionId);
            Assert.NotNull(missionRun.Id);
            Assert.Equal(MissionStatus.Pending, missionRun.Status);

            // Arrange - Schedule missions from mission definition
            var scheduleQuery1 = new ScheduleMissionQuery
            {
                RobotId = robotId,
                DesiredStartTime = DateTime.SpecifyKind(new DateTime(2050, 1, 1), DateTimeKind.Utc),
            };
            var scheduleContent1 = new StringContent(
                JsonSerializer.Serialize(scheduleQuery1),
                null,
                "application/json"
            );
            var scheduleQuery2 = new ScheduleMissionQuery
            {
                RobotId = robotId,
                DesiredStartTime = DateTime.UtcNow,
            };
            var scheduleContent2 = new StringContent(
                JsonSerializer.Serialize(scheduleQuery2),
                null,
                "application/json"
            );
            var scheduleQuery3 = new ScheduleMissionQuery
            {
                RobotId = robotId,
                DesiredStartTime = DateTime.SpecifyKind(new DateTime(2100, 1, 1), DateTimeKind.Utc),
            };
            var scheduleContent3 = new StringContent(
                JsonSerializer.Serialize(scheduleQuery3),
                null,
                "application/json"
            );
            string scheduleMissionsUrl = $"/missions/schedule/{missionRun.MissionId}";
            var missionRun1Response = await _client.PostAsync(scheduleMissionsUrl, scheduleContent1);
            var missionRun2Response = await _client.PostAsync(scheduleMissionsUrl, scheduleContent2);
            var missionRun3Response = await _client.PostAsync(scheduleMissionsUrl, scheduleContent3);
            var missionRun1 = await missionRun1Response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            var missionRun2 = await missionRun2Response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            var missionRun3 = await missionRun3Response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);

            // Act
            string nextMissionUrl = $"missions/definitions/{missionRun.MissionId}/next-run";
            var nextMissionResponse = await _client.GetAsync(nextMissionUrl);

            // Assert
            Assert.True(nextMissionResponse.IsSuccessStatusCode);
            var nextMissionRun = await nextMissionResponse.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.NotNull(nextMissionRun);
            Assert.NotNull(missionRun1);
            Assert.NotNull(missionRun2);
            Assert.NotNull(missionRun3);
            Assert.Equal(missionRun1.MissionId, missionRun.MissionId);
            Assert.Equal(missionRun2.MissionId, missionRun.MissionId);
            Assert.Equal(missionRun3.MissionId, missionRun.MissionId);
            Assert.True(nextMissionRun.Id == missionRun2.Id);
        }

        [Fact]
        public async Task ScheduleDuplicatMissionDefinitions()
        {
            // Arrange - Initialise area
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await _databaseUtilities.ReadOrNewInspectionArea(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.ReadOrNewArea(installation.InstallationCode, plant.PlantCode, inspectionArea.Name);

            // Arrange - Robot
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation);
            string robotId = robot.Id;

            string missionSourceId = "986";
            var source = await _databaseUtilities.NewSource(missionSourceId);

            var query = new ScheduledMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installation.InstallationCode,
                MissionSourceId = missionSourceId,
                DesiredStartTime = DateTime.UtcNow
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            string missionsUrl = "/missions";
            var response1 = await _client.PostAsync(missionsUrl, content);
            var response2 = await _client.PostAsync(missionsUrl, content);

            // Assert
            Assert.True(response1.IsSuccessStatusCode);
            Assert.True(response2.IsSuccessStatusCode);
            var missionRun1 = await response1.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            var missionRun2 = await response2.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.NotNull(missionRun1);
            Assert.NotNull(missionRun2);
            string? missionId1 = missionRun1.MissionId;
            string? missionId2 = missionRun2.MissionId;
            Assert.Equal(missionId1, missionId2);
            string missionDefinitionsUrl = "/missions/definitions?pageSize=50";
            var missionDefinitionsResponse = await _client.GetAsync(missionDefinitionsUrl);
            var missionDefinitions = await missionDefinitionsResponse.Content.ReadFromJsonAsync<List<MissionDefinition>>(_serializerOptions);
            Assert.NotNull(missionDefinitions);
            Assert.NotNull(missionDefinitions.Find(m => m.Id == missionId1));
        }

        [Fact]
        public async Task MissionDoesNotStartIfRobotIsNotInSameInstallationAsMission()
        {
            // Arrange - Initialise area
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await _databaseUtilities.ReadOrNewInspectionArea(installation.InstallationCode, plant.PlantCode);

            string testMissionName = "testMissionDoesNotStartIfRobotIsNotInSameInstallationAsMission";

            // Arrange - Get different installation
            string otherInstallationCode = "installationMissionDoesNotStartIfRobotIsNotInSameInstallationAsMission_Other";
            var otherInstallation = await _databaseUtilities.NewInstallation(otherInstallationCode);

            // Arrange - Robot
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, otherInstallation);
            string robotId = robot.Id;

            // Arrange - Create custom mission definition
            var query = new CustomMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installation.InstallationCode,
                InspectionAreaName = inspectionArea.Name,
                DesiredStartTime = DateTime.SpecifyKind(new DateTime(3050, 1, 1), DateTimeKind.Utc),
                InspectionFrequency = new TimeSpan(14, 0, 0, 0),
                Name = testMissionName,
                Tasks = [
                    new()
                    {
                        RobotPose = new Pose(),
                        Inspection = new CustomInspectionQuery
                        {
                            InspectionTarget = new Position(),
                            InspectionType = InspectionType.Image
                        },
                        TaskOrder = 0
                    },
                    new()
                    {
                        RobotPose = new Pose(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f),
                        Inspection = new CustomInspectionQuery
                        {
                            InspectionTarget = new Position(),
                            InspectionType = InspectionType.Image
                        },
                        TaskOrder = 1
                    }
                ]
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            string customMissionsUrl = "/missions/custom";
            var response = await _client.PostAsync(customMissionsUrl, content);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task MissionFailsIfRobotIsNotInSameInspectionAreaAsMission()
        {
            // Arrange - Initialise area
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);

            string inspectionAreaName1 = "inspectionAreaMissionFailsIfRobotIsNotInSameInspectionAreaAsMission1";
            var inspectionArea1 = await _databaseUtilities.NewInspectionArea(installation.InstallationCode, plant.PlantCode, inspectionAreaName1);

            string inspectionAreaName2 = "inspectionAreaMissionFailsIfRobotIsNotInSameInspectionAreaAsMission2";
            var inspectionArea2 = await _databaseUtilities.NewInspectionArea(installation.InstallationCode, plant.PlantCode, inspectionAreaName2);

            string testMissionName = "testMissionFailsIfRobotIsNotInSameInspectionAreaAsMission";

            // Arrange - Robot
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, inspectionArea1);
            string robotId = robot.Id;

            // Arrange - Mission Run Query
            var query = new CustomMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installation.InstallationCode,
                InspectionAreaName = inspectionArea2.Name,
                DesiredStartTime = DateTime.SpecifyKind(new DateTime(3050, 1, 1), DateTimeKind.Utc),
                InspectionFrequency = new TimeSpan(14, 0, 0, 0),
                Name = testMissionName,
                Tasks = [
                    new()
                    {
                        RobotPose = new Pose(new Position(1, 9, 4), new Orientation()),
                        Inspection = new CustomInspectionQuery
                        {
                            InspectionTarget = new Position(),
                            InspectionType = InspectionType.Image
                        },
                        TaskOrder = 0
                    },
                    new()
                    {
                        RobotPose = new Pose(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f),
                        Inspection = new CustomInspectionQuery
                        {
                            InspectionTarget = new Position(),
                            InspectionType = InspectionType.Image
                        },
                        TaskOrder = 1
                    }
                ]
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            string customMissionsUrl = "/missions/custom";
            var missionResponse = await _client.PostAsync(customMissionsUrl, content);
            Assert.Equal(HttpStatusCode.Conflict, missionResponse.StatusCode);
        }
    }
}
