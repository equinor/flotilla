using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.HostedServices
{
    public class TestAutoSchedulingHostedService : IAsyncLifetime
    {
        private PostgreSqlContainer _container = null!;
        private TestWebApplicationFactory<Program> _factory = null!;
        private IServiceScope _scope = null!;
        private FlotillaDbContext _context = null!;
        private IMissionDefinitionService _definitions = null!;
        private MissionDefinition _definition = null!;
        private AutoScheduleService _service = null!;
        private readonly Clock _clock = new();
        private readonly Mock<IMissionSchedulingService> _scheduling = new();
        private readonly Mock<IBackgroundJobClient> _jobs = new(MockBehavior.Strict);
        private readonly Mock<ILogger<AutoScheduleService>> _logger = new();
        private int _jobNumber;

        private sealed class SchedulingFactory : TestWebApplicationFactory<Program>
        {
            private readonly string _connectionString;

            public SchedulingFactory(string connectionString)
                : base(connectionString)
            {
                _connectionString = connectionString;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);
                builder.ConfigureTestServices(services =>
                    services.AddScoped<FlotillaDbContext>(_ => new FlotillaDbContext(
                        new DbContextOptionsBuilder<FlotillaDbContext>()
                            .UseNpgsql(_connectionString, o => o.EnableRetryOnFailure())
                            .Options
                    ))
                );
            }
        }

        private sealed class Clock : TimeProvider
        {
            public DateTimeOffset Now = DateTimeOffset.Parse("2026-10-16T20:00:00Z");

            public override DateTimeOffset GetUtcNow() => Now;
        }

        public async ValueTask InitializeAsync()
        {
            var setup = await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            _container = setup.Item1;
            await setup.Item3.DisposeAsync();
            _factory = new SchedulingFactory(setup.Item2);
            _scope = _factory.Services.CreateScope();
            var services = _scope.ServiceProvider;
            _context = services.GetRequiredService<FlotillaDbContext>();
            _definitions = services.GetRequiredService<IMissionDefinitionService>();
            var database = services.GetRequiredService<DatabaseUtilities>();
            var installation = await database.NewInstallation();
            var plant = await database.NewPlant(installation.InstallationCode);
            var area = await database.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            await database.NewRobot(RobotStatus.Available, installation, area.Id);
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
            _service = new AutoScheduleService(
                _logger.Object,
                _definitions,
                services.GetRequiredService<IRobotService>(),
                services.GetRequiredService<IMissionRunService>(),
                _scheduling.Object,
                services.GetRequiredService<ISignalRService>(),
                services.GetRequiredService<IAccessRoleService>(),
                _jobs.Object,
                _context,
                _clock
            );
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

        public async ValueTask DisposeAsync()
        {
            _scope?.Dispose();
            if (_factory != null)
                await _factory.DisposeAsync();
            if (_container != null)
                await _container.DisposeAsync();
        }

        private AutoScheduleService CreateWorker(
            IServiceProvider services,
            TimeProvider clock,
            IMissionRunService runs
        ) =>
            new(
                NullLogger<AutoScheduleService>.Instance,
                services.GetRequiredService<IMissionDefinitionService>(),
                services.GetRequiredService<IRobotService>(),
                runs,
                _scheduling.Object,
                services.GetRequiredService<ISignalRService>(),
                services.GetRequiredService<IAccessRoleService>(),
                _jobs.Object,
                services.GetRequiredService<FlotillaDbContext>(),
                clock
            );

        [Fact]
        public async Task FailedMissionCreationRollsBackTheClaim()
        {
            var occurrence = (await _service.ReadUpcoming(_definition.InstallationCode))[0];
            using var workerScope = _factory.Services.CreateScope();
            var runs = new Mock<IMissionRunService>(MockBehavior.Strict);
            var robot = await workerScope
                .ServiceProvider.GetRequiredService<IRobotService>()
                .ReadRobotsForInstallation(_definition.InstallationCode);
            runs.Setup(r => r.ReadMissionRunQueue(robot[0].Id, true)).ReturnsAsync([]);
            runs.Setup(r => r.Create(It.IsAny<MissionRun>()))
                .ThrowsAsync(new InvalidOperationException("Persistence failed"));
            var worker = CreateWorker(
                workerScope.ServiceProvider,
                new Clock { Now = occurrence.ScheduledAt },
                runs.Object
            );

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                worker.DispatchOccurrence(occurrence.Id)
            );
            Assert.Equal(
                AutoScheduleOccurrenceStatus.Scheduled,
                (
                    await _context.AutoScheduleOccurrences.SingleAsync(
                        o => o.Id == occurrence.Id,
                        TestContext.Current.CancellationToken
                    )
                ).Status
            );
            Assert.Empty(
                await _context.MissionRuns.ToListAsync(TestContext.Current.CancellationToken)
            );
            _clock.Now = occurrence.ScheduledAt;
            await _service.DispatchOccurrence(occurrence.Id);
            Assert.Single(
                await _context.MissionRuns.ToListAsync(TestContext.Current.CancellationToken)
            );
        }

        [Fact]
        public async Task DispatchWinningTheRaceRejectsAConcurrentSkip()
        {
            var occurrence = (await _service.ReadUpcoming(_definition.InstallationCode))[0];
            using var workerScope = _factory.Services.CreateScope();
            var services = workerScope.ServiceProvider;
            var realRuns = services.GetRequiredService<IMissionRunService>();
            var entered = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            var release = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            var runs = new Mock<IMissionRunService>(MockBehavior.Strict);
            runs.Setup(r => r.ReadMissionRunQueue(It.IsAny<string>(), true)).ReturnsAsync([]);
            runs.Setup(r => r.ReadById(occurrence.Id, true, false))
                .Returns(() => realRuns.ReadById(occurrence.Id));
            runs.Setup(r => r.Create(It.IsAny<MissionRun>()))
                .Returns(
                    async (MissionRun mission) =>
                    {
                        entered.SetResult();
                        await release.Task.WaitAsync(
                            TimeSpan.FromSeconds(10),
                            TestContext.Current.CancellationToken
                        );
                        return await realRuns.Create(mission);
                    }
                );
            var worker = CreateWorker(
                services,
                new Clock { Now = occurrence.ScheduledAt },
                runs.Object
            );
            var dispatch = worker.DispatchOccurrence(occurrence.Id);
            await entered.Task.WaitAsync(
                TimeSpan.FromSeconds(10),
                TestContext.Current.CancellationToken
            );
            var skip = _service.SkipOccurrence(_definition, occurrence.Id);
            release.SetResult();
            await dispatch;
            Assert.False(await skip);
            Assert.Single(
                await _context.MissionRuns.ToListAsync(TestContext.Current.CancellationToken)
            );
        }

        [Fact]
        public async Task RollingWindowAndReconciliationPreserveTomorrowSkipAcrossRestart()
        {
            var upcoming = await _service.ReadUpcoming(_definition.InstallationCode);
            Assert.Equal(2, upcoming.Count);
            Assert.Equal(DateTimeOffset.Parse("2026-10-16T23:00:00Z"), upcoming[1].ScheduledAt);
            Assert.Equal(
                4,
                await _context.AutoScheduleOccurrences.CountAsync(
                    TestContext.Current.CancellationToken
                )
            );
            var createdJobs = _jobNumber;
            await _service.Reconcile(_definition);
            Assert.Equal(createdJobs, _jobNumber);

            var tomorrow = upcoming[1];
            Assert.True(await _service.SkipOccurrence(_definition, tomorrow.Id));
            Assert.False(await _service.SkipOccurrence(_definition, tomorrow.Id));
            await _context.AutoScheduleOccurrences.ExecuteUpdateAsync(
                s => s.SetProperty(o => o.SchedulerInstance, "previous-process"),
                TestContext.Current.CancellationToken
            );
            await _service.Reconcile(_definition);
            Assert.Equal(
                AutoScheduleOccurrenceStatus.Skipped,
                (
                    await _context.AutoScheduleOccurrences.SingleAsync(
                        o => o.Id == tomorrow.Id,
                        TestContext.Current.CancellationToken
                    )
                ).Status
            );
            Assert.Equal(createdJobs + 3, _jobNumber);

            _clock.Now = tomorrow.ScheduledAt;
            await _service.DispatchOccurrence(tomorrow.Id);
            Assert.Empty(
                await _context.MissionRuns.ToListAsync(TestContext.Current.CancellationToken)
            );
        }

        [Fact]
        public async Task FailedJobDeletionCannotExecuteSkippedOccurrence()
        {
            var occurrence = (await _service.ReadUpcoming(_definition.InstallationCode))[1];
            _jobs
                .Setup(j => j.ChangeState(It.IsAny<string>(), It.IsAny<DeletedState>(), null))
                .Returns(false);
            Assert.True(await _service.SkipOccurrence(_definition, occurrence.Id));
            _clock.Now = occurrence.ScheduledAt;
            await _service.DispatchOccurrence(occurrence.Id);
            Assert.Empty(
                await _context.MissionRuns.ToListAsync(TestContext.Current.CancellationToken)
            );
        }

        [Fact]
        public async Task DispatchIsAtomicAndCannotBeRepeatedOrSkippedAfterwards()
        {
            var occurrence = (await _service.ReadUpcoming(_definition.InstallationCode))[0];
            await _service.DispatchOccurrence(occurrence.Id);
            Assert.Empty(
                await _context.MissionRuns.ToListAsync(TestContext.Current.CancellationToken)
            );
            _clock.Now = occurrence.ScheduledAt;
            await _service.DispatchOccurrence(occurrence.Id);
            await _service.DispatchOccurrence(occurrence.Id);
            Assert.Single(
                await _context.MissionRuns.ToListAsync(TestContext.Current.CancellationToken)
            );
            Assert.False(await _service.SkipOccurrence(_definition, occurrence.Id));
            Assert.Equal(
                AutoScheduleOccurrenceStatus.Dispatched,
                (
                    await _context.AutoScheduleOccurrences.SingleAsync(
                        o => o.Id == occurrence.Id,
                        TestContext.Current.CancellationToken
                    )
                ).Status
            );
        }

        [Fact]
        public async Task ScheduleEditsCancelRemovedOccurrencesButDoNotUndoSkips()
        {
            var upcoming = await _service.ReadUpcoming(_definition.InstallationCode);
            await _service.SkipOccurrence(_definition, upcoming[1].Id);
            var original = _definition.AutoScheduleFrequency!.SchedulingTimesCETperWeek.ToList();
            await _service.UpdateAutoScheduleFrequency(_definition, []);
            Assert.Equal(
                AutoScheduleOccurrenceStatus.Cancelled,
                (
                    await _context.AutoScheduleOccurrences.SingleAsync(
                        o => o.Id == upcoming[0].Id,
                        TestContext.Current.CancellationToken
                    )
                ).Status
            );
            await _service.UpdateAutoScheduleFrequency(_definition, original);
            Assert.Equal(
                AutoScheduleOccurrenceStatus.Scheduled,
                (
                    await _context.AutoScheduleOccurrences.SingleAsync(
                        o => o.Id == upcoming[0].Id,
                        TestContext.Current.CancellationToken
                    )
                ).Status
            );
            Assert.Equal(
                AutoScheduleOccurrenceStatus.Skipped,
                (
                    await _context.AutoScheduleOccurrences.SingleAsync(
                        o => o.Id == upcoming[1].Id,
                        TestContext.Current.CancellationToken
                    )
                ).Status
            );
            Assert.Equal(
                4,
                await _context.AutoScheduleOccurrences.CountAsync(
                    TestContext.Current.CancellationToken
                )
            );
        }

        [Fact]
        public async Task SkipRejectsWrongMissionAndOccurrencesOutsideWindow()
        {
            var outside = await _context
                .AutoScheduleOccurrences.OrderBy(o => o.ScheduledAt)
                .LastAsync(TestContext.Current.CancellationToken);
            Assert.False(await _service.SkipOccurrence(_definition, outside.Id));
            var upcoming = (await _service.ReadUpcoming(_definition.InstallationCode))[0];
            Assert.False(
                await _service.SkipOccurrence(
                    new MissionDefinition
                    {
                        Id = "different-definition",
                        InstallationCode = _definition.InstallationCode,
                    },
                    upcoming.Id
                )
            );
            Assert.False(await _service.SkipOccurrence(_definition, "missing-occurrence"));
        }

        [Theory]
        [InlineData("missing\r\nForged log entry")]
        [InlineData("missing\u001b[2J")]
        [InlineData("missing\tForged field")]
        public async Task RejectedSkipDoesNotLogUntrustedOccurrenceId(string occurrenceId)
        {
            _logger.Invocations.Clear();

            Assert.False(await _service.SkipOccurrence(_definition, occurrenceId));

            var warning = Assert.Single(_logger.Invocations);
            Assert.Equal(LogLevel.Warning, warning.Arguments[0]);
            const string message = "Cannot skip occurrence: it is no longer eligible";
            Assert.Equal(message, warning.Arguments[2].ToString());
            var state = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(
                warning.Arguments[2]
            );
            var field = Assert.Single(state);
            Assert.Equal("{OriginalFormat}", field.Key);
            Assert.Equal(message, field.Value);
            Assert.Equal(
                4,
                await _context.AutoScheduleOccurrences.CountAsync(
                    TestContext.Current.CancellationToken
                )
            );
            Assert.All(
                await _context.AutoScheduleOccurrences.ToListAsync(
                    TestContext.Current.CancellationToken
                ),
                o => Assert.Equal(AutoScheduleOccurrenceStatus.Scheduled, o.Status)
            );
            _jobs.Verify(
                j => j.ChangeState(It.IsAny<string>(), It.IsAny<DeletedState>(), null),
                Times.Never
            );
        }

        [Fact]
        public async Task MidnightDoesNotResetFutureOccurrencesAndLateJobsStillDispatch()
        {
            var tomorrow = (await _service.ReadUpcoming(_definition.InstallationCode))[1];
            _clock.Now = DateTimeOffset.Parse("2026-10-16T22:01:00Z");
            await _service.Reconcile(_definition);
            Assert.Contains(
                await _service.ReadUpcoming(_definition.InstallationCode),
                o => o.Id == tomorrow.Id
            );
            _clock.Now = tomorrow.ScheduledAt.AddSeconds(20);
            await _service.Reconcile(_definition);
            await _service.DispatchOccurrence(tomorrow.Id);
            Assert.Single(
                await _context.MissionRuns.ToListAsync(TestContext.Current.CancellationToken)
            );
        }
    }
}
