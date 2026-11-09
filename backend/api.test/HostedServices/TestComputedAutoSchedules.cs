using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Api.Controllers;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Api.Utilities;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.HostedServices;

public class TestComputedAutoSchedules : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    private SchedulingFactory _factory = null!;
    private IServiceScope _scope = null!;
    private FlotillaDbContext _context = null!;
    private IMissionDefinitionService _definitions = null!;
    private IMissionRunService _runs = null!;
    private MissionDefinition _definition = null!;
    private Robot _robot = null!;
    private AutoScheduleService _service = null!;
    private AutoScheduleJobs _registry = null!;
    private readonly Clock _clock = new();
    private readonly RetryInterceptor _retry = new();
    private readonly Mock<IMissionSchedulingService> _scheduling = new();
    private readonly Mock<IBackgroundJobClient> _jobs = new(MockBehavior.Strict);
    private readonly Mock<ISignalRService> _signals = new();
    private int _jobNumber;
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.Parse("2026-10-16T20:00:00Z");

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class RetryInterceptor : SaveChangesInterceptor
    {
        public bool FailOnce;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default
        )
        {
            if (eventData.Context!.ChangeTracker.Entries<MissionRun>().Any())
            {
                Assert.Null(eventData.Context.Database.CurrentTransaction);
                if (FailOnce)
                {
                    FailOnce = false;
                    throw new NpgsqlException(
                        "Simulated transient write failure",
                        new TimeoutException()
                    );
                }
            }
            return ValueTask.FromResult(result);
        }
    }

    private sealed class SchedulingFactory : TestWebApplicationFactory<Program>
    {
        private readonly string _connectionString;
        private readonly RetryInterceptor _retry;
        private readonly ISignalRService _signals;

        public SchedulingFactory(
            string connectionString,
            RetryInterceptor retry,
            ISignalRService signals
        )
            : base(connectionString)
        {
            _connectionString = connectionString;
            _retry = retry;
            _signals = signals;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<FlotillaDbContext>(_ => new FlotillaDbContext(
                    new DbContextOptionsBuilder<FlotillaDbContext>()
                        .UseNpgsql(_connectionString, o => o.EnableRetryOnFailure())
                        .AddInterceptors(_retry)
                        .Options
                ));
                services.AddScoped<IMissionRunService>(provider =>
                    ActivatorUtilities.CreateInstance<MissionRunService>(provider, _signals)
                );
            });
        }
    }

    public async ValueTask InitializeAsync()
    {
        var setup = await TestSetupHelpers.ConfigurePostgreSqlDatabase();
        _container = setup.Item1;
        await setup.Item3.DisposeAsync();
        _factory = new SchedulingFactory(setup.Item2, _retry, _signals.Object);
        _scope = _factory.Services.CreateScope();
        var services = _scope.ServiceProvider;
        _context = services.GetRequiredService<FlotillaDbContext>();
        _definitions = services.GetRequiredService<IMissionDefinitionService>();
        _runs = services.GetRequiredService<IMissionRunService>();
        var database = services.GetRequiredService<DatabaseUtilities>();
        var installation = await database.NewInstallation();
        var plant = await database.NewPlant(installation.InstallationCode);
        var area = await database.NewInspectionArea(installation.InstallationCode, plant.PlantCode);
        _robot = await database.NewRobot(RobotStatus.Available, installation, area.Id);
        _definition = await database.NewMissionDefinition(
            null,
            installation.InstallationCode,
            area,
            [
                new TaskDefinition
                {
                    Index = 1,
                    RobotPose = new Pose(),
                    TargetPosition = new Position(),
                    SensorType = SensorType.Image,
                },
            ],
            writeToDatabase: true
        );
        _jobs
            .Setup(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Returns(() => $"job-{Interlocked.Increment(ref _jobNumber)}");
        _registry = new AutoScheduleJobs(_clock);
        _service = Worker(services);
        _definition = await _service.UpdateAutoScheduleFrequency(
            _definition,
            [
                new TimeAndDay(DayOfWeek.Friday, new TimeOnly(23, 0)),
                new TimeAndDay(DayOfWeek.Saturday, new TimeOnly(1, 0)),
                new TimeAndDay(DayOfWeek.Saturday, new TimeOnly(22, 0)),
                new TimeAndDay(DayOfWeek.Sunday, new TimeOnly(1, 0)),
            ]
        );
        _context.ChangeTracker.Clear();
        _signals.Invocations.Clear();
    }

    private AutoScheduleService Worker(
        IServiceProvider services,
        AutoScheduleJobs? registry = null,
        IMissionRunService? runs = null,
        IAccessRoleService? roles = null
    ) =>
        new(
            NullLogger<AutoScheduleService>.Instance,
            services.GetRequiredService<IMissionDefinitionService>(),
            services.GetRequiredService<IRobotService>(),
            runs ?? services.GetRequiredService<IMissionRunService>(),
            _scheduling.Object,
            _signals.Object,
            roles ?? services.GetRequiredService<IAccessRoleService>(),
            _jobs.Object,
            services.GetRequiredService<FlotillaDbContext>(),
            _clock,
            registry ?? _registry
        );

    public async ValueTask DisposeAsync()
    {
        _scope?.Dispose();
        if (_factory != null)
            await _factory.DisposeAsync();
        if (_container != null)
            await _container.DisposeAsync();
    }

    private async Task<AutoScheduleOccurrenceResponse> Next() =>
        (await _service.ReadUpcoming(_definition.InstallationCode))[0];

    private Task Dispatch(AutoScheduleOccurrenceResponse next) =>
        _service.DispatchOccurrence(_definition.Id, next.ScheduledAt);

    private Task<MissionRun> Persisted(DateTimeOffset scheduledAt) =>
        _context
            .MissionRuns.Include(r => r.Tasks)
            .Include(r => r.Robot)
            .SingleAsync(
                r => r.MissionId == _definition.Id && r.AutoScheduledAt == scheduledAt,
                Token
            );

    [Fact]
    public async Task UpcomingIsDerivedAndReconciliationDoesNotDuplicateJobs()
    {
        Assert.Same(TimeProvider.System, _scope.ServiceProvider.GetRequiredService<TimeProvider>());
        Assert.NotNull(_scope.ServiceProvider.GetRequiredService<AutoScheduleJobs>());
        Assert.NotNull(_scope.ServiceProvider.GetRequiredService<IAutoScheduleService>());
        var upcoming = await _service.ReadUpcoming(_definition.InstallationCode);
        Assert.Equal(3, upcoming.Count);
        Assert.Equal(_clock.Now.AddHours(24), upcoming[^1].ScheduledAt);
        Assert.All(upcoming, o => Assert.Equal(AutoScheduleOccurrenceStatus.Scheduled, o.Status));
        Assert.Empty(await _context.MissionRuns.ToListAsync(Token));
        Assert.Empty(await _context.AutoScheduleSkips.ToListAsync(Token));
        using var a = _factory.Services.CreateScope();
        using var b = _factory.Services.CreateScope();
        await Task.WhenAll(
            Worker(a.ServiceProvider).Reconcile(_definition),
            Worker(b.ServiceProvider).Reconcile(_definition)
        );
        Assert.Equal(4, _jobNumber);
        Assert.All(
            _jobs.Invocations,
            i =>
            {
                var job = Assert.IsType<Job>(i.Arguments[0]);
                var state = Assert.IsType<ScheduledState>(i.Arguments[1]);
                Assert.Equal(((DateTimeOffset)job.Args[1]).UtcDateTime, state.EnqueueAt);
            }
        );
    }

    [Fact]
    public async Task SkipsSurviveRestartAndRemovingAndReaddingWeeklySlots()
    {
        var next = await Next();
        var frequency = _definition.AutoScheduleFrequency!.SchedulingTimesCETperWeek;
        using var concurrent = _factory.Services.CreateScope();
        var skips = await Task.WhenAll(
            _service.SkipOccurrence(_definition, next.ScheduledAt),
            Worker(concurrent.ServiceProvider).SkipOccurrence(_definition, next.ScheduledAt)
        );
        Assert.All(skips, Assert.True);
        Assert.DoesNotContain(
            _context.ChangeTracker.Entries<AutoScheduleSkip>(),
            e => e.State == EntityState.Added
        );
        Assert.DoesNotContain(
            concurrent
                .ServiceProvider.GetRequiredService<FlotillaDbContext>()
                .ChangeTracker.Entries<AutoScheduleSkip>(),
            e => e.State == EntityState.Added
        );
        var savedSkip = await _context.AutoScheduleSkips.SingleAsync(Token);
        Assert.True(savedSkip.Id > 0);
        Assert.True(await _service.SkipOccurrence(_definition, next.ScheduledAt));
        Assert.Empty(_context.ChangeTracker.Entries<AutoScheduleSkip>());
        Assert.Equal(savedSkip.Id, (await _context.AutoScheduleSkips.SingleAsync(Token)).Id);
        await _service.UpdateAutoScheduleFrequency(_definition, []);
        Assert.Empty(await _service.ReadUpcoming(_definition.InstallationCode));
        await _service.UpdateAutoScheduleFrequency(_definition, frequency);
        using var scope = _factory.Services.CreateScope();
        var restarted = Worker(scope.ServiceProvider, new AutoScheduleJobs(_clock));
        await restarted.Reconcile(_definition);
        Assert.Equal(
            AutoScheduleOccurrenceStatus.Skipped,
            (await restarted.ReadUpcoming(_definition.InstallationCode))[0].Status
        );
        Assert.Single(await _context.AutoScheduleSkips.ToListAsync(Token));
        _clock.Now = next.ScheduledAt;
        await restarted.DispatchOccurrence(_definition.Id, next.ScheduledAt);
        Assert.Empty(await _context.MissionRuns.ToListAsync(Token));
    }

    [Fact]
    public async Task UnskipAfterRestartRegistersMissingJobAtOriginalTimeAndNotifiesAfterCommit()
    {
        var next = await Next();
        var tomorrow = (await _service.ReadUpcoming(_definition.InstallationCode))[1];
        await _service.SkipOccurrence(_definition, next.ScheduledAt);
        await _service.SkipOccurrence(_definition, tomorrow.ScheduledAt);
        using var scope = _factory.Services.CreateScope();
        var restarted = Worker(scope.ServiceProvider, new AutoScheduleJobs(_clock));
        await restarted.Reconcile(_definition);
        var before = _jobNumber;
        _signals
            .Setup(s =>
                s.ReportAutoScheduleToSignalR(
                    "unskipAutoMission",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .Callback(() =>
            {
                using var readScope = _factory.Services.CreateScope();
                Assert.Equal(
                    tomorrow.ScheduledAt,
                    Assert
                        .Single(
                            readScope
                                .ServiceProvider.GetRequiredService<FlotillaDbContext>()
                                .AutoScheduleSkips.ToList()
                        )
                        .ScheduledAt
                );
            });
        Assert.True(await restarted.UnskipOccurrence(_definition, next.ScheduledAt));
        Assert.Equal(before + 1, _jobNumber);
        var invocation = _jobs.Invocations.Last();
        Assert.Equal(
            next.ScheduledAt.UtcDateTime,
            Assert.IsType<ScheduledState>(invocation.Arguments[1]).EnqueueAt
        );
        Assert.Equal(next.ScheduledAt, Assert.IsType<Job>(invocation.Arguments[0]).Args[1]);
        _signals.Verify(
            s =>
                s.ReportAutoScheduleToSignalR(
                    "unskipAutoMission",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ),
            Times.Once
        );
        await restarted.Reconcile(_definition);
        Assert.True(await restarted.UnskipOccurrence(_definition, next.ScheduledAt));
        Assert.Equal(before + 1, _jobNumber);
        _clock.Now = next.ScheduledAt;
        await restarted.DispatchOccurrence(_definition.Id, next.ScheduledAt);
        Assert.Equal(
            next.ScheduledAt,
            (await _context.MissionRuns.SingleAsync(Token)).AutoScheduledAt
        );
    }

    [Fact]
    public async Task SkipEndpointsUseExactTimestampsAndRejectMissingOrMalformedInput()
    {
        var instant = _clock.Now.AddHours(1).AddMicroseconds(123456);
        var local = TimeZoneInfo.ConvertTime(instant, TimeZoneUtilities.CetZone);
        await _service.UpdateAutoScheduleFrequency(
            _definition,
            [new TimeAndDay(local.DayOfWeek, TimeOnly.FromDateTime(local.DateTime))]
        );
        await using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IAutoScheduleService>(provider => Worker(provider))
            )
        );
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthHandler.AuthenticationScheme
        );
        var path = $"missions/definitions/{_definition.Id}/skip-auto-mission";
        var options = TestSetupHelpers.ConfigureJsonSerializerOptions();
        var upcoming = await client.GetFromJsonAsync<AutoScheduleOccurrenceResponse[]>(
            $"missions/definitions/auto-scheduled?installationCode={_definition.InstallationCode}",
            options,
            Token
        );
        Assert.Equal(instant, Assert.Single(upcoming!).ScheduledAt);
        using var skipped = await client.PutAsJsonAsync(path, new { scheduledAt = instant }, Token);
        Assert.Equal(HttpStatusCode.NoContent, skipped.StatusCode);
        Assert.Equal(instant, (await _context.AutoScheduleSkips.SingleAsync(Token)).ScheduledAt);
        var offsetTimestamp = Uri.EscapeDataString(
            instant.ToOffset(TimeSpan.FromHours(2)).ToString("O")
        );
        using var restored = await client.DeleteAsync(
            $"{path}?scheduledAt={offsetTimestamp}",
            Token
        );
        Assert.Equal(HttpStatusCode.NoContent, restored.StatusCode);
        Assert.Empty(await _context.AutoScheduleSkips.ToListAsync(Token));
        using var missing = await client.PutAsJsonAsync(path, new { }, Token);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        foreach (var suffix in new[] { "", "?scheduledAt=invalid" })
        {
            using var invalid = await client.DeleteAsync(path + suffix, Token);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
    }

    [Fact]
    public async Task FailedUnskipRegistrationRollsBackSkipRemovalWithoutSuccessNotification()
    {
        var next = await Next();
        await _service.SkipOccurrence(_definition, next.ScheduledAt);
        using var scope = _factory.Services.CreateScope();
        var restarted = Worker(scope.ServiceProvider, new AutoScheduleJobs(_clock));
        _jobs
            .Setup(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Throws(new InvalidOperationException("Registration unavailable"));
        _signals.Invocations.Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            restarted.UnskipOccurrence(_definition, next.ScheduledAt)
        );
        Assert.Single(await _context.AutoScheduleSkips.ToListAsync(Token));
        Assert.Equal(AutoScheduleOccurrenceStatus.Skipped, (await Next()).Status);
        Assert.Empty(_signals.Invocations);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SkipChangesRejectInvalidPastRemovedAndOutOfWindowOccurrences(bool skip)
    {
        Task<bool> Change(DateTimeOffset scheduledAt) =>
            skip
                ? _service.SkipOccurrence(_definition, scheduledAt)
                : _service.UnskipOccurrence(_definition, scheduledAt);

        var next = await Next();
        await _service.SkipOccurrence(_definition, next.ScheduledAt);
        var outside = _definition
            .AutoScheduleFrequency!.GetSchedulingInstants(
                _clock.Now.AddHours(24),
                _clock.Now.AddHours(48)
            )
            .Single();
        _signals.Invocations.Clear();
        foreach (
            var instant in new[]
            {
                default(DateTimeOffset),
                next.ScheduledAt.AddSeconds(1),
                outside,
            }
        )
            Assert.False(await Change(instant));
        Assert.Empty(_signals.Invocations);
        var frequency = _definition.AutoScheduleFrequency!.SchedulingTimesCETperWeek;
        await _service.UpdateAutoScheduleFrequency(_definition, []);
        Assert.False(await Change(next.ScheduledAt));
        await _service.UpdateAutoScheduleFrequency(_definition, frequency);
        _clock.Now = next.ScheduledAt;
        Assert.False(await Change(next.ScheduledAt));
        Assert.Single(await _context.AutoScheduleSkips.ToListAsync(Token));
        await Dispatch(next);
        Assert.Empty(await _context.MissionRuns.ToListAsync(Token));
    }

    [Fact]
    public async Task ExactEligibilityAndDuplicateJobsCreateOneRunAndWakeUp()
    {
        var next = await Next();
        _clock.Now = next.ScheduledAt.AddTicks(-1);
        await Dispatch(next);
        Assert.Empty(await _context.MissionRuns.ToListAsync(Token));
        _clock.Now = next.ScheduledAt;
        using var a = _factory.Services.CreateScope();
        using var b = _factory.Services.CreateScope();
        var creating = 0;
        var bothCreating = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        IMissionRunService ConcurrentRuns(IServiceProvider services)
        {
            var real = services.GetRequiredService<IMissionRunService>();
            var runs = new Mock<IMissionRunService>();
            runs.Setup(r => r.ReadMissionRunQueue(It.IsAny<string>(), true)).ReturnsAsync([]);
            runs.Setup(r => r.Create(It.IsAny<MissionRun>()))
                .Returns(
                    async (MissionRun run) =>
                    {
                        if (Interlocked.Increment(ref creating) == 2)
                            bothCreating.SetResult();
                        await bothCreating.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
                        return await real.Create(run);
                    }
                );
            return runs.Object;
        }
        await Task.WhenAll(
            Worker(a.ServiceProvider, runs: ConcurrentRuns(a.ServiceProvider))
                .DispatchOccurrence(_definition.Id, next.ScheduledAt),
            Worker(b.ServiceProvider, runs: ConcurrentRuns(b.ServiceProvider))
                .DispatchOccurrence(_definition.Id, next.ScheduledAt)
        );
        Assert.Equal(2, creating);
        await Dispatch(next);
        Assert.Single(await _context.MissionRuns.ToListAsync(Token));
        Assert.Single((await Persisted(next.ScheduledAt)).Tasks);
        _scheduling.Verify(
            s => s.StartNextMissionRunIfSystemIsAvailable(It.IsAny<Robot>()),
            Times.Once
        );
        _signals.Verify(
            s =>
                s.SendMessageAsync(
                    "Mission run created",
                    It.IsAny<Installation>(),
                    It.IsAny<MissionRunResponse>()
                ),
            Times.Once
        );
        await _runs.Delete((await Persisted(next.ScheduledAt)).Id);
        await Dispatch(next);
        Assert.Single(await _context.MissionRuns.ToListAsync(Token));
    }

    [Fact]
    public async Task RunUniquenessUsesMissionAndScheduledTimeWithoutRestrictingManualRuns()
    {
        var scheduledAt = (await Next()).ScheduledAt;
        Task<MissionRun> Create(DateTimeOffset? instant, string? missionId = null) =>
            _runs.Create(
                new MissionRun
                {
                    MissionId = missionId ?? _definition.Id,
                    AutoScheduledAt = instant,
                    Name = "Inspection",
                    Robot = _robot,
                    InspectionArea = _definition.InspectionArea,
                    InstallationCode = _definition.InstallationCode,
                    CreationTime = _clock.Now.UtcDateTime,
                    Tasks = [],
                }
            );

        var first = await Create(scheduledAt);
        var duplicate = await Assert.ThrowsAsync<DbUpdateException>(() =>
            Create(scheduledAt.AddTicks(9))
        );
        Assert.Equal(
            "IX_MissionRuns_MissionId_AutoScheduledAt",
            Assert.IsType<PostgresException>(duplicate.InnerException).ConstraintName
        );
        _context.ChangeTracker.Clear();
        await Create(scheduledAt.AddMicroseconds(1));
        await Create(scheduledAt, "another-mission");
        await Create(null);
        await Create(null);
        var runs = await _context.MissionRuns.ToListAsync(Token);
        Assert.Equal(5, runs.Count);
        Assert.Equal(5, runs.Select(r => r.Id).Distinct().Count());
        Assert.All(runs, r => Assert.True(Guid.TryParse(r.Id, out _)));
        Assert.Equal(scheduledAt, (await Persisted(scheduledAt)).AutoScheduledAt);
        Assert.Equal(first.Id, (await Persisted(scheduledAt)).Id);
    }

    [Fact]
    public async Task RestartDoesNotCatchUpButLiveProcessLateJobRemainsEligible()
    {
        var next = await Next();
        _clock.Now = next.ScheduledAt.AddMinutes(10);
        using var scope = _factory.Services.CreateScope();
        var restarted = Worker(scope.ServiceProvider, new AutoScheduleJobs(_clock));
        var jobsBefore = _jobNumber;
        await restarted.Reconcile(_definition);
        Assert.Equal(jobsBefore + 3, _jobNumber);
        await restarted.DispatchOccurrence(_definition.Id, next.ScheduledAt);
        Assert.Empty(await _context.MissionRuns.ToListAsync(Token));
        await Dispatch(next);
        Assert.Single(await _context.MissionRuns.ToListAsync(Token));
    }

    [Fact]
    public async Task RemovedOrDeletedDefinitionMakesPreviouslyRegisteredJobHarmless()
    {
        var next = await Next();
        var frequency = _definition.AutoScheduleFrequency!.SchedulingTimesCETperWeek;
        await _service.UpdateAutoScheduleFrequency(_definition, []);
        _clock.Now = next.ScheduledAt;
        await Dispatch(next);
        Assert.Empty(await _context.MissionRuns.ToListAsync(Token));
        await _service.UpdateAutoScheduleFrequency(_definition, frequency);
        await _definitions.Delete(_definition.Id);
        await Dispatch(next);
        Assert.Empty(await _context.MissionRuns.ToListAsync(Token));
    }

    [Fact]
    public async Task SuccessfulRunReferenceAndDeprecationStillPersist()
    {
        var next = await Next();
        _clock.Now = next.ScheduledAt;
        await Dispatch(next);
        var runId = (await Persisted(next.ScheduledAt)).Id;
        await _context
            .MissionRuns.Where(r => r.Id == runId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, MissionStatus.Successful), Token);
        _context.ChangeTracker.Clear();
        await _definitions.UpdateLastSuccessfulMissionRun(runId, _definition.Id);
        await _definitions.UpdateLastSuccessfulMissionRun(runId, _definition.Id);
        Assert.Equal(runId, (await _definitions.ReadById(_definition.Id))!.LastSuccessfulRun!.Id);

        var controller = ActivatorUtilities.CreateInstance<MissionDefinitionController>(
            _scope.ServiceProvider,
            _service
        );
        await controller.UpdateMissionDefinitionIsDeprecatedById(
            _definition.Id,
            new UpdateMissionDefinitionIsDeprecatedQuery { IsDeprecated = true }
        );
        Assert.Null(await _definitions.ReadById(_definition.Id));
        Assert.True(
            await _context.MissionDefinitions.AnyAsync(
                d => d.Id == _definition.Id && d.IsDeprecated,
                Token
            )
        );
        Assert.Single(await _context.MissionRuns.ToListAsync(Token));
    }

    [Fact]
    public async Task DispatchUsesCurrentTaskSnapshotAndRobotAssignment()
    {
        var next = await Next();
        var current = (await _definitions.ReadById(_definition.Id))!;
        current.Tasks[0].TagId = "edited-task";
        await _definitions.Update(current);
        var database = _scope.ServiceProvider.GetRequiredService<DatabaseUtilities>();
        var replacement = await database.NewRobot(
            RobotStatus.Available,
            _definition.InspectionArea.Installation,
            _definition.InspectionArea.Id
        );
        await _context
            .Robots.Where(r => r.Id == _robot.Id)
            .ExecuteUpdateAsync(
                s => s.SetProperty(r => r.CurrentInspectionAreaId, (string?)null),
                Token
            );
        _clock.Now = next.ScheduledAt;
        await Dispatch(next);
        var run = await Persisted(next.ScheduledAt);
        Assert.Equal(replacement.Id, run.Robot.Id);
        Assert.Equal("edited-task", Assert.Single(run.Tasks).TagId);
        Assert.Equal(next.ScheduledAt.UtcDateTime, run.CreationTime);
    }

    [Fact]
    public async Task CombinedPatchKeepsExplicitEditsAndTaskOnlyPatchKeepsSchedule()
    {
        var controller = ActivatorUtilities.CreateInstance<MissionDefinitionController>(
            _scope.ServiceProvider,
            _service
        );
        var originalFrequency = _definition.AutoScheduleFrequency!.SchedulingTimesCETperWeek;
        var query = new UpdateMissionDefinitionQuery
        {
            Name = "combined name",
            Comment = "combined comment",
            SchedulingTimesCETperWeek = [],
            Tasks =
            [
                new TaskQuery
                {
                    TagId = "combined task",
                    AnalysisTypes = [],
                    SensorType = SensorType.Image,
                    RobotPose = new Pose(),
                    TargetPosition = new Position(),
                },
                new TaskQuery
                {
                    TagId = "second task",
                    AnalysisTypes = [],
                    SensorType = SensorType.Image,
                    RobotPose = new Pose(),
                    TargetPosition = new Position(),
                },
            ],
        };
        await controller.UpdateMissionDefinitionById(_definition.Id, query);
        var actual = (await _definitions.ReadById(_definition.Id))!;
        Assert.Equal(query.Name, actual.Name);
        Assert.Equal(query.Comment, actual.Comment);
        Assert.Equal(2, actual.Tasks.Count);
        Assert.Equal("combined task", actual.Tasks[0].TagId);
        Assert.Null(actual.AutoScheduleFrequency);
        await _service.UpdateAutoScheduleFrequency(_definition, originalFrequency);
        actual = (await _definitions.ReadById(_definition.Id))!;
        Assert.Equal(query.Name, actual.Name);
        Assert.Equal(query.Comment, actual.Comment);
        Assert.Equal("combined task", actual.Tasks[0].TagId);
        await controller.UpdateMissionDefinitionById(
            _definition.Id,
            new UpdateMissionDefinitionQuery { Name = "task only", Tasks = [query.Tasks[0]] }
        );
        actual = (await _definitions.ReadById(_definition.Id))!;
        Assert.Equal("task only", actual.Name);
        Assert.Single(actual.Tasks);
        Assert.Equal(4, actual.AutoScheduleFrequency!.SchedulingTimesCETperWeek.Count);
    }

    [Fact]
    public async Task ReadyWorkKeepsItsQueuePositionAndSuppressesAnotherRunOfTheSameMission()
    {
        var next = await Next();
        var manual = await _runs.Create(
            new MissionRun
            {
                MissionId = "manual",
                Name = "Manual",
                Robot = _robot,
                InspectionArea = _definition.InspectionArea,
                InstallationCode = _definition.InstallationCode,
                CreationTime = _clock.Now.UtcDateTime,
                Tasks = [],
            }
        );
        _clock.Now = next.ScheduledAt;
        await Dispatch(next);
        var queue = await _runs.ReadMissionRunQueue(_robot.Id);
        Assert.Equal(
            new[] { manual.Id, (await Persisted(next.ScheduledAt)).Id },
            queue.Select(r => r.Id)
        );
        next = await Next();
        _clock.Now = next.ScheduledAt;
        await Dispatch(next);
        Assert.Equal(
            queue.Select(r => r.Id),
            (await _runs.ReadMissionRunQueue(_robot.Id)).Select(r => r.Id)
        );
        _scheduling.Verify(
            s => s.StartNextMissionRunIfSystemIsAvailable(It.IsAny<Robot>()),
            Times.Once
        );
    }

    [Fact]
    public async Task RetriedDispatchCreatesOneRunAndNotifiesOnlyAfterCommit()
    {
        var next = await Next();
        var notifications = 0;
        _signals
            .Setup(s =>
                s.SendMessageAsync(
                    "Mission run created",
                    It.IsAny<Installation>(),
                    It.IsAny<MissionRunResponse>()
                )
            )
            .Returns(() =>
            {
                using var scope = _factory.Services.CreateScope();
                Assert.True(
                    scope
                        .ServiceProvider.GetRequiredService<FlotillaDbContext>()
                        .MissionRuns.Any(r =>
                            r.MissionId == _definition.Id && r.AutoScheduledAt == next.ScheduledAt
                        )
                );
                notifications++;
                return Task.CompletedTask;
            });
        _clock.Now = next.ScheduledAt;
        _retry.FailOnce = true;
        await Dispatch(next);
        Assert.False(_retry.FailOnce);
        Assert.Equal(1, notifications);
        Assert.Single(await _context.MissionRuns.ToListAsync(Token));
        Assert.Single((await Persisted(next.ScheduledAt)).Tasks);
        _scheduling.Verify(
            s => s.StartNextMissionRunIfSystemIsAvailable(It.IsAny<Robot>()),
            Times.Once
        );
    }

    [Fact]
    public async Task MissingRobotReportsFailureWithoutCreatingFutureOrIncompleteRun()
    {
        var next = await Next();
        await _context
            .Robots.Where(r => r.Id == _robot.Id)
            .ExecuteUpdateAsync(
                s => s.SetProperty(r => r.CurrentInspectionAreaId, (string?)null),
                Token
            );
        _clock.Now = next.ScheduledAt;
        await Dispatch(next);
        Assert.Empty(await _context.MissionRuns.ToListAsync(Token));
        _signals.Verify(
            s =>
                s.ReportAutoScheduleToSignalR(
                    "AutoScheduleFail",
                    _definition.Name,
                    It.IsAny<string>(),
                    _definition.InstallationCode
                ),
            Times.Once
        );
        _scheduling.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReadAndWriteAccessFilterDerivedOccurrencesAndDefinitions()
    {
        using var scope = _factory.Services.CreateScope();
        var roles = new Mock<IAccessRoleService>();
        roles.Setup(r => r.GetAllowedInstallationCodes(AccessMode.Read)).ReturnsAsync(["OTHER"]);
        roles.Setup(r => r.GetAllowedInstallationCodes(AccessMode.Write)).ReturnsAsync(["OTHER"]);
        var definitions = ActivatorUtilities.CreateInstance<MissionDefinitionService>(
            scope.ServiceProvider,
            roles.Object
        );
        var service = ActivatorUtilities.CreateInstance<AutoScheduleService>(
            scope.ServiceProvider,
            definitions,
            _clock,
            _registry,
            _jobs.Object
        );
        Assert.Empty(await service.ReadUpcoming(_definition.InstallationCode));
        Assert.Null(await definitions.ReadById(_definition.Id, accessMode: AccessMode.Write));
    }

    [Fact]
    public async Task UnsupportedCapabilitiesNeverCreateRunOrSendSuccess()
    {
        var next = await Next();
        var current = (await _definitions.ReadById(_definition.Id))!;
        current.Tasks[0].SensorType = SensorType.ThermalImage;
        await _definitions.Update(current);
        _clock.Now = next.ScheduledAt;
        await Assert.ThrowsAsync<UnsupportedRobotCapabilityException>(() => Dispatch(next));
        Assert.Empty(await _context.MissionRuns.ToListAsync(Token));
        Assert.Empty(_signals.Invocations);
        _scheduling.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FractionalSlotsShareOneIdentityAcrossSkipRestartAndDispatch()
    {
        var instant = _clock.Now.AddHours(1).AddTicks(1234567);
        var expected = TimeZoneUtilities.ToPostgresPrecision(instant);
        var local = TimeZoneInfo.ConvertTime(instant, TimeZoneUtilities.CetZone);
        var time = TimeOnly.FromDateTime(local.DateTime);
        await _service.UpdateAutoScheduleFrequency(
            _definition,
            [
                new TimeAndDay(local.DayOfWeek, time),
                new TimeAndDay(local.DayOfWeek, time.Add(TimeSpan.FromTicks(1))),
            ]
        );
        var next = Assert.Single(await _service.ReadUpcoming(_definition.InstallationCode));
        Assert.Equal(expected, next.ScheduledAt);
        Assert.True(
            await _service.SkipOccurrence(_definition, instant.ToOffset(TimeSpan.FromHours(2)))
        );
        Assert.Equal(expected, (await _context.AutoScheduleSkips.SingleAsync(Token)).ScheduledAt);
        Assert.Equal(
            expected,
            await _context
                .Database.SqlQuery<DateTimeOffset>($"SELECT {instant} AS \"Value\"")
                .SingleAsync(Token)
        );
        using var scope = _factory.Services.CreateScope();
        var restarted = Worker(scope.ServiceProvider, new AutoScheduleJobs(_clock));
        await restarted.Reconcile(_definition);
        var upcoming = Assert.Single(await restarted.ReadUpcoming(_definition.InstallationCode));
        Assert.Equal(next.ScheduledAt, upcoming.ScheduledAt);
        Assert.Equal(AutoScheduleOccurrenceStatus.Skipped, upcoming.Status);
        Assert.True(await restarted.UnskipOccurrence(_definition, instant));
        _clock.Now = expected.AddTicks(-1);
        await restarted.DispatchOccurrence(_definition.Id, instant);
        Assert.Empty(await _context.MissionRuns.ToListAsync(Token));
        _clock.Now = expected;
        await restarted.DispatchOccurrence(_definition.Id, instant);
        await restarted.DispatchOccurrence(_definition.Id, expected);
        Assert.Equal(
            next.ScheduledAt,
            (await _context.MissionRuns.SingleAsync(Token)).AutoScheduledAt
        );
        _scheduling.Verify(
            s => s.StartNextMissionRunIfSystemIsAvailable(It.IsAny<Robot>()),
            Times.Once
        );
    }

    [Fact]
    public async Task MigrationsPreserveDefinitionsRunsAndSkipUniqueness()
    {
        var next = await Next();
        _clock.Now = next.ScheduledAt;
        await Dispatch(next);
        var originalRunId = (await Persisted(next.ScheduledAt)).Id;
        var schedulingMigration = _context
            .Database.GetMigrations()
            .Single(m => m.EndsWith("_AddDatedAutoScheduling", StringComparison.Ordinal));
        var previous = _context
            .Database.GetMigrations()
            .TakeWhile(m => m != schedulingMigration)
            .Last();
        await _context.GetService<IMigrator>().MigrateAsync(previous, Token);
        const string legacyJobs = """{"23:00:00":"old-job"}""";
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE "MissionDefinitions" SET "AutoScheduleFrequency_AutoScheduledJobs" = {legacyJobs} WHERE "Id" = {_definition.Id}""",
            Token
        );
        await _context.Database.MigrateAsync(Token);
        var oldRun = await _context.MissionRuns.SingleAsync(Token);
        Assert.Equal(originalRunId, oldRun.Id);
        Assert.Null(oldRun.AutoScheduledAt);
        Assert.Empty(await _context.AutoScheduleSkips.ToListAsync(Token));
        var skip = new AutoScheduleSkip
        {
            MissionDefinitionId = _definition.Id,
            ScheduledAt = next.ScheduledAt,
        };
        _context.AutoScheduleSkips.Add(skip);
        await _context.SaveChangesAsync(Token);
        Assert.True(skip.Id > 0);
        var duplicate = new AutoScheduleSkip
        {
            MissionDefinitionId = _definition.Id,
            ScheduledAt = next.ScheduledAt.AddTicks(9),
        };
        _context.AutoScheduleSkips.Add(duplicate);
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync(Token));
        _context.ChangeTracker.Clear();
        var another = new AutoScheduleSkip
        {
            MissionDefinitionId = _definition.Id,
            ScheduledAt = next.ScheduledAt.AddMicroseconds(1),
        };
        _context.AutoScheduleSkips.Add(another);
        await _context.SaveChangesAsync(Token);
        Assert.True(another.Id > 0);
        Assert.NotEqual(skip.Id, another.Id);
        _context.ChangeTracker.Clear();

        await _context.GetService<IMigrator>().MigrateAsync(previous, Token);
        await _context.Database.MigrateAsync(Token);
        Assert.Single(await _context.MissionRuns.ToListAsync(Token));
        Assert.Equal(
            4,
            (await _definitions.ReadById(_definition.Id))!
                .AutoScheduleFrequency!
                .SchedulingTimesCETperWeek
                .Count
        );
        Assert.False(_context.Database.HasPendingModelChanges());
        var columns = await _context
            .Database.SqlQueryRaw<string>(
                """SELECT column_name AS "Value" FROM information_schema.columns WHERE table_name = 'AutoScheduleSkips'"""
            )
            .ToListAsync(Token);
        Assert.Equal(new[] { "Id", "MissionDefinitionId", "ScheduledAt" }, columns.Order());
        Assert.Equal(
            "timestamp with time zone",
            await _context
                .Database.SqlQueryRaw<string>(
                    """SELECT data_type AS "Value" FROM information_schema.columns WHERE table_name = 'AutoScheduleSkips' AND column_name = 'ScheduledAt'"""
                )
                .SingleAsync(Token)
        );
        Assert.DoesNotContain(
            await _context
                .Database.SqlQueryRaw<string>(
                    """SELECT column_name AS "Value" FROM information_schema.columns WHERE table_name = 'MissionDefinitions'"""
                )
                .ToListAsync(Token),
            c => c == "AutoScheduleFrequency_AutoScheduledJobs"
        );
    }
}
