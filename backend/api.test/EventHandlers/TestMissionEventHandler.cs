using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Controllers;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.EventHandlers;
using Api.Mqtt;
using Api.Mqtt.Events;
using Api.Mqtt.MessageModels;
using Api.Services;
using Api.Services.Models;
using Api.Test.Mocks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
namespace Api.Test.EventHandlers
{
    [Collection("Database collection")]
    public class TestMissionEventHandler : IDisposable
    {
        private static readonly Installation testInstallation = new()
        {
            InstallationCode = "test",
            Name = "test test"
        };
        private static readonly Plant testPlant = new()
        {
            PlantCode = "test",
            Name = "test test",
            Installation = testInstallation
        };
        private readonly FlotillaDbContext _context;

        private readonly MissionEventHandler _missionEventHandler;
        private readonly IMissionRunService _missionRunService;

#pragma warning disable IDE0052
        private readonly MqttEventHandler _mqttEventHandler;
#pragma warning restore IDE0052

        private readonly MqttService _mqttService;
        private readonly RobotControllerMock _robotControllerMock;
        private readonly IRobotModelService _robotModelService;
        private readonly IRobotService _robotService;
        private readonly IMissionScheduling _missionSchedulingService;
        private readonly IIsarService _isarServiceMock;
        private readonly IInstallationService _installationService;
        private readonly IPlantService _plantService;
        private readonly IDeckService _deckService;
        private readonly IAreaService _areaService;

