﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public class MissionTests : IAsyncLifetime
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
        public async Task ScheduleOneMissionTest()
        {
            // Arrange - Area
            var installation = await DatabaseUtilities.ReadOrNewInstallation();

            // Arrange - Robot
            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Busy, installation);
            string robotId = robot.Id;

            string missionsUrl = "/missions";
            string missionSourceId = "95";

            // Act
            var query = new ScheduledMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installation.InstallationCode,
                MissionSourceId = missionSourceId,
                DesiredStartTime = DateTime.UtcNow,
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            var response = await Client.PostAsync(missionsUrl, content);

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var missionRun = await response.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );
            Assert.NotNull(missionRun);
            Assert.NotNull(missionRun.Id);
            Assert.Equal(MissionStatus.Pending, missionRun.Status);
        }

        [Fact]
        public async Task Schedule3MissionsTest()
        {
            // Arrange - Area
            var installation = await DatabaseUtilities.ReadOrNewInstallation();

            // Arrange - Robot
            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Busy, installation);
            string robotId = robot.Id;

            string missionSourceId = "97";

            // Act
            var query = new ScheduledMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installation.InstallationCode,
                MissionSourceId = missionSourceId,
                DesiredStartTime = DateTime.UtcNow,
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Increasing pageSize to 50 to ensure the missions we are looking for is included
            string urlMissionRuns = "/missions/runs?pageSize=50";
            var response = await Client.GetAsync(urlMissionRuns);
            var missionRuns = await response.Content.ReadFromJsonAsync<List<MissionRun>>(
                SerializerOptions
            );
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(missionRuns);
            int missionRunsBefore = missionRuns.Count;

            string missionsUrl = "/missions";
            response = await Client.PostAsync(missionsUrl, content);

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            response = await Client.PostAsync(missionsUrl, content);
            var missionRun1 = await response.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(missionRun1);

            response = await Client.PostAsync(missionsUrl, content);
            var missionRun2 = await response.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(missionRun2);

            response = await Client.PostAsync(missionsUrl, content);
            var missionRun3 = await response.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(missionRun3);

            response = await Client.GetAsync(urlMissionRuns);
            missionRuns = await response.Content.ReadFromJsonAsync<List<MissionRun>>(
                SerializerOptions
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
            var installation = await DatabaseUtilities.ReadOrNewInstallation();
            var plant = await DatabaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.ReadOrNewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var _ = await DatabaseUtilities.ReadOrNewArea(
                installation.InstallationCode,
                plant.PlantCode,
                inspectionArea.Name
            );

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
            var areaQuery = new CreateAreaQuery
            {
                InstallationCode = installation.InstallationCode,
                PlantCode = plant.PlantCode,
                InspectionAreaName = inspectionArea.Name,
                AreaName = "AddNonDuplicateAreasToDb_Area",
                DefaultLocalizationPose = testPose,
            };
            var areaContent = new StringContent(
                JsonSerializer.Serialize(areaQuery),
                null,
                "application/json"
            );
            string areaUrl = "/areas";
            var response = await Client.PostAsync(areaUrl, areaContent);
            Assert.True(
                response.IsSuccessStatusCode,
                $"Failed to post to {areaUrl}. Status code: {response.StatusCode}"
            );

            Assert.True(response != null, $"Failed to post to {areaUrl}. Null returned");
            var responseObject = await response.Content.ReadFromJsonAsync<AreaResponse>(
                SerializerOptions
            );
            Assert.True(responseObject != null, $"No object returned from post to {areaUrl}");
        }

        [Fact]
        public async Task AddDuplicateAreasToDb_Fails()
        {
            // Arrange
            var installation = await DatabaseUtilities.ReadOrNewInstallation();
            var plant = await DatabaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.ReadOrNewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var area = await DatabaseUtilities.ReadOrNewArea(
                installation.InstallationCode,
                plant.PlantCode,
                inspectionArea.Name
            );

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
            var areaQuery = new CreateAreaQuery
            {
                InstallationCode = installation.InstallationCode,
                PlantCode = plant.PlantCode,
                InspectionAreaName = inspectionArea.Name,
                AreaName = area.Name,
                DefaultLocalizationPose = testPose,
            };
            var areaContent = new StringContent(
                JsonSerializer.Serialize(areaQuery),
                null,
                "application/json"
            );
            string areaUrl = "/areas";
            var response = await Client.PostAsync(areaUrl, areaContent);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task GetMissionById_ShouldReturnNotFound()
        {
            string missionId = "RandomString";
            string url = "/missions/runs/" + missionId;
            var response = await Client.GetAsync(url);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteMission_ShouldReturnNotFound()
        {
            string missionId = "RandomString";
            string url = "/missions/runs/" + missionId;
            var response = await Client.DeleteAsync(url);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ScheduleDuplicateCustomMissionDefinitions()
        {
            // Arrange
            var installation = await DatabaseUtilities.ReadOrNewInstallation();
            var plant = await DatabaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.ReadOrNewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );

            string testMissionName = "testMissionScheduleDuplicateCustomMissionDefinitions";

            // Arrange - Robot
            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Busy, installation);
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
                Tasks =
                [
                    new()
                    {
                        RobotPose = new Pose(new Position(23, 14, 4), new Orientation()),
                        Inspection = new CustomInspectionQuery
                        {
                            InspectionTarget = new Position(),
                            InspectionType = InspectionType.Image,
                        },
                        TaskOrder = 0,
                    },
                    new()
                    {
                        RobotPose = new Pose(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f),
                        Inspection = new CustomInspectionQuery
                        {
                            InspectionTarget = new Position(),
                            InspectionType = InspectionType.Image,
                        },
                        TaskOrder = 1,
                    },
                ],
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            string customMissionsUrl = "/missions/custom";
            var response1 = await Client.PostAsync(customMissionsUrl, content);
            var response2 = await Client.PostAsync(customMissionsUrl, content);

            // Assert
            Assert.True(response1.IsSuccessStatusCode);
            Assert.True(response2.IsSuccessStatusCode);
            var missionRun1 = await response1.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );
            var missionRun2 = await response2.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );
            Assert.NotNull(missionRun1);
            Assert.NotNull(missionRun2);
            string? missionId1 = missionRun1.MissionId;
            string? missionId2 = missionRun2.MissionId;
            Assert.Equal(missionId1, missionId2);
            // Increasing pageSize to 50 to ensure the missions we are looking for is included
            string missionDefinitionsUrl = "/missions/definitions?pageSize=50";
            var missionDefinitionsResponse = await Client.GetAsync(missionDefinitionsUrl);
            var missionDefinitions = await missionDefinitionsResponse.Content.ReadFromJsonAsync<
                List<MissionDefinition>
            >(SerializerOptions);
            Assert.NotNull(missionDefinitions);
            Assert.Single(missionDefinitions.Where(m => m.Id == missionId1));
        }

        [Fact]
        public async Task GetNextRun()
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
                Tasks =
                [
                    new()
                    {
                        RobotPose = new Pose(),
                        Inspection = new CustomInspectionQuery
                        {
                            InspectionTarget = new Position(),
                            InspectionType = InspectionType.Image,
                        },
                        TaskOrder = 0,
                    },
                ],
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            string customMissionsUrl = "/missions/custom";
            var response = await Client.PostAsync(customMissionsUrl, content);
            Assert.True(response.IsSuccessStatusCode);
            var missionRun = await response.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );
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
            var missionRun1Response = await Client.PostAsync(scheduleMissionsUrl, scheduleContent1);
            var missionRun2Response = await Client.PostAsync(scheduleMissionsUrl, scheduleContent2);
            var missionRun3Response = await Client.PostAsync(scheduleMissionsUrl, scheduleContent3);
            var missionRun1 = await missionRun1Response.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );
            var missionRun2 = await missionRun2Response.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );
            var missionRun3 = await missionRun3Response.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );

            // Act
            string nextMissionUrl = $"missions/definitions/{missionRun.MissionId}/next-run";
            var nextMissionResponse = await Client.GetAsync(nextMissionUrl);

            // Assert
            Assert.True(nextMissionResponse.IsSuccessStatusCode);
            var nextMissionRun = await nextMissionResponse.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );
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
            var installation = await DatabaseUtilities.ReadOrNewInstallation();
            var plant = await DatabaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.ReadOrNewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var area = await DatabaseUtilities.ReadOrNewArea(
                installation.InstallationCode,
                plant.PlantCode,
                inspectionArea.Name
            );

            // Arrange - Robot
            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);
            string robotId = robot.Id;

            string missionSourceId = "986";
            var source = await DatabaseUtilities.NewSource(missionSourceId);

            var query = new ScheduledMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installation.InstallationCode,
                MissionSourceId = missionSourceId,
                DesiredStartTime = DateTime.UtcNow,
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            string missionsUrl = "/missions";
            var response1 = await Client.PostAsync(missionsUrl, content);
            var response2 = await Client.PostAsync(missionsUrl, content);

            // Assert
            Assert.True(response1.IsSuccessStatusCode);
            Assert.True(response2.IsSuccessStatusCode);
            var missionRun1 = await response1.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );
            var missionRun2 = await response2.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );
            Assert.NotNull(missionRun1);
            Assert.NotNull(missionRun2);
            string? missionId1 = missionRun1.MissionId;
            string? missionId2 = missionRun2.MissionId;
            Assert.Equal(missionId1, missionId2);
            string missionDefinitionsUrl = "/missions/definitions?pageSize=50";
            var missionDefinitionsResponse = await Client.GetAsync(missionDefinitionsUrl);
            var missionDefinitions = await missionDefinitionsResponse.Content.ReadFromJsonAsync<
                List<MissionDefinition>
            >(SerializerOptions);
            Assert.NotNull(missionDefinitions);
            Assert.NotNull(missionDefinitions.Find(m => m.Id == missionId1));
        }

        [Fact]
        public async Task MissionDoesNotStartIfRobotIsNotInSameInstallationAsMission()
        {
            // Arrange - Initialise area
            var installation = await DatabaseUtilities.ReadOrNewInstallation();
            var plant = await DatabaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.ReadOrNewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );

            string testMissionName =
                "testMissionDoesNotStartIfRobotIsNotInSameInstallationAsMission";

            // Arrange - Get different installation
            string otherInstallationCode =
                "installationMissionDoesNotStartIfRobotIsNotInSameInstallationAsMission_Other";
            var otherInstallation = await DatabaseUtilities.NewInstallation(otherInstallationCode);

            // Arrange - Robot
            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Available, otherInstallation);
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
                Tasks =
                [
                    new()
                    {
                        RobotPose = new Pose(),
                        Inspection = new CustomInspectionQuery
                        {
                            InspectionTarget = new Position(),
                            InspectionType = InspectionType.Image,
                        },
                        TaskOrder = 0,
                    },
                    new()
                    {
                        RobotPose = new Pose(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f),
                        Inspection = new CustomInspectionQuery
                        {
                            InspectionTarget = new Position(),
                            InspectionType = InspectionType.Image,
                        },
                        TaskOrder = 1,
                    },
                ],
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            string customMissionsUrl = "/missions/custom";
            var response = await Client.PostAsync(customMissionsUrl, content);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task MissionFailsIfRobotIsNotInSameInspectionAreaAsMission()
        {
            // Arrange - Initialise area
            var installation = await DatabaseUtilities.ReadOrNewInstallation();
            var plant = await DatabaseUtilities.ReadOrNewPlant(installation.InstallationCode);

            string inspectionAreaName1 =
                "inspectionAreaMissionFailsIfRobotIsNotInSameInspectionAreaAsMission1";
            var inspectionArea1 = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode,
                inspectionAreaName1
            );

            string inspectionAreaName2 =
                "inspectionAreaMissionFailsIfRobotIsNotInSameInspectionAreaAsMission2";
            var inspectionArea2 = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode,
                inspectionAreaName2
            );

            string testMissionName = "testMissionFailsIfRobotIsNotInSameInspectionAreaAsMission";

            // Arrange - Robot
            var robot = await DatabaseUtilities.NewRobot(
                RobotStatus.Available,
                installation,
                inspectionArea1
            );
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
                Tasks =
                [
                    new()
                    {
                        RobotPose = new Pose(new Position(1, 9, 4), new Orientation()),
                        Inspection = new CustomInspectionQuery
                        {
                            InspectionTarget = new Position(),
                            InspectionType = InspectionType.Image,
                        },
                        TaskOrder = 0,
                    },
                    new()
                    {
                        RobotPose = new Pose(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f),
                        Inspection = new CustomInspectionQuery
                        {
                            InspectionTarget = new Position(),
                            InspectionType = InspectionType.Image,
                        },
                        TaskOrder = 1,
                    },
                ],
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            string customMissionsUrl = "/missions/custom";
            var missionResponse = await Client.PostAsync(customMissionsUrl, content);
            Assert.Equal(HttpStatusCode.Conflict, missionResponse.StatusCode);
        }
    }
}
