using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.HostedServices
{
    public class TestScheduledMissionRuns : IAsyncLifetime
    {
        private PostgreSqlContainer _container = null!;
        private TestWebApplicationFactory<Program> _factory = null!;
        private IServiceScope _scope = null!;
        private FlotillaDbContext _context = null!;
        private IMissionDefinitionService _definitions = null!;
        private IMissionRunService _runs = null!;
        private MissionDefinition _definition = null!;
        private Robot _robot = null!;
        private AutoScheduleService _service = null!;
        private readonly Clock _clock = new();
        private readonly Mock<IMissionSchedulingService> _scheduling = new();
        private readonly Mock<IBackgroundJobClient> _jobs = new(MockBehavior.Strict);
        private readonly Mock<ILogger<AutoScheduleService>> _logger = new();
        private readonly RetryInterceptor _retry = new();
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
                if (
                    FailOnce
                    && eventData
                        .Context!.ChangeTracker.Entries<MissionRun>()
                        .Any(e => e.Entity.Status == MissionStatus.Queued)
                )
                {
                    FailOnce = false;
                    throw new NpgsqlException(
                        "Simulated transient write failure",
                        new TimeoutException()
                    );
                }
                return ValueTask.FromResult(result);
            }
        }

        private sealed class SchedulingFactory : TestWebApplicationFactory<Program>
        {
            private readonly string _connectionString;
            private readonly RetryInterceptor _retry;

            public SchedulingFactory(string connectionString, RetryInterceptor retry)
                : base(connectionString)
            {
                _connectionString = connectionString;
                _retry = retry;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);
                builder.ConfigureTestServices(services =>
                    services.AddScoped<FlotillaDbContext>(_ => new FlotillaDbContext(
                        new DbContextOptionsBuilder<FlotillaDbContext>()
                            .UseNpgsql(_connectionString, o => o.EnableRetryOnFailure())
                            .AddInterceptors(_retry)
                            .Options
                    ))
                );
            }
        }

        public async ValueTask InitializeAsync()
        {
            var setup = await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            _container = setup.Item1;
            await setup.Item3.DisposeAsync();
            _factory = new SchedulingFactory(setup.Item2, _retry);
            _scope = _factory.Services.CreateScope();
            var services = _scope.ServiceProvider;
            _context = services.GetRequiredService<FlotillaDbContext>();
            _definitions = services.GetRequiredService<IMissionDefinitionService>();
            _runs = services.GetRequiredService<IMissionRunService>();
            var database = services.GetRequiredService<DatabaseUtilities>();
            var installation = await database.NewInstallation();
            var plant = await database.NewPlant(installation.InstallationCode);
            var area = await database.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
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
                .Returns(() => $"job-{++_jobNumber}");
            _jobs
                .Setup(j => j.ChangeState(It.IsAny<string>(), It.IsAny<DeletedState>(), null))
                .Returns(true);
            _service = Worker(services, _clock);
            await _service.UpdateAutoScheduleFrequency(
                _definition,
                [
                    new TimeAndDay(DayOfWeek.Friday, new TimeOnly(23, 0)),
                    new TimeAndDay(DayOfWeek.Saturday, new TimeOnly(1, 0)),
                    new TimeAndDay(DayOfWeek.Saturday, new TimeOnly(23, 0)),
                    new TimeAndDay(DayOfWeek.Sunday, new TimeOnly(1, 0)),
                ]
            );
        }

        private AutoScheduleService Worker(
            IServiceProvider services,
            TimeProvider clock,
            IMissionRunService? runs = null
        ) =>
            new(
                _logger.Object,
                services.GetRequiredService<IMissionDefinitionService>(),
                services.GetRequiredService<IRobotService>(),
                runs ?? services.GetRequiredService<IMissionRunService>(),
                _scheduling.Object,
                services.GetRequiredService<ISignalRService>(),
                services.GetRequiredService<IAccessRoleService>(),
                _jobs.Object,
                services.GetRequiredService<FlotillaDbContext>(),
                clock
            );

        public async ValueTask DisposeAsync()
        {
            _scope?.Dispose();
            if (_factory != null)
                await _factory.DisposeAsync();
            if (_container != null)
                await _container.DisposeAsync();
        }

        private Task<MissionRun> Persisted(string id) =>
            _context
                .MissionRuns.Include(r => r.Tasks)
                .Include(r => r.Robot)
                .SingleAsync(r => r.Id == id, Token);

        private async Task<ScheduledMissionRunResponse> Next() =>
            (await _service.ReadUpcoming(_definition.InstallationCode))[0];

        [Fact]
        public async Task MigrationRollbackDiscardsOnlyUnactivatedRowsAndCanBeReapplied()
        {
            var next = await Next();
            _clock.Now = next.ScheduledAt;
            await _service.ActivateMissionRun(next.Id);
            var previous = _context.Database.GetMigrations().Reverse().Skip(1).First();
            await _context.GetService<IMigrator>().MigrateAsync(previous, Token);
            Assert.Equal(1, await _context.MissionRuns.CountAsync(Token));
            await _context.Database.MigrateAsync(Token);
            var retained = await Persisted(next.Id);
            Assert.NotNull(retained.Robot);
            Assert.Single(retained.Tasks);
            Assert.Null(retained.ScheduledAt);
            Assert.False(_context.Database.HasPendingModelChanges());
        }

        [Fact]
        public async Task ActivationDoesNotJumpAheadOfAlreadyReadyWork()
        {
            var next = await Next();
            var manual = await _runs.Create(
                new MissionRun
                {
                    MissionId = "other-mission",
                    Name = "Already ready",
                    Robot = _robot,
                    InstallationCode = _definition.InstallationCode,
                    InspectionArea = _definition.InspectionArea,
                    CreationTime = next.ScheduledAt.AddMinutes(-1).UtcDateTime,
                    Tasks = [],
                }
            );
            _clock.Now = next.ScheduledAt;
            await _service.ActivateMissionRun(next.Id);
            Assert.Equal(manual.Id, (await _runs.ReadNextScheduledMissionRun(_robot.Id))!.Id);
            Assert.Equal(
                new[] { manual.Id, next.Id },
                (await _runs.ReadMissionRunQueue(_robot.Id)).Select(r => r.Id)
            );
        }

        [Fact]
        public async Task UnsupportedCapabilitiesFailWithoutQueueingOrLeavingTaskSnapshots()
        {
            var next = await Next();
            await _context
                .Robots.Where(r => r.Id == _robot.Id)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(r => r.RobotCapabilities, new List<RobotCapabilitiesEnum>()),
                    Token
                );
            _clock.Now = next.ScheduledAt;
            await _service.ActivateMissionRun(next.Id);
            var run = await Persisted(next.Id);
            Assert.Equal(MissionStatus.Failed, run.Status);
            Assert.Null(run.Robot);
            Assert.Null(run.ActivatedAt);
            Assert.Empty(run.Tasks);
            Assert.Empty(await _runs.ReadMissionRunQueue(_robot.Id));
            _scheduling.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task RemovedSlotCannotActivateEvenIfItsOldJobStillRuns()
        {
            var next = await Next();
            await _service.UpdateAutoScheduleFrequency(_definition, []);
            _clock.Now = next.ScheduledAt;
            await _service.ActivateMissionRun(next.Id);
            Assert.Equal(MissionStatus.Cancelled, (await Persisted(next.Id)).Status);
            _scheduling.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ReadAndSkipUseExactlyNext24ElapsedHours()
        {
            await _service.UpdateAutoScheduleFrequency(
                _definition,
                [
                    new TimeAndDay(DayOfWeek.Friday, new TimeOnly(22, 0)),
                    new TimeAndDay(DayOfWeek.Saturday, new TimeOnly(22, 0)),
                    new TimeAndDay(DayOfWeek.Saturday, new TimeOnly(22, 1)),
                ]
            );
            var last = Assert.Single(await _service.ReadUpcoming(_definition.InstallationCode));
            Assert.Equal(_clock.Now.AddHours(24), last.ScheduledAt);
            Assert.True(await _service.SkipMissionRun(_definition, last.Id));
            var after = await _context.MissionRuns.SingleAsync(
                r => r.ScheduledAt == _clock.Now.AddHours(24).AddMinutes(1),
                Token
            );
            Assert.False(await _service.SkipMissionRun(_definition, after.Id));
            _clock.Now = after.ScheduledAt!.Value;
            Assert.False(await _service.SkipMissionRun(_definition, after.Id));
        }

        [Fact]
        public async Task UnavailableDefinitionReconciliationLogsAndLeavesNoTransaction()
        {
            await _definitions.Delete(_definition.Id);
            _logger.Invocations.Clear();
            _jobs.Invocations.Clear();
            await _service.Reconcile(_definition);
            Assert.Equal(LogLevel.Information, Assert.Single(_logger.Invocations).Arguments[0]);
            Assert.Null(_context.Database.CurrentTransaction);
            _jobs.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ReadUpcomingRespectsInstallationReadAccess()
        {
            var roles = new Mock<IAccessRoleService>();
            roles.Setup(r => r.GetAllowedInstallationCodes(AccessMode.Read)).ReturnsAsync([]);
            var services = _scope.ServiceProvider;
            var signalR = services.GetRequiredService<ISignalRService>();
            var definitions = new MissionDefinitionService(
                _context,
                signalR,
                roles.Object,
                NullLogger<IMissionDefinitionService>.Instance,
                _runs
            );
            var service = new AutoScheduleService(
                _logger.Object,
                definitions,
                services.GetRequiredService<IRobotService>(),
                _runs,
                _scheduling.Object,
                signalR,
                roles.Object,
                _jobs.Object,
                _context,
                _clock
            );
            Assert.Empty(await service.ReadUpcoming(_definition.InstallationCode));
            roles.Verify(r => r.GetAllowedInstallationCodes(AccessMode.Write), Times.Never);
        }

        [Fact]
        public async Task FutureRunsExistButCannotBlockQueueOrAppearInHistory()
        {
            var upcoming = await _service.ReadUpcoming(_definition.InstallationCode);
            Assert.Equal(2, upcoming.Count);
            Assert.Equal(DateTimeOffset.Parse("2026-10-16T23:00:00Z"), upcoming[1].ScheduledAt);
            Assert.Equal(4, await _context.MissionRuns.CountAsync(Token));
            Assert.All(
                await _context.MissionRuns.ToListAsync(Token),
                r =>
                {
                    Assert.Equal(MissionStatus.Scheduled, r.Status);
                    Assert.Null(r.Robot);
                    Assert.Empty(r.Tasks);
                    Assert.Null(r.ActivatedAt);
                }
            );
            Assert.Empty(await _runs.ReadAll(new MissionRunQueryStringParameters()));
            Assert.Null(await _runs.ReadById(upcoming[0].Id));
            Assert.Null(await _runs.ReadNextScheduledMissionRun(_robot.Id));
            var manual = await _runs.Create(
                new MissionRun
                {
                    Name = "Immediate",
                    MissionId = "manual",
                    InstallationCode = _definition.InstallationCode,
                    InspectionArea = _definition.InspectionArea,
                    Robot = _robot,
                    CreationTime = _clock.Now.UtcDateTime,
                    Tasks = [],
                }
            );
            Assert.Equal(manual.Id, (await _runs.ReadNextScheduledMissionRun(_robot.Id))!.Id);
            Assert.Single(await _runs.ReadMissionRunQueue(_robot.Id));
            Assert.Single(await _runs.ReadAll(new MissionRunQueryStringParameters()));
            await _service.ActivateMissionRun(upcoming[0].Id);
            Assert.Null((await Persisted(upcoming[0].Id)).ActivatedAt);
            _scheduling.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ReconciliationIsIdempotentAndRestoresJobsWithoutRestoringSkips()
        {
            var next = await Next();
            await _service.Reconcile(_definition);
            Assert.Equal(4, _jobNumber);
            Assert.True(await _service.SkipMissionRun(_definition, next.Id));
            Assert.False(await _service.SkipMissionRun(_definition, next.Id));
            await _context.MissionRuns.ExecuteUpdateAsync(
                s => s.SetProperty(r => r.SchedulerInstance, "old-process"),
                Token
            );
            using var restartedScope = _factory.Services.CreateScope();
            await Worker(restartedScope.ServiceProvider, _clock).Reconcile(_definition);
            Assert.Equal(7, _jobNumber);
            Assert.Equal(4, await _context.MissionRuns.CountAsync(Token));
            Assert.Equal(MissionStatus.Skipped, (await Persisted(next.Id)).Status);
            _clock.Now = next.ScheduledAt;
            await _service.ActivateMissionRun(next.Id);
            Assert.Empty(await _runs.ReadAll(new MissionRunQueryStringParameters()));
            _scheduling.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ActivationUsesSameRowAndCurrentRobotAndTaskSnapshotExactlyOnce()
        {
            var next = await Next();
            _definition.Name = "Edited after generation";
            _definition.Tasks[0].TagId = "new-tag";
            await _definitions.Update(_definition);
            await _context
                .Robots.Where(r => r.Id == _robot.Id)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(r => r.CurrentInspectionAreaId, (string?)null),
                    Token
                );
            var replacement = await _scope
                .ServiceProvider.GetRequiredService<DatabaseUtilities>()
                .NewRobot(
                    RobotStatus.Available,
                    _definition.InspectionArea.Installation,
                    _definition.InspectionArea.Id
                );
            _clock.Now = next.ScheduledAt;
            await _service.ActivateMissionRun(next.Id);
            await _service.ActivateMissionRun(next.Id);
            var activated = await _runs.ReadById(next.Id);
            Assert.NotNull(activated);
            Assert.Equal(replacement.Id, activated.Robot!.Id);
            Assert.Equal(_definition.Name, activated.Name);
            Assert.Equal("new-tag", Assert.Single(activated.Tasks).TagId);
            Assert.Equal(_clock.Now, activated.ActivatedAt);
            Assert.Equal(next.ScheduledAt, activated.ScheduledAt);
            Assert.Equal(MissionStatus.Queued, activated.Status);
            Assert.Equal(4, await _context.MissionRuns.CountAsync(Token));
            Assert.False(await _service.SkipMissionRun(_definition, next.Id));
            Assert.Single(await _runs.ReadMissionRunQueue(replacement.Id));
            _scheduling.Verify(
                s => s.StartNextMissionRunIfSystemIsAvailable(It.IsAny<Robot>()),
                Times.Once
            );
        }

        [Fact]
        public async Task TransientActivationFailureRollsBackAndRetriesWithoutDuplicateTasks()
        {
            var next = await Next();
            _clock.Now = next.ScheduledAt;
            _retry.FailOnce = true;
            await _service.ActivateMissionRun(next.Id);
            Assert.False(_retry.FailOnce);
            Assert.Equal(4, await _context.MissionRuns.CountAsync(Token));
            Assert.Single((await Persisted(next.Id)).Tasks);
            Assert.Equal(MissionStatus.Queued, (await Persisted(next.Id)).Status);
            _scheduling.Verify(
                s => s.StartNextMissionRunIfSystemIsAvailable(It.IsAny<Robot>()),
                Times.Once
            );
        }

        [Fact]
        public async Task ActivationWinningRaceRejectsConcurrentSkip()
        {
            var next = await Next();
            using var workerScope = _factory.Services.CreateScope();
            var entered = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            var release = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            var runs = new Mock<IMissionRunService>(MockBehavior.Strict);
            runs.Setup(r => r.ReadMissionRunQueue(It.IsAny<string>(), true))
                .Returns(async () =>
                {
                    entered.SetResult();
                    await release.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
                    return new List<MissionRun>();
                });
            runs.Setup(r => r.IncludesUnsupportedInspectionType(It.IsAny<MissionRun>()))
                .Returns(false);
            var worker = Worker(
                workerScope.ServiceProvider,
                new Clock { Now = next.ScheduledAt },
                runs.Object
            );
            var activate = worker.ActivateMissionRun(next.Id);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
            var skip = _service.SkipMissionRun(_definition, next.Id);
            release.SetResult();
            await activate;
            Assert.False(await skip);
            Assert.Equal(MissionStatus.Queued, (await Persisted(next.Id)).Status);
        }

        [Fact]
        public async Task ConcurrentActivationProducesOneQueueEntryAndOneWakeUp()
        {
            var next = await Next();
            _clock.Now = next.ScheduledAt;
            using var other = _factory.Services.CreateScope();
            await Task.WhenAll(
                _service.ActivateMissionRun(next.Id),
                Worker(other.ServiceProvider, _clock).ActivateMissionRun(next.Id)
            );
            Assert.Single(await _runs.ReadMissionRunQueue(_robot.Id));
            Assert.Single((await Persisted(next.Id)).Tasks);
            _scheduling.Verify(
                s => s.StartNextMissionRunIfSystemIsAvailable(It.IsAny<Robot>()),
                Times.Once
            );
        }

        [Fact]
        public async Task ConcurrentReconciliationCreatesNoDuplicateRowsOrJobs()
        {
            using var other = _factory.Services.CreateScope();
            await Task.WhenAll(
                _service.Reconcile(_definition),
                Worker(other.ServiceProvider, _clock).Reconcile(_definition)
            );
            Assert.Equal(4, await _context.MissionRuns.CountAsync(Token));
            Assert.Equal(4, _jobNumber);
        }

        [Fact]
        public async Task ScheduleRemovalAndReadditionCannotUndoExplicitSkip()
        {
            var upcoming = await _service.ReadUpcoming(_definition.InstallationCode);
            await _service.SkipMissionRun(_definition, upcoming[1].Id);
            var original = _definition.AutoScheduleFrequency!.SchedulingTimesCETperWeek.ToList();
            await _service.UpdateAutoScheduleFrequency(_definition, []);
            Assert.Equal(MissionStatus.Cancelled, (await Persisted(upcoming[0].Id)).Status);
            await _service.UpdateAutoScheduleFrequency(_definition, original);
            Assert.Equal(MissionStatus.Scheduled, (await Persisted(upcoming[0].Id)).Status);
            Assert.Null((await Persisted(upcoming[0].Id)).EndTime);
            Assert.Equal(MissionStatus.Skipped, (await Persisted(upcoming[1].Id)).Status);
            Assert.Equal(4, await _context.MissionRuns.CountAsync(Token));
        }

        [Fact]
        public async Task DeletedDefinitionCancelsUnactivatedRunsAndCannotActivate()
        {
            var next = await Next();
            await _definitions.Delete(_definition.Id);
            _clock.Now = next.ScheduledAt;
            await _service.ActivateMissionRun(next.Id);
            Assert.All(
                await _context.MissionRuns.ToListAsync(Token),
                r => Assert.Equal(MissionStatus.Cancelled, r.Status)
            );
            Assert.Empty(await _service.ReadUpcoming(_definition.InstallationCode));
            _scheduling.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task RestartMissesOverdueRunsButSameProcessLateJobsRemainEligible()
        {
            var upcoming = await _service.ReadUpcoming(_definition.InstallationCode);
            await _context
                .MissionRuns.Where(r => r.Id == upcoming[0].Id)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(r => r.SchedulerInstance, "old-process"),
                    Token
                );
            _clock.Now = upcoming[1].ScheduledAt.AddSeconds(20);
            await _service.Reconcile(_definition);
            Assert.Equal(MissionStatus.Missed, (await Persisted(upcoming[0].Id)).Status);
            await _service.ActivateMissionRun(upcoming[0].Id);
            await _service.ActivateMissionRun(upcoming[1].Id);
            Assert.Equal(
                upcoming[1].Id,
                Assert.Single(await _runs.ReadMissionRunQueue(_robot.Id)).Id
            );
        }

        [Fact]
        public async Task FailedJobDeletionCannotActivateSkippedRun()
        {
            var next = await Next();
            _jobs
                .Setup(j => j.ChangeState(It.IsAny<string>(), It.IsAny<DeletedState>(), null))
                .Returns(false);
            Assert.True(await _service.SkipMissionRun(_definition, next.Id));
            _clock.Now = next.ScheduledAt;
            await _service.ActivateMissionRun(next.Id);
            Assert.Equal(MissionStatus.Skipped, (await Persisted(next.Id)).Status);
            _scheduling.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task NoRobotFailureIsDurableAndSafeForDtoAndHistory()
        {
            var next = await Next();
            await _context
                .Robots.Where(r => r.Id == _robot.Id)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(r => r.CurrentInspectionAreaId, (string?)null),
                    Token
                );
            _clock.Now = next.ScheduledAt;
            await _service.ActivateMissionRun(next.Id);
            var run = await Persisted(next.Id);
            Assert.Equal(MissionStatus.Failed, run.Status);
            Assert.Null(run.Robot);
            Assert.Null(run.ActivatedAt);
            Assert.NotNull(run.EndTime);
            Assert.True(new MissionRunResponse(run).IsCompleted);
            Assert.Empty(await _runs.ReadAll(new MissionRunQueryStringParameters()));
            await _service.Reconcile(_definition);
            Assert.Equal(MissionStatus.Failed, (await Persisted(next.Id)).Status);
        }

        [Fact]
        public async Task ExistingQueuedMissionSuppressesDueRunWithoutCreatingAnotherQueueEntry()
        {
            var next = await Next();
            var manual = await _runs.Create(
                new MissionRun
                {
                    Name = "Manual",
                    MissionId = _definition.Id,
                    Robot = _robot,
                    InstallationCode = _definition.InstallationCode,
                    InspectionArea = _definition.InspectionArea,
                    CreationTime = _clock.Now.UtcDateTime,
                    Tasks = [],
                }
            );
            _clock.Now = next.ScheduledAt;
            await _service.ActivateMissionRun(next.Id);
            Assert.Equal(MissionStatus.Cancelled, (await Persisted(next.Id)).Status);
            Assert.Equal(manual.Id, Assert.Single(await _runs.ReadMissionRunQueue(_robot.Id)).Id);
            _scheduling.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task InvalidSkipTargetsHaveNoSideEffects()
        {
            var outside = await _context.MissionRuns.OrderBy(r => r.ScheduledAt).LastAsync(Token);
            Assert.False(await _service.SkipMissionRun(_definition, outside.Id));
            var next = await Next();
            Assert.False(
                await _service.SkipMissionRun(
                    new MissionDefinition
                    {
                        Id = "other",
                        InstallationCode = _definition.InstallationCode,
                    },
                    next.Id
                )
            );
            Assert.False(await _service.SkipMissionRun(_definition, "missing"));
            Assert.All(
                await _context.MissionRuns.ToListAsync(Token),
                r => Assert.Equal(MissionStatus.Scheduled, r.Status)
            );
        }

        [Theory]
        [InlineData("missing\r\nForged log")]
        [InlineData("missing\u001b[2J")]
        [InlineData("missing\tForged field")]
        public async Task RejectedSkipNeverLogsRequestControlledId(string id)
        {
            _logger.Invocations.Clear();
            Assert.False(await _service.SkipMissionRun(_definition, id));
            var warning = Assert.Single(_logger.Invocations);
            Assert.Equal(LogLevel.Warning, warning.Arguments[0]);
            Assert.Equal(
                "Cannot skip scheduled mission run: it is no longer eligible",
                warning.Arguments[2].ToString()
            );
            var state = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(
                warning.Arguments[2]
            );
            Assert.Equal("{OriginalFormat}", Assert.Single(state).Key);
        }

        [Fact]
        public async Task DatabaseRejectsDuplicateScheduledIdentityAndAllowsMultipleManualRuns()
        {
            var next = await Next();
            var duplicate = new MissionRun
            {
                MissionId = _definition.Id,
                ScheduledAt = next.ScheduledAt,
                Status = MissionStatus.Scheduled,
                Name = "Duplicate",
                InstallationCode = _definition.InstallationCode,
                InspectionArea = _definition.InspectionArea,
                CreationTime = _clock.Now.UtcDateTime,
                Tasks = [],
            };
            _context.Entry(duplicate.InspectionArea).State = EntityState.Unchanged;
            _context.MissionRuns.Add(duplicate);
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
                _context.SaveChangesAsync(Token)
            );
            Assert.Equal(
                PostgresErrorCodes.UniqueViolation,
                Assert.IsType<PostgresException>(exception.InnerException).SqlState
            );
            _context.ChangeTracker.Clear();
            for (var i = 0; i < 2; i++)
                await _runs.Create(
                    new MissionRun
                    {
                        MissionId = _definition.Id,
                        Name = "Manual",
                        Robot = _robot,
                        InstallationCode = _definition.InstallationCode,
                        InspectionArea = _definition.InspectionArea,
                        CreationTime = _clock.Now.UtcDateTime,
                        Tasks = [],
                    }
                );
            Assert.Equal(2, (await _runs.ReadMissionRunQueue(_robot.Id)).Count);
        }
    }
}
