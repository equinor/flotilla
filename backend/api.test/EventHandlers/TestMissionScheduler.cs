using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Api.Controllers;
using Api.Database.Context;
using Api.Database.Models;
using Api.EventHandlers;
using Api.Services;
using Api.Services.Models;
using Api.Test.Mocks;
using Api.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

#pragma warning disable CS1998
namespace Api.Test.EventHandlers
{
    [Collection("Database collection")]
    public class TestMissionScheduler : IDisposable
    {
        private static MissionRun ScheduledMission =>
            new()
            {
                Name = "testMission",
                MissionId = Guid.NewGuid().ToString(),
                Status = MissionStatus.Pending,
                DesiredStartTime = DateTimeOffset.Now,
                MapMetadata = new MapMetadata()
                {
                    MapName = "TestMap",
                    Boundary = new(),
                    TransformationMatrices = new()
                },
                Area = new Area()
            };

        private readonly MissionScheduler _scheduledMissionEventHandler;
        private readonly IMissionRunService _missionService;
        private readonly IRobotService _robotService;
        private readonly RobotControllerMock _robotControllerMock;
        private readonly FlotillaDbContext _context;

        public TestMissionScheduler(DatabaseFixture fixture)
        {
            // Using Moq https://github.com/moq/moq4

            var schedulerLogger = new Mock<ILogger<MissionScheduler>>().Object;
            var missionLogger = new Mock<ILogger<MissionRunService>>().Object;

            // Mock ScheduledMissionService:
            _context = fixture.NewContext;
            _missionService = new MissionRunService(_context, missionLogger);
            _robotService = new RobotService(_context);
            _robotControllerMock = new RobotControllerMock();

            var mockServiceProvider = new Mock<IServiceProvider>();

            // Mock injection of MissionService:
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IMissionRunService)))
                .Returns(_missionService);
            // Mock injection of RobotService:
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IRobotService)))
                .Returns(_robotService);
            // Mock injection of Robot Controller
            mockServiceProvider
                .Setup(p => p.GetService(typeof(RobotController)))
                .Returns(_robotControllerMock.Mock.Object);
            // Mock injection of Database context
            mockServiceProvider
                .Setup(p => p.GetService(typeof(FlotillaDbContext)))
                .Returns(_context);

            // Mock service injector
            var mockScope = new Mock<IServiceScope>();
            mockScope.Setup(scope => scope.ServiceProvider).Returns(mockServiceProvider.Object);
            var mockFactory = new Mock<IServiceScopeFactory>();
            mockFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

            _scheduledMissionEventHandler = new MissionScheduler(
                schedulerLogger,
                mockFactory.Object
            );
        }

        public void Dispose()
        {
            _context.Dispose();
            _scheduledMissionEventHandler.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// This function schedules a mission to be started immediately, waits for it to be executed and verifies
        /// that the status went from <paramref name="preStatus"/> to <paramref name="postStatus"/> in the handling of the scheduled mission
        /// </summary>
        /// <param name="preStatus"></param>
        /// <param name="postStatus"></param>
        /// <param name="mission"></param>
        private async void AssertExpectedStatusChange(
            MissionStatus preStatus,
            MissionStatus postStatus,
            MissionRun mission
        )
        {
            // ARRANGE

            var cts = new CancellationTokenSource();

            // Add Scheduled mission
            await _missionService.Create(mission);

            _robotControllerMock.RobotServiceMock
                .Setup(service => service.ReadById(mission.Robot.Id))
                .Returns(async () => mission.Robot);

            _robotControllerMock.MissionServiceMock
                .Setup(service => service.ReadById(mission.Id))
                .Returns(async () => mission);

            // Assert start conditions
            var preMission = await _missionService.ReadById(mission.Id);
            Assert.NotNull(preMission);
            Assert.Equal(preStatus, preMission!.Status);

            // ACT

            // Start / Stop eventhandler
            await _scheduledMissionEventHandler.StartAsync(cts.Token);
            await Task.Delay(3000);
            await _scheduledMissionEventHandler.StopAsync(cts.Token);

            // ASSERT

            // Verify status change
            var postMission = await _missionService.ReadById(mission.Id);
            Assert.NotNull(postMission);
            Assert.Equal(postStatus, postMission!.Status);
        }

        [Fact]
        // Test that if robot is busy, mission awaits available robot
        public async void ScheduledMissionPendingIfRobotBusy()
        {
            var mission = ScheduledMission;

            // Get real robot to avoid error on robot model
            var robot = (await _robotService.ReadAll()).First(
                r => r is { Status: RobotStatus.Busy, Enabled: true }
            );
            mission.Robot = robot;

            // Expect failed because robot does not exist
            AssertExpectedStatusChange(MissionStatus.Pending, MissionStatus.Pending, mission);
        }

        [Fact]
        // Test that if robot is available, mission is started
        public async void ScheduledMissionStartedIfRobotAvailable()
        {
            var mission = ScheduledMission;

            // Get real robot to avoid error on robot model
            var robot = (await _robotService.ReadAll()).First(
                r => r is { Status: RobotStatus.Available, Enabled: true }
            );
            mission.Robot = robot;

            // Mock successful Start Mission:
            _robotControllerMock.IsarServiceMock
                .Setup(isar => isar.StartMission(robot, mission))
                .Returns(
                    async () =>
                        new IsarMission(
                            new IsarStartMissionResponse
                            {
                                MissionId = "test",
                                Tasks = new List<IsarTaskResponse>()
                            }
                        )
                );

            // Expect failed because robot does not exist
            AssertExpectedStatusChange(MissionStatus.Pending, MissionStatus.Ongoing, mission);
        }

        [Fact]
        // Test that if ISAR fails, mission is set to failed
        public async void ScheduledMissionFailedIfIsarUnavailable()
        {
            var mission = ScheduledMission;

            // Get real robot to avoid error on robot model
            var robot = (await _robotService.ReadAll()).First();
            robot.Enabled = true;
            robot.Status = RobotStatus.Available;
            await _robotService.Update(robot);
            mission.Robot = robot;

            // Mock failing ISAR:
            _robotControllerMock.IsarServiceMock
                .Setup(isar => isar.StartMission(robot, mission))
                .Throws(new MissionException("ISAR Failed test message"));

            // Expect failed because robot does not exist
            AssertExpectedStatusChange(MissionStatus.Pending, MissionStatus.Failed, mission);
        }
    }
}
