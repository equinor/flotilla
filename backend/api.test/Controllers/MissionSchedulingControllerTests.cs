using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.Models;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Controllers
{
    public class MissionSchedulingControllerTests : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required PostgreSqlContainer Container;
        public required HttpClient Client;
        public required JsonSerializerOptions SerializerOptions;

        public required IMissionDefinitionService MissionDefinitionService;

        public async Task InitializeAsync()
        {
            (Container, string connectionString, var _) =
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
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task CheckThatSchedulingAMissionToBusyRobotSetsMissionToPending()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var robot = await DatabaseUtilities.NewRobot(
                RobotStatus.Busy,
                installation,
                inspectionArea.Id
            );
            string missionsUrl = "/missions";

            // Act
            var query = new ScheduledMissionQuery
            {
                RobotId = robot.Id,
                InstallationCode = installation.InstallationCode,
                MissionSourceId = "95",
                CreationTime = DateTime.UtcNow,
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            var response = await Client.PostAsync(missionsUrl, content);

            // Assert
            var missionRun = await response.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(MissionStatus.Pending, missionRun!.Status);
        }

        [Fact]
        public async Task CheckThatSchedulingThreeAdditionalMissionsToTheQueueWorksAsExpected()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var robot = await DatabaseUtilities.NewRobot(
                RobotStatus.Busy,
                installation,
                inspectionArea.Id
            );

            // Act
            var query = new ScheduledMissionQuery
            {
                RobotId = robot.Id,
                InstallationCode = installation.InstallationCode,
                MissionSourceId = "97",
                CreationTime = DateTime.UtcNow,
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            const string MissionsUrl = "/missions";
            _ = await Client.PostAsync(MissionsUrl, content);

            var responseMissionOne = await Client.PostAsync(MissionsUrl, content);
            var missionRunOne = await responseMissionOne.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );
            var responseMissionTwo = await Client.PostAsync(MissionsUrl, content);
            var missionRunTwo = await responseMissionTwo.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );
            var responseMissionThree = await Client.PostAsync(MissionsUrl, content);
            var missionRunThree = await responseMissionThree.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );

            var responseActiveMissionRuns = await Client.GetAsync("/missions/runs?pageSize=50");
            var missionRuns = await responseActiveMissionRuns.Content.ReadFromJsonAsync<
                List<MissionRun>
            >(SerializerOptions);

            // Assert
            Assert.True(responseMissionOne.IsSuccessStatusCode);
            Assert.True(responseMissionTwo.IsSuccessStatusCode);
            Assert.True(responseMissionThree.IsSuccessStatusCode);

            Assert.Single(missionRuns!.Where(m => m.Id == missionRunOne!.Id).ToList());
            Assert.Single(missionRuns!.Where(m => m.Id == missionRunTwo!.Id).ToList());
            Assert.Single(missionRuns!.Where(m => m.Id == missionRunThree!.Id).ToList());
        }

        [Fact]
        public async Task CheckThatUnknownMissionIdReturnsNotFound()
        {
            const string MissionId = "ThisMissionDoesNotExist";
            const string Url = "/missions/runs/" + MissionId;
            var response = await Client.GetAsync(Url);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CheckThatDeletingMissionRunThatDoesNotExistReturnsNotFound()
        {
            const string MissionId = "ThisMissionDoesNotExist";
            const string Url = "/missions/runs/" + MissionId;
            var response = await Client.DeleteAsync(Url);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ScheduleDuplicateCustomMissionDefinitions()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var robot = await DatabaseUtilities.NewRobot(
                RobotStatus.Busy,
                installation,
                inspectionArea.Id
            );

            var query = CreateDefaultCustomMissionQuery(robot.Id, installation.InstallationCode);
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            const string CustomMissionsUrl = "/missions/custom";
            var responseMissionOne = await Client.PostAsync(CustomMissionsUrl, content);
            var missionRunOne = await responseMissionOne.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );

            var responseMissionTwo = await Client.PostAsync(CustomMissionsUrl, content);
            var missionRunTwo = await responseMissionTwo.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );

            // Assert
            var missionDefinitions = await MissionDefinitionService.ReadAll(
                new MissionDefinitionQueryStringParameters()
            );

            Assert.Equal(missionRunOne!.MissionId, missionRunTwo!.MissionId);
            Assert.Single(missionDefinitions);
        }

        [Fact]
        public async Task CheckThatNextRunIsCorrectlySelectedWhenSchedulingMultipleMissions()
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

            var query = CreateDefaultCustomMissionQuery(robot.Id, installation.InstallationCode);
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            const string CustomMissionsUrl = "/missions/custom";
            var response = await Client.PostAsync(CustomMissionsUrl, content);
            var activeMissionRun = await response.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );

            var scheduleQuery = new ScheduleMissionQuery
            {
                RobotId = robot.Id,
                CreationTime = DateTime.SpecifyKind(new DateTime(2050, 1, 1), DateTimeKind.Utc),
            };
            var scheduleContent = new StringContent(
                JsonSerializer.Serialize(scheduleQuery),
                null,
                "application/json"
            );

            string scheduleMissionsUrl = $"/missions/schedule/{activeMissionRun!.MissionId}";

            var missionRunOneResponse = await Client.PostAsync(
                scheduleMissionsUrl,
                scheduleContent
            );
            var missionRunOne = await missionRunOneResponse.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );

            var missionRunTwoResponse = await Client.PostAsync(
                scheduleMissionsUrl,
                scheduleContent
            );
            var missionRunTwo = await missionRunTwoResponse.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );

            var missionRunThreeResponse = await Client.PostAsync(
                scheduleMissionsUrl,
                scheduleContent
            );
            var missionRunThree =
                await missionRunThreeResponse.Content.ReadFromJsonAsync<MissionRun>(
                    SerializerOptions
                );

            Thread.Sleep(1000);
            // Act
            string nextMissionUrl = $"missions/definitions/{activeMissionRun.MissionId}/next-run";
            var nextMissionResponse = await Client.GetAsync(nextMissionUrl);

            // Assert
            var nextMissionRun = await nextMissionResponse.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );

            // Next mission can be any of these three missions due to timing
            var possibleNextMissionRuns = new List<string>
            {
                missionRunOne!.Id,
                missionRunTwo!.Id,
                missionRunThree!.Id,
            };

            Assert.True(nextMissionResponse.IsSuccessStatusCode);
            Assert.NotNull(nextMissionRun);
            Assert.Equal(missionRunOne!.MissionId, activeMissionRun.MissionId);
            Assert.Equal(missionRunTwo!.MissionId, activeMissionRun.MissionId);
            Assert.Equal(missionRunThree!.MissionId, activeMissionRun.MissionId);
            Assert.Contains(nextMissionRun.Id, possibleNextMissionRuns);
        }

        [Fact]
        public async Task CheckThatMissionDoesNotStartIfRobotIsNotInSameInstallationAsMission()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );

            var otherInstallation = await DatabaseUtilities.NewInstallation("OtherCode");
            var robot = await DatabaseUtilities.NewRobot(
                RobotStatus.Available,
                otherInstallation,
                inspectionArea.Id
            );

            var query = CreateDefaultCustomMissionQuery(robot.Id, installation.InstallationCode);
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            const string CustomMissionsUrl = "/missions/custom";
            var response = await Client.PostAsync(CustomMissionsUrl, content);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task CheckThatMissionFailsIfRobotIsNotInSameInspectionAreaAsMission()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);

            var inspectionPolygonRobot = new AreaPolygon
            {
                ZMin = 0,
                ZMax = 10,
                Positions =
                [
                    new PolygonPoint { X = 11, Y = 11 },
                    new PolygonPoint { X = 11, Y = 20 },
                    new PolygonPoint { X = 20, Y = 20 },
                    new PolygonPoint { X = 20, Y = 11 },
                ],
            };

            var inspectionAreaRobot = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode,
                "InspectionAreaRobot",
                inspectionPolygonRobot
            );

            var inspectionPolygonMission = new AreaPolygon
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
            var _inspectionAreaMission = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode,
                "InspectionAreaMission",
                inspectionPolygonMission
            );

            var robot = await DatabaseUtilities.NewRobot(
                RobotStatus.Available,
                installation,
                inspectionAreaRobot.Id
            );

            var query = CreateDefaultCustomMissionQuery(robot.Id, installation.InstallationCode);
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            const string CustomMissionsUrl = "/missions/custom";
            var missionResponse = await Client.PostAsync(CustomMissionsUrl, content);
            Assert.Equal(HttpStatusCode.BadRequest, missionResponse.StatusCode);
        }

        private static CustomMissionQuery CreateDefaultCustomMissionQuery(
            string robotId,
            string installationCode
        )
        {
            return new CustomMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installationCode,
                CreationTime = DateTime.SpecifyKind(new DateTime(3050, 1, 1), DateTimeKind.Utc),
                InspectionFrequency = new TimeSpan(14, 0, 0, 0),
                Name = "TestMission",
                Tasks =
                [
                    new CustomTaskQuery
                    {
                        RobotPose = new Pose(),
                        Inspection = new CustomInspectionQuery
                        {
                            InspectionTarget = new Position(),
                            InspectionType = InspectionType.Image,
                        },
                        TaskOrder = 0,
                    },
                    new CustomTaskQuery
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
        }
    }
}