        public TestMissionEventHandler(DatabaseFixture fixture)
        {
            var missionEventHandlerLogger = new Mock<ILogger<MissionEventHandler>>().Object;
            var mqttServiceLogger = new Mock<ILogger<MqttService>>().Object;
            var mqttEventHandlerLogger = new Mock<ILogger<MqttEventHandler>>().Object;
            var missionLogger = new Mock<ILogger<MissionRunService>>().Object;
            var missionSchedulingLogger = new Mock<ILogger<MissionScheduling>>().Object;

            var configuration = WebApplication.CreateBuilder().Configuration;

            _context = fixture.NewContext;

            _mqttService = new MqttService(mqttServiceLogger, configuration);
            _missionRunService = new MissionRunService(_context, missionLogger);
            _robotModelService = new RobotModelService(_context);
            _robotService = new RobotService(_context, _robotModelService);
            _robotModelService = new RobotModelService(_context);
            _robotControllerMock = new RobotControllerMock();
            _isarServiceMock = new MockIsarService();
            _installationService = new InstallationService(_context);
            _plantService = new PlantService(_context, _installationService);
            _deckService = new DeckService(_context, _installationService, _plantService);
            _areaService = new AreaService(_context, _installationService, _plantService, _deckService);
            _missionSchedulingService = new MissionScheduling(missionSchedulingLogger, _missionRunService, _isarServiceMock, _robotService, _robotControllerMock.Mock.Object, _areaService);

            var mockServiceProvider = new Mock<IServiceProvider>();

            // Mock services and controllers that are passed through the mocked service injector
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IMissionRunService)))
                .Returns(_missionRunService);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IRobotService)))
                .Returns(_robotService);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IMissionScheduling)))
                .Returns(_missionSchedulingService);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(RobotController)))
                .Returns(_robotControllerMock.Mock.Object);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(FlotillaDbContext)))
                .Returns(_context);

            // Mock service injector
            var mockScope = new Mock<IServiceScope>();
            mockScope.Setup(scope => scope.ServiceProvider).Returns(mockServiceProvider.Object);
            var mockFactory = new Mock<IServiceScopeFactory>();
            mockFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

            // Instantiating the event handlers are required for the event subscribers to be activated
            _missionEventHandler = new MissionEventHandler(missionEventHandlerLogger, mockFactory.Object);
            _mqttEventHandler = new MqttEventHandler(mqttEventHandlerLogger, mockFactory.Object);
        }

        private static Area NewArea => new()
        {
            Deck = new Deck
            {
                Plant = testPlant,
                Installation = testInstallation,
                Name = "testDeck"
            },
            Installation = testInstallation,
            Plant = testPlant,
            Name = "testArea",
            MapMetadata = new MapMetadata
            {
                MapName = "TestMap",
                Boundary = new Boundary(),
                TransformationMatrices = new TransformationMatrices()
            },
            DefaultLocalizationPose = null,
            SafePositions = new List<SafePosition>()
        };

        private static MissionRun ScheduledMission =>
            new()
            {
                Name = "testMission",
                MissionId = Guid.NewGuid().ToString(),
                MissionRunPriority = MissionRunPriority.Normal,
                Status = MissionStatus.Pending,
                DesiredStartTime = DateTimeOffset.Now,
                Area = NewArea,
                Map = new MapMetadata
                {
                    MapName = "TestMap",
                    Boundary = new Boundary(),
                    TransformationMatrices = new TransformationMatrices()
                },
                InstallationCode = "testInstallation"
            };

        public void Dispose()
        {
            _missionEventHandler.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task<Robot> NewRobot(RobotStatus status)
        {
            var createRobotQuery = new CreateRobotQuery
            {
                Name = "TestBot",
                IsarId = Guid.NewGuid().ToString(),
                RobotType = RobotType.Robot,
                SerialNumber = "0001",
                CurrentInstallation = "kaa",
                CurrentArea = NewArea,
                VideoStreams = new List<CreateVideoStreamQuery>(),
                Host = "localhost",
                Port = 3000,
                Enabled = true,
                Status = status
            };

            var robotModel = await _robotModelService.ReadByRobotType(createRobotQuery.RobotType);
            return new Robot(createRobotQuery)
            {
                Model = robotModel!
            };
        }

        [Fact]
        public async void ScheduledMissionStartedWhenSystemIsAvailable()
        {
            // Arrange
            var missionRun = ScheduledMission;

            var robot = await NewRobot(RobotStatus.Available);
            await _robotService.Create(robot);
            missionRun.Robot = robot;

            SetupMocksForRobotController(robot, missionRun);

            // Act
            await _missionRunService.Create(missionRun);

            // Assert
            var postTestMissionRun = await _missionRunService.ReadById(missionRun.Id);
            Assert.Equal(MissionStatus.Ongoing, postTestMissionRun!.Status);
        }

        [Fact]
        public async void SecondScheduledMissionQueuedIfRobotIsBusy()
        {
            // Arrange
            var missionRunOne = ScheduledMission;
            var missionRunTwo = ScheduledMission;

            var robot = await NewRobot(RobotStatus.Available);
            await _robotService.Create(robot);

            missionRunOne.Robot = robot;
            missionRunTwo.Robot = robot;

            SetupMocksForRobotController(robot, missionRunOne);

            // Act
            await _missionRunService.Create(missionRunOne);
            await _missionRunService.Create(missionRunTwo);

            // Assert
            var postTestMissionRunOne = await _missionRunService.ReadById(missionRunOne.Id);
            var postTestMissionRunTwo = await _missionRunService.ReadById(missionRunTwo.Id);
            Assert.Equal(MissionStatus.Ongoing, postTestMissionRunOne!.Status);
            Assert.Equal(MissionStatus.Pending, postTestMissionRunTwo!.Status);
        }

        [Fact]
        public async void NewMissionIsStartedWhenRobotBecomesAvailable()
        {
            // Arrange
            var missionRun = ScheduledMission;

            var robot = await NewRobot(RobotStatus.Busy);
            await _robotService.Create(robot);
            missionRun.Robot = robot;

            SetupMocksForRobotController(robot, missionRun);

            await _missionRunService.Create(missionRun);

            var mqttEventArgs = new MqttReceivedArgs(
                new IsarRobotStatusMessage
                {
                    RobotName = robot.Name,
                    IsarId = robot.IsarId,
                    RobotStatus = RobotStatus.Available,
                    PreviousRobotStatus = RobotStatus.Busy,
                    CurrentState = "idle",
                    CurrentMissionId = "",
                    CurrentTaskId = "",
                    CurrentStepId = "",
                    Timestamp = DateTime.UtcNow
                });

            // Act
            _mqttService.RaiseEvent(nameof(MqttService.MqttIsarRobotStatusReceived), mqttEventArgs);

            // Assert
            var postTestMissionRun = await _missionRunService.ReadById(missionRun.Id);
            Assert.Equal(MissionStatus.Ongoing, postTestMissionRun!.Status);
        }

        [Fact]
        public async void NoMissionIsStartedIfQueueIsEmptyWhenRobotBecomesAvailable()
        {
            // Arrange
            var robot = await NewRobot(RobotStatus.Busy);
            await _robotService.Create(robot);

            var mqttEventArgs = new MqttReceivedArgs(
                new IsarRobotStatusMessage
                {
                    RobotName = robot.Name,
                    IsarId = robot.IsarId,
                    RobotStatus = RobotStatus.Available,
                    PreviousRobotStatus = RobotStatus.Busy,
                    CurrentState = "idle",
                    CurrentMissionId = "",
                    CurrentTaskId = "",
                    CurrentStepId = "",
                    Timestamp = DateTime.UtcNow
                });

            // Act
            _mqttService.RaiseEvent(nameof(MqttService.MqttIsarRobotStatusReceived), mqttEventArgs);

            // Assert
            var ongoingMission = await _missionRunService.ReadAll(
                new MissionRunQueryStringParameters
                {
                    Statuses = new List<MissionStatus>
                    {
                        MissionStatus.Ongoing
                    },
                    OrderBy = "DesiredStartTime",
                    PageSize = 100
                });
            Assert.False(ongoingMission.Any());
        }

        [Fact]
        public async void MissionRunIsStartedForOtherAvailableRobotIfOneRobotHasAnOngoingMissionRun()
        {
            // Arrange
            var robotOne = await NewRobot(RobotStatus.Available);
            var robotTwo = await NewRobot(RobotStatus.Available);
            await _robotService.Create(robotOne);
            await _robotService.Create(robotTwo);

            var missionRunOne = ScheduledMission;
            var missionRunTwo = ScheduledMission;

            missionRunOne.Robot = robotOne;
            missionRunTwo.Robot = robotTwo;

            SetupMocksForRobotController(robotOne, missionRunOne);

            // Act (Ensure first mission is started)
            await _missionRunService.Create(missionRunOne);

            // Assert
            var postStartMissionRunOne = await _missionRunService.ReadById(missionRunOne.Id);
            Assert.NotNull(postStartMissionRunOne);
            Assert.Equal(MissionStatus.Ongoing, postStartMissionRunOne.Status);

            // Rearrange
            SetupMocksForRobotController(robotTwo, missionRunTwo);

            // Act (Ensure second mission is started for second robot)
            await _missionRunService.Create(missionRunTwo);

            // Assert
            var postStartMissionRunTwo = await _missionRunService.ReadById(missionRunTwo.Id);
            Assert.NotNull(postStartMissionRunTwo);
            Assert.Equal(MissionStatus.Ongoing, postStartMissionRunTwo.Status);
        }

        private void SetupMocksForRobotController(Robot robot, MissionRun missionRun)
        {
            _robotControllerMock.IsarServiceMock
                .Setup(isar => isar.StartMission(robot, missionRun))
                .ReturnsAsync(
                    () =>
                        new IsarMission(
                            new IsarStartMissionResponse
                            {
                                MissionId = "test",
                                Tasks = new List<IsarTaskResponse>()
                            }
                        )
                );

            _robotControllerMock.RobotServiceMock
                .Setup(service => service.ReadById(robot.Id))
                .ReturnsAsync(() => robot);

            // This mock uses "It.IsAny" rather than the specific ID as the ID is created once the object is written to the database
            _robotControllerMock.MissionServiceMock
                .Setup(service => service.ReadById(It.IsAny<string>()))
                .ReturnsAsync(() => missionRun);
        }
    }
}
