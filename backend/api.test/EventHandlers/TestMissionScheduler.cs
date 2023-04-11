using System;
using System.Threading;
using System.Threading.Tasks;
using Api.Controllers;
using Api.Database.Context;
using Api.Database.Models;
using Api.EventHandlers;
using Api.Services;
using Api.Test.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Api.Test.EventHandlers
{
    [Collection("Database collection")]
    public class TestMissionScheduler : IDisposable
    {
        private static Robot Robot =>
            new()
            {
                Id = "IamTestRobot",
                Status = RobotStatus.Available,
                Host = "localhost",
                Model = RobotModel.Turtlebot,
                Name = "TestosteroneTesty",
                SerialNumber = "12354",
                Enabled = true
            };
        private static Mission ScheduledMission =>
            new()
            {
                Id = "testMission",
                Name = "testMission",
                EchoMissionId = 2,
                Robot = Robot,
                Status = MissionStatus.Pending,
                DesiredStartTime = DateTimeOffset.Now,
                AssetCode = "TestAsset",
                Map = new MissionMap()
                {
                    MapName = "TestMap",
                    Boundary = new(),
                    TransformationMatrices = new()
                }
            };

        private readonly MissionScheduler _scheduledMissionEventHandler;
        private readonly IMissionService _missionService;
        private readonly RobotControllerMock _robotControllerMock;
        private readonly FlotillaDbContext _context;

        public TestMissionScheduler(DatabaseFixture fixture)
        {
            // Using Moq https://github.com/moq/moq4

            var schedulerLogger = new Mock<ILogger<MissionScheduler>>().Object;
            var missionLogger = new Mock<ILogger<MissionService>>().Object;

            // Mock ScheduledMissionService:
            _context = fixture.NewContext;
            _missionService = new MissionService(_context, missionLogger);
            _robotControllerMock = new RobotControllerMock();

            var mockServiceProvider = new Mock<IServiceProvider>();

            // Mock injection of ScheduledMissionService:
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IMissionService)))
                .Returns(_missionService);
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
        private async void AssertExpectedStatusChange(
            MissionStatus preStatus,
            MissionStatus postStatus
        )
        {
            // ARRANGE

            var cts = new CancellationTokenSource();

            // Add Scheduled mission
            await _missionService.Create(ScheduledMission);

            // Assert start conditions
            var preMission = await _missionService.ReadById(ScheduledMission.Id);
            Assert.NotNull(preMission);
            Assert.Equal(preStatus, preMission!.Status);

            // ACT

            // Start / Stop eventhandler
            await _scheduledMissionEventHandler.StartAsync(cts.Token);
            await Task.Delay(3000);
            await _scheduledMissionEventHandler.StopAsync(cts.Token);

            // ASSERT

            // Verify status change
            var postMission = await _missionService.ReadById(ScheduledMission.Id);
            Assert.NotNull(postMission);
            Assert.Equal(postStatus, postMission!.Status);
        }

        [Fact]
        public void ScheduledMissionSetTFailed()
        {
            // Mock bad path of 'RobotController.StartMission'
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            _robotControllerMock.RobotServiceMock
                .Setup(r => r.ReadById(Robot.Id))
                .Returns(async () => null);
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

            // Expect failed because robot is busy
            AssertExpectedStatusChange(MissionStatus.Pending, MissionStatus.Failed);
        }
    }
}
