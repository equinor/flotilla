using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Database.Models;
using Api.Services;
using Api.Utilities;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Api.Test.Security
{
    public class AutoScheduleAuthorizationTests
    {
        private readonly Mock<IMissionDefinitionService> _definitions = new(MockBehavior.Strict);
        private readonly Mock<IRobotService> _robots = new(MockBehavior.Strict);
        private readonly Mock<IMissionRunService> _missions = new(MockBehavior.Strict);
        private readonly Mock<IMissionSchedulingService> _scheduling = new(MockBehavior.Strict);
        private readonly Mock<IAccessRoleService> _roles = new(MockBehavior.Strict);
        private readonly Mock<IBackgroundJobClient> _jobs = new(MockBehavior.Strict);
        private static readonly TimeOnly ScheduledTime = new(12, 0);

        private AutoScheduleService CreateService() =>
            new(
                NullLogger<AutoScheduleService>.Instance,
                _definitions.Object,
                _robots.Object,
                _missions.Object,
                _scheduling.Object,
                Mock.Of<ISignalRService>(),
                _roles.Object,
                _jobs.Object
            );

        private static MissionDefinition CreateDefinition() =>
            new()
            {
                Id = "mission",
                Name = "Mission",
                InstallationCode = "BBB",
                InspectionArea = new InspectionArea { Id = "area" },
                Tasks =
                [
                    new TaskDefinition
                    {
                        Index = 1,
                        RobotPose = new Pose(),
                        TargetPosition = new Position(),
                        SensorType = SensorType.Image,
                    },
                ],
                AutoScheduleFrequency = new AutoScheduleFrequency
                {
                    SchedulingTimesCETperWeek =
                    [
                        new TimeAndDay(TimeZoneUtilities.NowCet().DayOfWeek, TimeOnly.MaxValue),
                    ],
                    AutoScheduledJobs = JsonSerializer.Serialize(
                        new Dictionary<TimeOnly, string> { [ScheduledTime] = "job" }
                    ),
                },
            };

        [Theory]
        [InlineData("start")]
        [InlineData("update")]
        [InlineData("new")]
        [InlineData("remove")]
        [InlineData("skip")]
        [InlineData("skip-all")]
        public async Task UnauthorizedSchedulingHasNoSideEffects(string operation)
        {
            _roles
                .Setup(s => s.GetAllowedInstallationCodes(AccessMode.Write))
                .ReturnsAsync(["AAA"]);
            var definition = CreateDefinition();
            if (operation == "new")
                definition.AutoScheduleFrequency = null;
            var originalJobs = definition.AutoScheduleFrequency?.AutoScheduledJobs;
            var service = CreateService();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                operation switch
                {
                    "start" => service.StartJobsForMissionDefinition(definition),
                    "update" => service.UpdateAutoScheduleFrequency(definition, null),
                    "new" => service.UpdateAutoScheduleFrequency(
                        definition,
                        [new TimeAndDay(DayOfWeek.Monday, ScheduledTime)]
                    ),
                    "remove" => service.RemoveFromAutoMissionScheduledJobs(
                        definition,
                        ScheduledTime
                    ),
                    "skip" => service.SkipAutoMissionScheduledJob(definition, ScheduledTime),
                    _ => service.SkipAllAutoMissions(definition),
                }
            );

            Assert.Equal(originalJobs, definition.AutoScheduleFrequency?.AutoScheduledJobs);
            if (operation == "new")
                Assert.Null(definition.AutoScheduleFrequency);
            _jobs.VerifyNoOtherCalls();
            _definitions.VerifyNoOtherCalls();
            _robots.VerifyNoOtherCalls();
            _missions.VerifyNoOtherCalls();
            _scheduling.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task FailedSchedulingIntentIsNotEnqueued()
        {
            _roles
                .Setup(s => s.GetAllowedInstallationCodes(AccessMode.Write))
                .ReturnsAsync(["BBB"]);
            var definition = CreateDefinition();
            definition.AutoScheduleFrequency = null;
            _definitions
                .Setup(s => s.Update(definition))
                .ThrowsAsync(new InvalidOperationException("Persistence failed"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService()
                    .UpdateAutoScheduleFrequency(
                        definition,
                        [new TimeAndDay(TimeZoneUtilities.NowCet().DayOfWeek, TimeOnly.MaxValue)]
                    )
            );

            _jobs.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task FailedJobRecordIsRemovedFromQueue()
        {
            _roles
                .Setup(s => s.GetAllowedInstallationCodes(AccessMode.Write))
                .ReturnsAsync(["BBB"]);
            var definition = CreateDefinition();
            definition.AutoScheduleFrequency!.AutoScheduledJobs = null;
            _jobs.Setup(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("new-job");
            _jobs
                .Setup(j => j.ChangeState("new-job", It.IsAny<DeletedState>(), null))
                .Returns(true);
            _definitions
                .Setup(s => s.Update(definition))
                .ThrowsAsync(new InvalidOperationException("Persistence failed"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().StartJobsForMissionDefinition(definition)
            );

            _jobs.Verify(j => j.ChangeState("new-job", It.IsAny<DeletedState>(), null), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("{}")]
        [InlineData("invalid-json")]
        public async Task WorkerWithoutPersistedJobCannotCreateMission(string? jobs)
        {
            _roles
                .Setup(s => s.GetAllowedInstallationCodes(AccessMode.Write))
                .ReturnsAsync(["BBB"]);
            var definition = CreateDefinition();
            definition.AutoScheduleFrequency = jobs is null
                ? null
                : new AutoScheduleFrequency { AutoScheduledJobs = jobs };
            _definitions.Setup(s => s.ReadById(definition.Id, true)).ReturnsAsync(definition);

            await CreateService().AutoScheduleMissionRun(definition.Id, ScheduledTime);

            _jobs.VerifyNoOtherCalls();
            _robots.VerifyNoOtherCalls();
            _missions.VerifyNoOtherCalls();
            _scheduling.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WorkerStopsWhenConsumingScheduleFails(bool jobDeleted)
        {
            _roles
                .Setup(s => s.GetAllowedInstallationCodes(AccessMode.Write))
                .ReturnsAsync(["BBB"]);
            var definition = CreateDefinition();
            _definitions.Setup(s => s.ReadById(definition.Id, true)).ReturnsAsync(definition);
            if (jobDeleted)
                _definitions
                    .Setup(s => s.Update(definition))
                    .ThrowsAsync(new InvalidOperationException("Persistence failed"));
            _jobs
                .Setup(j => j.ChangeState("job", It.IsAny<DeletedState>(), null))
                .Returns(jobDeleted);

            await CreateService().AutoScheduleMissionRun(definition.Id, ScheduledTime);

            _definitions.Verify(
                s => s.Update(It.IsAny<MissionDefinition>()),
                jobDeleted ? Times.Once() : Times.Never()
            );
            _robots.VerifyNoOtherCalls();
            _missions.VerifyNoOtherCalls();
            _scheduling.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task WorkerWithPersistedScheduleStillCreatesMission()
        {
            _roles
                .Setup(s => s.GetAllowedInstallationCodes(AccessMode.Write))
                .ReturnsAsync(["BBB"]);
            var definition = CreateDefinition();
            var robot = new Robot { Id = "robot", CurrentInspectionAreaId = "area" };
            _definitions.Setup(s => s.ReadById(definition.Id, true)).ReturnsAsync(definition);
            _definitions.Setup(s => s.Update(definition)).ReturnsAsync(definition);
            _jobs.Setup(j => j.ChangeState("job", It.IsAny<DeletedState>(), null)).Returns(true);
            _robots.Setup(s => s.ReadRobotsForInstallation("BBB", true)).ReturnsAsync([robot]);
            _missions.Setup(s => s.ReadMissionRunQueue(robot.Id, true)).ReturnsAsync([]);
            _missions
                .Setup(s => s.Create(It.IsAny<MissionRun>()))
                .ReturnsAsync((MissionRun mission) => mission);
            _scheduling
                .Setup(s => s.StartNextMissionRunIfSystemIsAvailable(robot))
                .Returns(Task.CompletedTask);

            await CreateService().AutoScheduleMissionRun(definition.Id, ScheduledTime);

            _missions.Verify(
                s => s.Create(It.Is<MissionRun>(m => m.MissionId == definition.Id)),
                Times.Once
            );
            _scheduling.Verify(s => s.StartNextMissionRunIfSystemIsAvailable(robot), Times.Once);
        }
    }
}
