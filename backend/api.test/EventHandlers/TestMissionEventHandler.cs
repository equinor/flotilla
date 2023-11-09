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
        private readonly IAreaService _areaService;
        private readonly FlotillaDbContext _context;
        private readonly IDeckService _deckService;
        private readonly IDefaultLocalizationPoseService _defaultLocalisationPoseService;
        private readonly IInstallationService _installationService;
        private readonly IIsarService _isarServiceMock;

        private readonly MissionEventHandler _missionEventHandler;
        private readonly IMissionRunService _missionRunService;
        private readonly IMissionSchedulingService _missionSchedulingService;

#pragma warning disable IDE0052
        private readonly MqttEventHandler _mqttEventHandler;
#pragma warning restore IDE0052

        private readonly MqttService _mqttService;
        private readonly IPlantService _plantService;
        private readonly RobotControllerMock _robotControllerMock;
        private readonly IRobotModelService _robotModelService;
        private readonly IRobotService _robotService;
        private readonly ISignalRService _signalRService;

        public TestMissionEventHandler(DatabaseFixture fixture)
        {
            var missionEventHandlerLogger = new Mock<ILogger<MissionEventHandler>>().Object;
            var mqttServiceLogger = new Mock<ILogger<MqttService>>().Object;
            var mqttEventHandlerLogger = new Mock<ILogger<MqttEventHandler>>().Object;
            var missionLogger = new Mock<ILogger<MissionRunService>>().Object;
            var missionSchedulingServiceLogger = new Mock<ILogger<MissionSchedulingService>>().Object;

            var configuration = WebApplication.CreateBuilder().Configuration;

            _context = fixture.NewContext;

            _signalRService = new MockSignalRService();
            _mqttService = new MqttService(mqttServiceLogger, configuration);
            _missionRunService = new MissionRunService(_context, _signalRService, missionLogger);
            _robotModelService = new RobotModelService(_context);
            _robotService = new RobotService(_context, _robotModelService, _signalRService);
            _robotModelService = new RobotModelService(_context);
            _robotControllerMock = new RobotControllerMock();
            _isarServiceMock = new MockIsarService();
            _installationService = new InstallationService(_context);
            _defaultLocalisationPoseService = new DefaultLocalizationPoseService(_context);
            _plantService = new PlantService(_context, _installationService);
            _deckService = new DeckService(_context, _defaultLocalisationPoseService, _installationService, _plantService);
            _areaService = new AreaService(_context, _installationService, _plantService, _deckService, _defaultLocalisationPoseService);
            _missionSchedulingService = new MissionSchedulingService(missionSchedulingServiceLogger, _missionRunService, _robotService, _robotControllerMock.Mock.Object, _areaService,
                _isarServiceMock);

            var mockServiceProvider = new Mock<IServiceProvider>();

            // Mock services and controllers that are passed through the mocked service injector
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IMissionRunService)))
                .Returns(_missionRunService);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IRobotService)))
                .Returns(_robotService);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IMissionSchedulingService)))
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

        public void Dispose()
        {
            _missionEventHandler.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task<MissionRun> NewMissionRun(string installationCode, Robot robot, Area? area, bool writeToDatabase = true)
        {
            var missionRun = new MissionRun
            {
                Name = "testMission",
                Robot = robot,
                MissionId = null,
                MissionRunPriority = MissionRunPriority.Normal,
                Status = MissionStatus.Pending,
                DesiredStartTime = DateTime.Now,
                Area = area,
                Map = new MapMetadata(),
                InstallationCode = installationCode
            };
            if (writeToDatabase) { return await _missionRunService.Create(missionRun); }
            return missionRun;
        }

        private async Task<Installation> NewInstallation()
        {
            var createInstallationQuery = new CreateInstallationQuery
            {
                InstallationCode = "testInstallationCode",
                Name = "testInstallation"
            };

            return await _installationService.Create(createInstallationQuery);
        }

        private async Task<Plant> NewPlant(string installationCode)
        {
            var createPlantQuery = new CreatePlantQuery
            {
                InstallationCode = installationCode,
                PlantCode = "testPlantCode",
                Name = "testPlant"
            };

            return await _plantService.Create(createPlantQuery);
        }

        private async Task<Deck> NewDeck(string installationCode, string plantCode)
        {
            var createDeckQuery = new CreateDeckQuery
            {
                InstallationCode = installationCode,
                PlantCode = plantCode,
                Name = "testDeck"
            };

            return await _deckService.Create(createDeckQuery);
        }

        private async Task<Area> NewArea(string installationCode, string plantCode, string deckName)
        {
            var createAreaQuery = new CreateAreaQuery
            {
                InstallationCode = installationCode,
                PlantCode = plantCode,
                DeckName = deckName,
                AreaName = "testArea",
                DefaultLocalizationPose = new Pose()
            };

            return await _areaService.Create(createAreaQuery);
        }

        private async Task<Robot> NewRobot(RobotStatus status, Area area)
        {
            var createRobotQuery = new CreateRobotQuery
            {
                Name = "TestBot",
                IsarId = Guid.NewGuid().ToString(),
                RobotType = RobotType.Robot,
                SerialNumber = "0001",
                CurrentInstallation = "kaa",
                CurrentArea = area,
                VideoStreams = new List<CreateVideoStreamQuery>(),
                Host = "localhost",
                Port = 3000,
                Enabled = true,
                Status = status
            };

            var robotModel = await _robotModelService.ReadByRobotType(createRobotQuery.RobotType);
            var robot = new Robot(createRobotQuery)
            {
                Model = robotModel!
            };
            return await _robotService.Create(robot);
        }

        [Fact]
        public async void ScheduledMissionStartedWhenSystemIsAvailable()
        {
            // Arrange
            var installation = await NewInstallation();
            var plant = await NewPlant(installation.InstallationCode);
            var deck = await NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await NewRobot(RobotStatus.Available, area);
            var missionRun = await NewMissionRun(installation.InstallationCode, robot, area, false);

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
            var installation = await NewInstallation();
            var plant = await NewPlant(installation.InstallationCode);
            var deck = await NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await NewRobot(RobotStatus.Available, area);
            var missionRunOne = await NewMissionRun(installation.InstallationCode, robot, area, false);
            var missionRunTwo = await NewMissionRun(installation.InstallationCode, robot, area, false);

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
            var installation = await NewInstallation();
            var plant = await NewPlant(installation.InstallationCode);
            var deck = await NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await NewRobot(RobotStatus.Busy, area);
            var missionRun = await NewMissionRun(installation.InstallationCode, robot, area, false);

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
            var installation = await NewInstallation();
            var plant = await NewPlant(installation.InstallationCode);
            var deck = await NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await NewRobot(RobotStatus.Busy, area);

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
            var installation = await NewInstallation();
            var plant = await NewPlant(installation.InstallationCode);
            var deck = await NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robotOne = await NewRobot(RobotStatus.Available, area);
            var robotTwo = await NewRobot(RobotStatus.Available, area);
            var missionRunOne = await NewMissionRun(installation.InstallationCode, robotOne, area, false);
            var missionRunTwo = await NewMissionRun(installation.InstallationCode, robotTwo, area, false);

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
