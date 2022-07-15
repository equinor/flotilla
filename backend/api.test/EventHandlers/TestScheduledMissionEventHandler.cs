using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Api.Controllers;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.EventHandlers;
using Api.Services;
using Api.Test.Mocks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Api.Test.EventHandlers
{
    [Collection("Database collection")]
    public class TestScheduledMissionEventHandler : IDisposable
    {
        private readonly ScheduledMissionEventHandler _scheduledMissionEventHandler;
        private readonly IScheduledMissionService _scheduledMissionService;
        private readonly RobotControllerMock _robotControllerMock;
        private readonly FlotillaDbContext _context;

        private static Robot Robot => new()
        {
            Id = "IamTestRobot",
            Status = RobotStatus.Available,
            Host = "localhost",
            Model = "TesTModel",
            Name = "TestosteroneTesty",
            SerialNumber = "12354"
        };
        private static ScheduledMission ScheduledMission => new()
        {
            Id = "testScheduledMission",
            EchoMissionId = 2,
            Robot = Robot,
            Status = ScheduledMissionStatus.Pending,
            StartTime = DateTimeOffset.Now
        };
        private static EchoMission EchoMission => new()
        {
            Tags = new List<EchoTag>() {
                            new EchoTag() { Id = 1,
                                Inspections = new List<EchoInspection>() {
                                new EchoInspection(IsarStep.InspectionTypeEnum.Image, null) }, TagId = "123", URL = new Uri("http://localhost:3000") } }
        };
        private static Report TestReport => new()
        {
            Id = "id",
            Robot = Robot,
            IsarMissionId = "isarId",
            EchoMissionId = "echoId",
            Log = "",
            ReportStatus = ReportStatus.InProgress,
            StartTime = DateTimeOffset.Now,
            EndTime = DateTimeOffset.Now.AddMinutes(3),
            Tasks = new List<IsarTask>()
        };

        public TestScheduledMissionEventHandler(DatabaseFixture fixture)
        {
            // Using Moq https://github.com/moq/moq4

            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var logger = new Mock<ILogger<ScheduledMissionEventHandler>>().Object;

            // Mock ScheduledMissionService:
            _context = fixture.NewContext;
            _scheduledMissionService = new ScheduledMissionService(_context);
            _robotControllerMock = new RobotControllerMock();

            var mockServiceProvider = new Mock<IServiceProvider>();

            // Mock injection of ScheduledMissionService:
            mockServiceProvider.Setup(p => p.GetService(typeof(IScheduledMissionService)))
                .Returns(_scheduledMissionService);
            // Mock injection of Robot Controller
            mockServiceProvider.Setup(p => p.GetService(typeof(RobotController)))
                .Returns(_robotControllerMock.Mock.Object);
            // Mock injection of Database context
            mockServiceProvider.Setup(p => p.GetService(typeof(FlotillaDbContext)))
                .Returns(_context);

            // Mock service injector
            var mockScope = new Mock<IServiceScope>();
            mockScope.Setup(scope => scope.ServiceProvider)
                .Returns(mockServiceProvider.Object);
            var mockFactory = new Mock<IServiceScopeFactory>();
            mockFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

            _scheduledMissionEventHandler = new ScheduledMissionEventHandler(logger, mockFactory.Object);
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
        private async void AssertExpectedStatusChange(ScheduledMissionStatus preStatus, ScheduledMissionStatus postStatus)
        {
            // ARRANGE

            var cts = new CancellationTokenSource();

            // Add Scheduled mission
            await _scheduledMissionService.Create(ScheduledMission);

            // Assert start conditions
            var preMission = await _scheduledMissionService.ReadById(ScheduledMission.Id);
            Assert.NotNull(preMission);
            Assert.True(preMission!.Status == preStatus);

            // ACT

            // Start / Stop eventhandler 
            await _scheduledMissionEventHandler.StartAsync(cts.Token);
            await Task.Delay(100);
            await _scheduledMissionEventHandler.StopAsync(cts.Token);

            // ASSERT

            // Verify status change
            var postMission = await _scheduledMissionService.ReadById(ScheduledMission.Id);
            Assert.NotNull(postMission);
            Assert.True(postMission!.Status == postStatus);
        }

        [Fact]
        public void ScheduledMissionSetToOngoing()
        {
            // Mock happy path of 'RobotController.StartMission'

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            _robotControllerMock.RobotServiceMock.Setup(r => r.ReadById(Robot.Id)).Returns(async () => Robot);
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            _robotControllerMock.IsarServiceMock.Setup(i => i.StartMission(Robot, It.IsAny<IsarMissionDefinition>()).Result).Returns(TestReport);
            _robotControllerMock.EchoServiceMock.Setup(i => i.GetMissionById(It.IsAny<int>()).Result).Returns(EchoMission);

            AssertExpectedStatusChange(ScheduledMissionStatus.Pending, ScheduledMissionStatus.Ongoing);
        }

        [Fact]
        public void ScheduledMissionSetToWarning()
        {
            // Mock bad path of 'RobotController.StartMission'

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            _robotControllerMock.RobotServiceMock.Setup(r => r.ReadById(Robot.Id)).Returns(async () => null);
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

            AssertExpectedStatusChange(ScheduledMissionStatus.Pending, ScheduledMissionStatus.Warning);
        }

    }
}
