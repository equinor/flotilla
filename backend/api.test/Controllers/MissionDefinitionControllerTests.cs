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
using Api.Test.Mocks;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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
        private IRobotService _robotService = null!;
        private IMissionRunService _missionRunService = null!;
        private MockIsarService _isarService = null!;

        public async ValueTask InitializeAsync()
        {
            (Container, string connectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);
            factory
                .BackgroundJobs.Setup(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()))
                .Returns("job");
            _robotService = serviceProvider.GetRequiredService<IRobotService>();
            _missionRunService = serviceProvider.GetRequiredService<IMissionRunService>();
            _isarService = factory.MockIsarService;

            Client = TestSetupHelpers.ConfigureHttpClient(factory);
            SerializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();

            DatabaseUtilities = serviceProvider.GetRequiredService<DatabaseUtilities>();

            MissionDefinitionService =
                serviceProvider.GetRequiredService<IMissionDefinitionService>();
        }

        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task StoredMissionCanBeCreatedScheduledAndRerunWithoutInspectionTasks()
        {
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var area = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var robot = await DatabaseUtilities.NewRobot(
                RobotStatus.Available,
                installation,
                area.Id
            );
            robot.RobotCapabilities = [RobotCapabilitiesEnum.stored_mission];
            await _robotService.Update(robot);

            var response = await Client.PostAsJsonAsync(
                "missions/definitions",
                new
                {
                    installationCode = installation.InstallationCode,
                    name = "Daily diagnostics",
                    robotMissionId = "DiagnosticRoutine",
                    inspectionAreaId = area.Id,
                    tasks = System.Array.Empty<TaskQuery>(),
                },
                TestContext.Current.CancellationToken
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<MissionDefinition>(
                SerializerOptions,
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(created);
            var definition = await MissionDefinitionService.ReadById(created.Id);
            Assert.Equal("DiagnosticRoutine", definition!.RobotMissionId);
            Assert.Empty(definition.Tasks);
            Assert.False(definition.IsAdHoc);

            var patch = await Client.PatchAsJsonAsync(
                $"missions/definitions/{created.Id}",
                new UpdateMissionDefinitionQuery
                {
                    Name = "Renamed diagnostics",
                    SchedulingTimesCETperWeek =
                    [
                        new() { DayOfWeek = DayOfWeek.Monday, TimeOfDay = new TimeOnly(8, 0) },
                        new() { DayOfWeek = DayOfWeek.Tuesday, TimeOfDay = new TimeOnly(8, 0) },
                        new() { DayOfWeek = DayOfWeek.Wednesday, TimeOfDay = new TimeOnly(8, 0) },
                        new() { DayOfWeek = DayOfWeek.Thursday, TimeOfDay = new TimeOnly(8, 0) },
                        new() { DayOfWeek = DayOfWeek.Friday, TimeOfDay = new TimeOnly(8, 0) },
                        new() { DayOfWeek = DayOfWeek.Saturday, TimeOfDay = new TimeOnly(8, 0) },
                        new() { DayOfWeek = DayOfWeek.Sunday, TimeOfDay = new TimeOnly(8, 0) },
                    ],
                },
                TestContext.Current.CancellationToken
            );
            Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
            var updated = await patch.Content.ReadFromJsonAsync<MissionDefinitionResponse>(
                SerializerOptions,
                TestContext.Current.CancellationToken
            );
            Assert.Equal("DiagnosticRoutine", updated!.RobotMissionId);
            Assert.Equal(7, updated.AutoScheduleFrequency!.SchedulingTimesCETperWeek.Count);

            var schedule = await Client.PostAsJsonAsync(
                $"missions/schedule/{created.Id}",
                new ScheduleMissionQuery { RobotId = robot.Id },
                TestContext.Current.CancellationToken
            );
            Assert.Equal(HttpStatusCode.Created, schedule.StatusCode);
            var run = await schedule.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions,
                TestContext.Current.CancellationToken
            );
            Assert.Equal("DiagnosticRoutine", run!.RobotMissionId);
            Assert.Equal("Renamed diagnostics", run.Name);
            Assert.Empty(run.Tasks);
            Assert.True(_isarService.isStarted);
            Assert.Equal(
                "DiagnosticRoutine",
                (await _missionRunService.ReadById(run.Id))!.RobotMissionId
            );

            var rerun = await Client.PostAsJsonAsync(
                $"missions/rerun/{run.Id}",
                new ScheduleMissionQuery { RobotId = robot.Id },
                TestContext.Current.CancellationToken
            );
            Assert.Equal(HttpStatusCode.Created, rerun.StatusCode);
            var repeated = await rerun.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions,
                TestContext.Current.CancellationToken
            );
            Assert.Equal("DiagnosticRoutine", repeated!.RobotMissionId);
            Assert.NotEqual(run.Id, repeated.Id);

            var editTasks = await Client.PatchAsJsonAsync(
                $"missions/definitions/{created.Id}",
                new UpdateMissionDefinitionQuery { Tasks = [] },
                TestContext.Current.CancellationToken
            );
            Assert.Equal(HttpStatusCode.BadRequest, editTasks.StatusCode);

            robot.Status = RobotStatus.Busy;
            await _robotService.Update(robot);
            _isarService.isStarted = false;
            var busy = await Client.PostAsJsonAsync(
                $"missions/schedule/{created.Id}",
                new ScheduleMissionQuery { RobotId = robot.Id },
                TestContext.Current.CancellationToken
            );
            Assert.Equal(HttpStatusCode.Created, busy.StatusCode);
            Assert.False(_isarService.isStarted);
            var queued = await busy.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(MissionStatus.Queued, queued!.Status);

            robot.RobotCapabilities = [RobotCapabilitiesEnum.take_image];
            await _robotService.Update(robot);
            var incompatible = await Client.PostAsJsonAsync(
                $"missions/schedule/{created.Id}",
                new ScheduleMissionQuery { RobotId = robot.Id },
                TestContext.Current.CancellationToken
            );
            Assert.Equal(HttpStatusCode.BadRequest, incompatible.StatusCode);
        }

        [Fact]
        public async Task InvalidStoredMissionRequestsAreRejected()
        {
            var invalidQueries = new[]
            {
                new CreateMissionQuery
                {
                    RobotMissionId = "",
                    InspectionAreaId = "area",
                    Tasks = [],
                },
                new CreateMissionQuery
                {
                    RobotMissionId = " ",
                    InspectionAreaId = "area",
                    Tasks = [],
                },
                new CreateMissionQuery { RobotMissionId = "DiagnosticRoutine", Tasks = [] },
                new CreateMissionQuery
                {
                    RobotMissionId = "DiagnosticRoutine",
                    InspectionAreaId = "area",
                    Tasks = [new TaskQuery()],
                },
                new CreateMissionQuery { Tasks = [] },
            };
            foreach (var invalid in invalidQueries)
            {
                var query = invalid;
                query.Name = "Diagnostics";
                query.InstallationCode = "TEST";
                var response = await Client.PostAsJsonAsync(
                    "missions/definitions",
                    query,
                    TestContext.Current.CancellationToken
                );
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Fact]
        public async Task StoredMissionCannotUseAnotherInstallationsInspectionArea()
        {
            var installation = await DatabaseUtilities.NewInstallation();
            var otherInstallation = await DatabaseUtilities.NewInstallation("OTHER");
            var plant = await DatabaseUtilities.NewPlant(otherInstallation.InstallationCode);
            var area = await DatabaseUtilities.NewInspectionArea(
                otherInstallation.InstallationCode,
                plant.PlantCode
            );
            var response = await Client.PostAsJsonAsync(
                "missions/definitions",
                new
                {
                    installationCode = installation.InstallationCode,
                    name = "Daily diagnostics",
                    robotMissionId = "DiagnosticRoutine",
                    inspectionAreaId = area.Id,
                    tasks = System.Array.Empty<TaskQuery>(),
                },
                TestContext.Current.CancellationToken
            );
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

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

            var task = new TaskDefinition(
                new TaskQuery
                {
                    TagId = "test",
                    TargetPosition = new Position(),
                    SensorType = SensorType.Image,
                    AnalysisTypes = [AnalysisType.Fencilla],
                    RobotPose = new Pose(11, 11, 11, 0, 0, 0, 1),
                },
                1
            );
            var missionDefinition = new MissionDefinition
            {
                Tasks = [task],
                InstallationCode = installation.InstallationCode,
                Name = "Test Mission Definition",
                InspectionArea = inspectionArea,
                IsDeprecated = false,
            };

            _ = await MissionDefinitionService.Create(missionDefinition);

            // Act
            var response = await Client.GetAsync(
                "missions/definitions",
                TestContext.Current.CancellationToken
            );

            // Assert
            var missionDefinitions = await response.Content.ReadFromJsonAsync<
                IEnumerable<MissionDefinitionResponse>
            >(SerializerOptions, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Single(missionDefinitions!);
        }

        [Fact]
        public async Task CheckThatListAllMissionDefinitionsSucceedWhenThereAreNoMissionDefinitions()
        {
            var response = await Client.GetAsync(
                "missions/definitions",
                TestContext.Current.CancellationToken
            );

            var missionDefinitions = await response.Content.ReadFromJsonAsync<
                List<MissionDefinitionResponse>
            >(SerializerOptions, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Empty(missionDefinitions!);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CheckThatCreateMissionPersistsIsAdHoc(bool? isAdHoc)
        {
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            _ = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode,
                areaPolygon: new AreaPolygon
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
                }
            );
            var query = new Dictionary<string, object>
            {
                ["installationCode"] = installation.InstallationCode,
                ["name"] = "Test mission",
                ["tasks"] = new List<TaskQuery>
                {
                    new()
                    {
                        TagId = "test",
                        TargetPosition = new Position(1, 1, 1),
                        SensorType = SensorType.Image,
                        AnalysisTypes = [AnalysisType.Fencilla],
                        RobotPose = new Pose(1, 1, 1, 0, 0, 0, 1),
                    },
                },
            };
            if (isAdHoc.HasValue)
                query["isAdHoc"] = isAdHoc.Value;

            var response = await Client.PostAsJsonAsync(
                "missions/definitions",
                query,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<MissionDefinition>(
                SerializerOptions,
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(created);
            Assert.Equal(isAdHoc ?? false, created.IsAdHoc);

            var saved = await MissionDefinitionService.ReadById(created.Id);
            Assert.NotNull(saved);
            Assert.Equal(isAdHoc ?? false, saved.IsAdHoc);

            var definition = await Client.GetFromJsonAsync<MissionDefinitionResponse>(
                $"missions/definitions/{created.Id}",
                SerializerOptions,
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(definition);
            Assert.Equal(isAdHoc ?? false, definition.IsAdHoc);

            var definitions = await Client.GetFromJsonAsync<List<MissionDefinitionResponse>>(
                "missions/definitions",
                SerializerOptions,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(isAdHoc ?? false, Assert.Single(definitions!).IsAdHoc);
        }

        [Fact]
        public async Task CheckThatUpdateMissionSucceedsForValidInspectionArea()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode,
                areaPolygon: new AreaPolygon
                {
                    ZMin = -10,
                    ZMax = 10,
                    Positions =
                    [
                        new PolygonPoint { X = 0, Y = 0 },
                        new PolygonPoint { X = 0, Y = 10 },
                        new PolygonPoint { X = 10, Y = 10 },
                        new PolygonPoint { X = 10, Y = 0 },
                    ],
                }
            );
            var missionDefinition = await DatabaseUtilities.NewMissionDefinition(
                id: null,
                installationCode: installation.InstallationCode,
                inspectionArea,
                tasks:
                [
                    new TaskDefinition(
                        new TaskQuery
                        {
                            TagId = "existing-task",
                            TargetPosition = new Position(),
                            SensorType = SensorType.Image,
                            AnalysisTypes = [AnalysisType.Fencilla],
                            RobotPose = new Pose(),
                        },
                        1
                    ),
                ],
                writeToDatabase: true
            );
            var query = new UpdateMissionDefinitionQuery
            {
                Tasks =
                [
                    new TaskQuery
                    {
                        TagId = "task-with-valid-inspection-area",
                        TargetPosition = new Position(10, 10, 10),
                        SensorType = SensorType.Image,
                        AnalysisTypes = [AnalysisType.Fencilla],
                        RobotPose = new Pose(1, 1, 1, 0, 0, 0, 1),
                    },
                ],
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"missions/definitions/{missionDefinition.Id}",
                query,
                TestContext.Current.CancellationToken
            );

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CheckThatUpdateMissionSucceedsForChangedInspectionArea()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode,
                areaPolygon: new AreaPolygon
                {
                    ZMin = 0,
                    ZMax = 1,
                    Positions =
                    [
                        new PolygonPoint { X = 0, Y = 0 },
                        new PolygonPoint { X = 0, Y = 1 },
                        new PolygonPoint { X = 1, Y = 1 },
                        new PolygonPoint { X = 1, Y = 0 },
                    ],
                }
            );
            var nextInspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode,
                inspectionAreaName: "next-inspection-area",
                areaPolygon: new AreaPolygon
                {
                    ZMin = 10,
                    ZMax = 20,
                    Positions =
                    [
                        new PolygonPoint { X = 0, Y = 0 },
                        new PolygonPoint { X = 0, Y = 1 },
                        new PolygonPoint { X = 1, Y = 1 },
                        new PolygonPoint { X = 1, Y = 0 },
                    ],
                }
            );
            var missionDefinition = await DatabaseUtilities.NewMissionDefinition(
                id: null,
                installationCode: installation.InstallationCode,
                inspectionArea,
                tasks:
                [
                    new TaskDefinition(
                        new TaskQuery
                        {
                            TagId = "existing-task",
                            TargetPosition = new Position(),
                            SensorType = SensorType.Image,
                            AnalysisTypes = [AnalysisType.Fencilla],
                            RobotPose = new Pose(),
                        },
                        1
                    ),
                ],
                writeToDatabase: true
            );
            var query = new UpdateMissionDefinitionQuery
            {
                Tasks =
                [
                    new TaskQuery
                    {
                        TagId = "in-next-inspection-area",
                        TargetPosition = new Position(10, 10, 10),
                        SensorType = SensorType.Image,
                        AnalysisTypes = [AnalysisType.Fencilla],
                        RobotPose = new Pose(0, 0, 15, 0, 0, 0, 1),
                    },
                ],
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"missions/definitions/{missionDefinition.Id}",
                query,
                TestContext.Current.CancellationToken
            );

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CheckThatUpdateMissionFailsForInvalidInspectionArea()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode,
                areaPolygon: new AreaPolygon
                {
                    ZMin = 0,
                    ZMax = 1,
                    Positions =
                    [
                        new PolygonPoint { X = 0, Y = 0 },
                        new PolygonPoint { X = 0, Y = 1 },
                        new PolygonPoint { X = 1, Y = 1 },
                        new PolygonPoint { X = 1, Y = 0 },
                    ],
                }
            );
            var missionDefinition = await DatabaseUtilities.NewMissionDefinition(
                id: null,
                installationCode: installation.InstallationCode,
                inspectionArea,
                tasks:
                [
                    new TaskDefinition(
                        new TaskQuery
                        {
                            TagId = "existing-task",
                            TargetPosition = new Position(),
                            SensorType = SensorType.Image,
                            AnalysisTypes = [AnalysisType.Fencilla],
                            RobotPose = new Pose(),
                        },
                        1
                    ),
                ],
                writeToDatabase: true
            );
            var query = new UpdateMissionDefinitionQuery
            {
                Tasks =
                [
                    new TaskQuery
                    {
                        TagId = "missing-inspection-area",
                        TargetPosition = new Position(10, 10, 10),
                        SensorType = SensorType.Image,
                        AnalysisTypes = [AnalysisType.Fencilla],
                        RobotPose = new Pose(10, 10, 10, 0, 0, 0, 1),
                    },
                ],
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"missions/definitions/{missionDefinition.Id}",
                query,
                TestContext.Current.CancellationToken
            );

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
