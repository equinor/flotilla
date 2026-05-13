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

        public async ValueTask InitializeAsync()
        {
            (Container, string connectionString, var _) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);

            Client = TestSetupHelpers.ConfigureHttpClient(factory);
            SerializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();

            DatabaseUtilities = serviceProvider.GetRequiredService<DatabaseUtilities>();
            MissionDefinitionService =
                serviceProvider.GetRequiredService<IMissionDefinitionService>();
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        [Fact]
        public async Task CheckThatSchedulingAMissionToBusyRobotSetsMissionToQueued()
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
            TaskDefinition task = new()
            {
                TagId = "dummy tag id 1",
                Description = "dummy task 1",
                RobotPose = new Pose(),
                AnalysisTypes = [AnalysisType.Fencilla],
                TargetPosition = new Position
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                },
            };
            var missionDefinition = await DatabaseUtilities.NewMissionDefinition(
                null,
                installation.InstallationCode,
                inspectionArea,
                [task],
                writeToDatabase: true
            );
            string missionsUrl = $"/missions/schedule/{missionDefinition.Id}";

            // Act
            var query = new ScheduledMissionQuery { RobotId = robot.Id };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            var response = await Client.PostAsync(
                missionsUrl,
                content,
                TestContext.Current.CancellationToken
            );

            // Assert
            var missionRun = await response.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions,
                cancellationToken: TestContext.Current.CancellationToken
            );

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(MissionStatus.Queued, missionRun!.Status);
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
            TaskDefinition task = new()
            {
                TagId = "dummy tag id 1",
                Description = "dummy task 1",
                RobotPose = new Pose(),
                AnalysisTypes = [AnalysisType.Fencilla],
                TargetPosition = new Position
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                },
            };
            var missionDefinition = await DatabaseUtilities.NewMissionDefinition(
                null,
                installation.InstallationCode,
                inspectionArea,
                [task],
                writeToDatabase: true
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

            string missionsUrl = $"/missions/schedule/{missionDefinition.Id}";
            _ = await Client.PostAsync(missionsUrl, content, TestContext.Current.CancellationToken);

            var responseMissionOne = await Client.PostAsync(
                missionsUrl,
                content,
                TestContext.Current.CancellationToken
            );
            var missionRunOne = await responseMissionOne.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions,
                cancellationToken: TestContext.Current.CancellationToken
            );
            var responseMissionTwo = await Client.PostAsync(
                missionsUrl,
                content,
                TestContext.Current.CancellationToken
            );
            var missionRunTwo = await responseMissionTwo.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions,
                cancellationToken: TestContext.Current.CancellationToken
            );
            var responseMissionThree = await Client.PostAsync(
                missionsUrl,
                content,
                TestContext.Current.CancellationToken
            );
            var missionRunThree = await responseMissionThree.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions,
                cancellationToken: TestContext.Current.CancellationToken
            );

            var responseActiveMissionRuns = await Client.GetAsync(
                "/missions/runs?pageSize=50",
                TestContext.Current.CancellationToken
            );
            var missionRuns = await responseActiveMissionRuns.Content.ReadFromJsonAsync<
                List<MissionRun>
            >(SerializerOptions, cancellationToken: TestContext.Current.CancellationToken);

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
            var response = await Client.GetAsync(Url, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CheckThatDeletingMissionRunThatDoesNotExistReturnsNotFound()
        {
            const string MissionId = "ThisMissionDoesNotExist";
            const string Url = "/missions/runs/" + MissionId;
            var response = await Client.DeleteAsync(Url, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateDuplicateMissionDefinitions()
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

            TaskDefinition task = new()
            {
                TagId = "dummy tag id 1",
                Description = "dummy task 1",
                RobotPose = new Pose(),
                AnalysisTypes = [AnalysisType.Fencilla],
                TargetPosition = new Position
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                },
            };
            var missionDefinition = await DatabaseUtilities.NewMissionDefinition(
                null,
                installation.InstallationCode,
                inspectionArea,
                [task],
                writeToDatabase: true
            );

            var query = new ScheduledMissionQuery { RobotId = robot.Id };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            string customMissionsUrl = $"/missions/schedule/{missionDefinition.Id}";
            var responseMissionOne = await Client.PostAsync(
                customMissionsUrl,
                content,
                TestContext.Current.CancellationToken
            );
            var missionRunOne = await responseMissionOne.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions,
                cancellationToken: TestContext.Current.CancellationToken
            );

            var responseMissionTwo = await Client.PostAsync(
                customMissionsUrl,
                content,
                TestContext.Current.CancellationToken
            );
            var missionRunTwo = await responseMissionTwo.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions,
                cancellationToken: TestContext.Current.CancellationToken
            );

            // Assert
            var missionDefinitions = await MissionDefinitionService.ReadAll(
                new MissionDefinitionQueryStringParameters()
            );

            Assert.Equal(missionRunOne!.MissionId, missionRunTwo!.MissionId);
            Assert.Single(missionDefinitions);
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

            var missionId = Guid.NewGuid().ToString();
            TaskDefinition task1 = new()
            {
                TagId = "dummy tag id 1",
                Description = "dummy task 1",
                RobotPose = new Pose(),
                AnalysisTypes = [AnalysisType.Fencilla],
                TargetPosition = new Position
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                },
            };
            var missionDefinition = await DatabaseUtilities.NewMissionDefinition(
                missionId,
                installation.InstallationCode,
                inspectionArea,
                [task1],
                writeToDatabase: true
            );

            var scheduleQuery = new ScheduleMissionQuery { RobotId = robot.Id };
            var scheduleContent = new StringContent(
                JsonSerializer.Serialize(scheduleQuery),
                null,
                "application/json"
            );

            string scheduleUrl = $"/missions/schedule/{missionDefinition.Id}";

            var scheduleMissionResponse = await Client.PostAsync(
                scheduleUrl,
                scheduleContent,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(HttpStatusCode.Conflict, scheduleMissionResponse.StatusCode);
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

            var missionId = Guid.NewGuid().ToString();
            TaskDefinition task1 = new()
            {
                TagId = "dummy tag id 1",
                Description = "dummy task 1",
                RobotPose = new Pose(),
                AnalysisTypes = [AnalysisType.Fencilla],
                TargetPosition = new Position
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                },
            };
            var missionDefinition = await DatabaseUtilities.NewMissionDefinition(
                missionId,
                installation.InstallationCode,
                _inspectionAreaMission,
                [task1],
                writeToDatabase: true
            );

            var scheduleQuery = new ScheduleMissionQuery { RobotId = robot.Id };
            var scheduleContent = new StringContent(
                JsonSerializer.Serialize(scheduleQuery),
                null,
                "application/json"
            );

            string scheduleUrl = $"/missions/schedule/{missionDefinition.Id}";

            var scheduleMissionResponse = await Client.PostAsync(
                scheduleUrl,
                scheduleContent,
                TestContext.Current.CancellationToken
            );

            // Act
            Assert.Equal(HttpStatusCode.BadRequest, scheduleMissionResponse.StatusCode);
        }

        private static MissionQuery CreateDefaultMissionQuery(
            string robotId,
            string installationCode
        )
        {
            return new MissionQuery
            {
                RobotId = robotId,
                InstallationCode = installationCode,
                Name = "TestMission",
                Tasks =
                [
                    new TaskQuery
                    {
                        TagId = "test",
                        TargetPosition = new Position(),
                        SensorType = SensorType.Image,
                        AnalysisTypes = [AnalysisType.Fencilla],
                        RobotPose = new Pose(11, 11, 11, 0, 0, 0, 1),
                    },
                    new TaskQuery
                    {
                        RobotPose = new Pose(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f),
                        TagId = "test",
                        TargetPosition = new Position(),
                        SensorType = SensorType.Image,
                        AnalysisTypes = [AnalysisType.Fencilla],
                    },
                ],
            };
        }
    }
}
