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

        private static readonly Robot robot = new()
        {
            Id = "IamTestRobot",
            Status = RobotStatus.Available,
            Host = "localhost",
            Model = "TesTModel",
            Name = "TestosteroneTesty",
            SerialNumber = "12354"
        };
        private static readonly ScheduledMission scheduledMission = new()
        {
            Id = "testScheduledMission",
            EchoMissionId = 2,
            Robot = robot,
            Status = ScheduledMissionStatus.Pending,
            StartTime = DateTimeOffset.Now
        };
        private static readonly EchoMission echoMission = new()
        {
            Tags = new List<EchoTag>() {
                    new EchoTag() { Id = 1,
                        Inspections = new List<EchoInspection>() {
                        new EchoInspection(IsarStep.InspectionTypeEnum.Image, null) }, TagId = "123", URL = new Uri("http://localhost:3000") } }
        };
        private static readonly Report testReport = new()
        {
            Id = "id",
            Robot = robot,
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
            GC.SuppressFinalize(this);
        }
        [Fact]
        public async void ScheduledMissionSetToOngoing()
        {
            // Mock happy path of 'RobotController.StartMission'

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            _robotControllerMock.RobotServiceMock.Setup(r => r.ReadById(robot.Id)).Returns(async () => robot);
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            _robotControllerMock.IsarServiceMock.Setup(i => i.StartMission(robot, It.IsAny<IsarMissionDefinition>()).Result).Returns(testReport);
            _robotControllerMock.EchoServiceMock.Setup(i => i.GetMissionById(It.IsAny<int>()).Result).Returns(echoMission);

            // Add Scheduled mission
            await _scheduledMissionService.Create(scheduledMission);

            var preMission = await _scheduledMissionService.ReadById(scheduledMission.Id);
            Assert.True(preMission?.Status == ScheduledMissionStatus.Pending);

            // Start eventhandler
            await _scheduledMissionEventHandler.StartAsync(new CancellationToken());
            await Task.Delay(500);

            // Verify status change
            var postMission = await _scheduledMissionService.ReadById(scheduledMission.Id);
            Assert.True(postMission?.Status == ScheduledMissionStatus.Ongoing);
        }
    }
}
