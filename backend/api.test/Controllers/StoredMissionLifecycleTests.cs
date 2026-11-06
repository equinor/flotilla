using System;
using System.Collections.Generic;
using System.Data.Common;
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
using Hangfire.States;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Controllers
{
    public class StoredMissionLifecycleTests : IAsyncLifetime
    {
        private TestWebApplicationFactory<Program> _factory = null!;
        private WebApplicationFactory<Program> _app = null!;
        private PostgreSqlContainer _container = null!;
        private DbConnection _connection = null!;
        private HttpClient _client = null!;
        private Robot _robot = null!;
        private MissionDefinition _definition = null!;
        private readonly List<string> _requests = [];
        private readonly JsonSerializerOptions _json =
            TestSetupHelpers.ConfigureJsonSerializerOptions();
        private IMissionRunService Runs => _app.Services.GetRequiredService<IMissionRunService>();

        public async ValueTask InitializeAsync()
        {
            (_container, string connectionString, _connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var downstream = new Mock<IDownstreamApi>(MockBehavior.Strict);
            downstream
                .Setup(api =>
                    api.CallApiForAppAsync(
                        IsarService.ServiceName,
                        It.IsAny<Action<DownstreamApiOptions>>(),
                        It.IsAny<HttpContent>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Returns(
                    async (
                        string name,
                        Action<DownstreamApiOptions> configure,
                        HttpContent content,
                        CancellationToken token
                    ) =>
                    {
                        var options = new DownstreamApiOptions();
                        configure(options);
                        Assert.Equal("schedule/start-mission", options.RelativePath);
                        using var json = JsonDocument.Parse(await content.ReadAsStringAsync(token));
                        var mission = json.RootElement.GetProperty("mission_definition");
                        Assert.Equal(4, mission.EnumerateObject().Count());
                        Assert.Empty(mission.GetProperty("tasks").EnumerateArray());
                        Assert.Equal(
                            "DiagnosticRoutine",
                            mission.GetProperty("robot_mission_id").GetString()
                        );
                        var runId = mission.GetProperty("id").GetString()!;
                        var saved = await Runs.ReadById(runId);
                        Assert.Equal("DiagnosticRoutine", saved!.RobotMissionId);
                        Assert.Empty(saved.Tasks);
                        _requests.Add(runId);
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = JsonContent.Create(
                                new { id = runId, tasks = Array.Empty<object>() }
                            ),
                        };
                    }
                );
            _factory = TestSetupHelpers.ConfigureWebApplicationFactory(connectionString);
            _factory
                .BackgroundJobs.Setup(j => j.ChangeState("job", It.IsAny<DeletedState>(), null))
                .Returns(true);
            _app = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped<IIsarService, IsarService>();
                    services.AddSingleton(downstream.Object);
                })
            );
            _client = _app.CreateClient();
            var database = _app.Services.GetRequiredService<DatabaseUtilities>();
            var installation = await database.NewInstallation();
            var plant = await database.NewPlant(installation.InstallationCode);
            var area = await database.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            _robot = await database.NewRobot(RobotStatus.Available, installation, area.Id);
            _robot.RobotCapabilities = [RobotCapabilitiesEnum.stored_mission];
            await _app.Services.GetRequiredService<IRobotService>().Update(_robot);
            using var response = await _client.PostAsJsonAsync(
                "missions/definitions",
                new CreateMissionQuery
                {
                    Name = "Daily diagnostics",
                    RobotMissionId = "DiagnosticRoutine",
                    Tasks = [],
                    InspectionAreaId = area.Id,
                    InstallationCode = installation.InstallationCode,
                },
                TestContext.Current.CancellationToken
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var created = (
                await response.Content.ReadFromJsonAsync<MissionDefinitionResponse>(
                    _json,
                    TestContext.Current.CancellationToken
                )
            )!;
            _definition = (
                await _app
                    .Services.GetRequiredService<IMissionDefinitionService>()
                    .ReadById(created.Id)
            )!;
        }

        [Fact]
        public async Task ManualRunPersistsStoredReferenceBeforeDispatch()
        {
            using var response = await _client.PostAsJsonAsync(
                $"missions/schedule/{_definition.Id}",
                new ScheduleMissionQuery { RobotId = _robot.Id },
                TestContext.Current.CancellationToken
            );
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var run = await response.Content.ReadFromJsonAsync<MissionRun>(
                _json,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(run!.Id, Assert.Single(_requests));
            Assert.Equal("DiagnosticRoutine", run.RobotMissionId);
            Assert.Empty(run.Tasks);
        }

        [Fact]
        public async Task AutomaticAndRerunMissionsPreserveStoredReference()
        {
            var time = new TimeOnly(8, 0);
            _definition.AutoScheduleFrequency = new AutoScheduleFrequency
            {
                SchedulingTimesCETperWeek = [new TimeAndDay(DayOfWeek.Monday, time)],
                AutoScheduledJobs = JsonSerializer.Serialize(
                    new Dictionary<TimeOnly, string> { [time] = "job" }
                ),
            };
            await _app.Services.GetRequiredService<IMissionDefinitionService>().Update(_definition);
            using var jobScope = _app.Services.CreateScope();
            var scheduler = (AutoScheduleService)
                jobScope.ServiceProvider.GetRequiredService<IAutoScheduleService>();
            await scheduler.AutoScheduleMissionRun(_definition.Id, time);
            var first = Assert.Single(_requests);
            await Runs.UpdateMissionRunStatus(first, MissionStatus.Failed);
            using var response = await _client.PostAsJsonAsync(
                $"missions/rerun/{first}",
                new ScheduleMissionQuery { RobotId = _robot.Id },
                TestContext.Current.CancellationToken
            );
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(2, _requests.Count);
            Assert.NotEqual(first, _requests[1]);
        }

        public async ValueTask DisposeAsync()
        {
            _client?.Dispose();
            if (_app is not null)
                await _app.DisposeAsync();
            if (_factory is not null)
                await _factory.DisposeAsync();
            if (_connection is not null)
                await _connection.DisposeAsync();
            if (_container is not null)
                await _container.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
