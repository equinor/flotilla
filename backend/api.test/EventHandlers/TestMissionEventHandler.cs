using System;
using System.Linq;
using System.Threading;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.EventHandlers;
using Api.Mqtt;
using Api.Mqtt.Events;
using Api.Mqtt.MessageModels;
using Api.Options;
using Api.Services;
using Api.Services.ActionServices;
using Api.Services.Events;
using Api.Test.Database;
using Api.Test.Mocks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Api.Test.EventHandlers
{
    [Collection("Database collection")]
    public class TestMissionEventHandler : IDisposable
    {
        private readonly MissionEventHandler _missionEventHandler;
        private readonly MissionRunService _missionRunService;
        private readonly MqttEventHandler _mqttEventHandler;
        private readonly MqttService _mqttService;
        private readonly DatabaseUtilities _databaseUtilities;
        private readonly RobotService _robotService;
        private readonly LocalizationService _localizationService;
        private readonly EmergencyActionService _emergencyActionService;
        private readonly MissionSchedulingService _missionSchedulingService;

        public TestMissionEventHandler(DatabaseFixture fixture)
        {
            var missionEventHandlerLogger = new Mock<ILogger<MissionEventHandler>>().Object;
            var mqttServiceLogger = new Mock<ILogger<MqttService>>().Object;
            var mqttEventHandlerLogger = new Mock<ILogger<MqttEventHandler>>().Object;
            var missionLogger = new Mock<ILogger<MissionRunService>>().Object;
            var missionSchedulingServiceLogger = new Mock<ILogger<MissionSchedulingService>>().Object;
            var robotServiceLogger = new Mock<ILogger<RobotService>>().Object;
            var localizationServiceLogger = new Mock<ILogger<LocalizationService>>().Object;
            var mapServiceLogger = new Mock<ILogger<MapService>>().Object;
            var mapBlobOptions = new Mock<IOptions<MapBlobOptions>>().Object;
            var returnToHomeServiceLogger = new Mock<ILogger<ReturnToHomeService>>().Object;
            var missionDefinitionServiceLogger = new Mock<ILogger<MissionDefinitionService>>().Object;
            var lastMissionRunServiceLogger = new Mock<ILogger<LastMissionRunService>>().Object;

            var configuration = WebApplication.CreateBuilder().Configuration;

            var context = fixture.NewContext;

            var signalRService = new MockSignalRService();
            var accessRoleService = new AccessRoleService(context, new HttpContextAccessor());

            _mqttService = new MqttService(mqttServiceLogger, configuration);
            _missionRunService = new MissionRunService(context, signalRService, missionLogger, accessRoleService);


            var echoServiceMock = new MockEchoService();
            var stidServiceMock = new MockStidService(context);
            var customMissionServiceMock = new MockCustomMissionService();
            var missionDefinitionService = new MissionDefinitionService(context, echoServiceMock, customMissionServiceMock, signalRService, accessRoleService, missionDefinitionServiceLogger);
            var robotModelService = new RobotModelService(context);
            var taskDurationServiceMock = new MockTaskDurationService();
            var isarServiceMock = new MockIsarService();
            var installationService = new InstallationService(context, accessRoleService);
            var defaultLocalizationPoseService = new DefaultLocalizationPoseService(context);
            var plantService = new PlantService(context, installationService, accessRoleService);
            var deckService = new DeckService(context, defaultLocalizationPoseService, installationService, plantService, accessRoleService, signalRService);
            var areaService = new AreaService(context, installationService, plantService, deckService, defaultLocalizationPoseService, accessRoleService);
            var mapServiceMock = new MockMapService();
            _robotService = new RobotService(context, robotServiceLogger, robotModelService, signalRService, accessRoleService, installationService, areaService, _missionRunService);
            _localizationService = new LocalizationService(localizationServiceLogger, _robotService, _missionRunService, installationService, areaService, mapServiceMock);

            var returnToHomeService = new ReturnToHomeService(returnToHomeServiceLogger, _robotService, _missionRunService, mapServiceMock);
            _missionSchedulingService = new MissionSchedulingService(missionSchedulingServiceLogger, _missionRunService, _robotService, areaService,
                isarServiceMock, _localizationService, returnToHomeService, signalRService);
            var lastMissionRunService = new LastMissionRunService(lastMissionRunServiceLogger, missionDefinitionService, _missionRunService);

            _databaseUtilities = new DatabaseUtilities(context);
            _emergencyActionService = new EmergencyActionService();

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
                .Setup(p => p.GetService(typeof(FlotillaDbContext)))
                .Returns(context);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(ILocalizationService)))
                .Returns(_localizationService);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IReturnToHomeService)))
                .Returns(returnToHomeService);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IMapService)))
                .Returns(mapServiceMock);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(ITaskDurationService)))
                .Returns(taskDurationServiceMock);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(ILastMissionRunService)))
                .Returns(lastMissionRunService);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IAreaService)))
                .Returns(areaService);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(ISignalRService)))
                .Returns(signalRService);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IEmergencyActionService)))
                .Returns(_emergencyActionService);


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

        [Fact]
        public async void ScheduledMissionStartedWhenSystemIsAvailable()
        {
            // Arrange
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);
            var missionRun = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area);

            // Act
            await _missionRunService.Create(missionRun);
            Thread.Sleep(100);

            // Assert
            var postTestMissionRun = await _missionRunService.ReadById(missionRun.Id);
            Assert.Equal(MissionStatus.Ongoing, postTestMissionRun!.Status);
        }

        [Fact]
        public async void SecondScheduledMissionQueuedIfRobotIsBusy()
        {
            // Arrange
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);
            var missionRunOne = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area);
            var missionRunTwo = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area);

            // Act
            await _missionRunService.Create(missionRunOne);
            Thread.Sleep(100);
            await _missionRunService.Create(missionRunTwo);

            // Assert
            var postTestMissionRunOne = await _missionRunService.ReadById(missionRunOne.Id);
            var postTestMissionRunTwo = await _missionRunService.ReadById(missionRunTwo.Id);
            Assert.Equal(MissionStatus.Ongoing, postTestMissionRunOne!.Status);
            Assert.Equal(MissionStatus.Pending, postTestMissionRunTwo!.Status);
        }

#pragma warning disable xUnit1004
        [Fact(Skip = "Skipping until a solution has been found for ExecuteUpdate in tests")]
#pragma warning restore xUnit1004
        public async void NewMissionIsStartedWhenRobotBecomesAvailable()
        {
            // Arrange
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Busy, installation, area);
            var missionRun = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area);

            await _missionRunService.Create(missionRun);
            Thread.Sleep(100);

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
            Thread.Sleep(500);

            // Assert
            var postTestMissionRun = await _missionRunService.ReadById(missionRun.Id);
            Assert.Equal(MissionStatus.Ongoing, postTestMissionRun!.Status);
        }

        [Fact]
        public async void ReturnToHomeMissionIsStartedIfQueueIsEmptyWhenRobotBecomesAvailable()
        {
            // Arrange
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Busy, installation, area);

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
            Thread.Sleep(1000);
            var ongoingMission = await _missionRunService.ReadAll(
                new MissionRunQueryStringParameters
                {
                    Statuses = [
                        MissionStatus.Ongoing
                    ],
                    OrderBy = "DesiredStartTime",
                    PageSize = 100
                });
            Assert.True(ongoingMission.Any());
        }

        [Fact]
        public async void MissionRunIsStartedForOtherAvailableRobotIfOneRobotHasAnOngoingMissionRun()
        {
            // Arrange
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robotOne = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);
            var robotTwo = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);
            var missionRunOne = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robotOne, area);
            var missionRunTwo = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robotTwo, area);

            // Act (Ensure first mission is started)
            await _missionRunService.Create(missionRunOne);
            Thread.Sleep(100);

            // Assert
            var postStartMissionRunOne = await _missionRunService.ReadById(missionRunOne.Id);
            Assert.NotNull(postStartMissionRunOne);
            Assert.Equal(MissionStatus.Ongoing, postStartMissionRunOne.Status);

            // Act (Ensure second mission is started for second robot)
            await _missionRunService.Create(missionRunTwo);
            Thread.Sleep(100);

            // Assert
            var postStartMissionRunTwo = await _missionRunService.ReadById(missionRunTwo.Id);
            Assert.NotNull(postStartMissionRunTwo);
            Assert.Equal(MissionStatus.Ongoing, postStartMissionRunTwo.Status);
        }

        [Fact]
        public async void QueuedMissionsAreAbortedWhenLocalizationFails()
        {
            // Arrange
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);
            var localizationMissionRun = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area, true, MissionRunType.Localization, MissionStatus.Ongoing, Guid.NewGuid().ToString());
            var missionRun1 = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area, true);

            Thread.Sleep(100);

            var mqttEventArgs = new MqttReceivedArgs(
                new IsarMissionMessage
                {
                    RobotName = robot.Name,
                    IsarId = robot.IsarId,
                    MissionId = localizationMissionRun.IsarMissionId,
                    Status = "failed",
                    Timestamp = DateTime.UtcNow
                });

            // Act
            _mqttService.RaiseEvent(nameof(MqttService.MqttIsarMissionReceived), mqttEventArgs);
            Thread.Sleep(500);

            // Assert
            var postTestMissionRun = await _missionRunService.ReadById(missionRun1.Id);
            Assert.Equal(MissionStatus.Aborted, postTestMissionRun!.Status);
        }

        [Fact]
        public async void LocalizationMissionCompletesAfterPressingSendToSafeZoneButton()
        {
            // Arrange
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Busy, installation, area);
            await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area, true, MissionRunType.Localization, MissionStatus.Ongoing, Guid.NewGuid().ToString());

            Thread.Sleep(100);

            // Act
            var eventArgs = new EmergencyButtonPressedForRobotEventArgs(robot.Id);
            _emergencyActionService.RaiseEvent(nameof(EmergencyActionService.EmergencyButtonPressedForRobot), eventArgs);

            Thread.Sleep(1000);

            // Assert 
            var updatedRobot = await _robotService.ReadById(robot.Id);
            Assert.True(updatedRobot?.MissionQueueFrozen);

            bool isRobotLocalized = await _localizationService.RobotIsLocalized(robot.Id);
            Assert.True(isRobotLocalized);
        }

        [Fact]
        public async void ReturnHomeMissionNotScheduledIfRobotIsNotLocalized()
        {
            // Arrange
            var installation = await _databaseUtilities.NewInstallation();
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Busy, installation, null);

            Thread.Sleep(100);

            // Act
            var eventArgs = new RobotAvailableEventArgs(robot.Id);
            _missionSchedulingService.RaiseEvent(nameof(MissionSchedulingService.RobotAvailable), eventArgs);

            Thread.Sleep(100);

            // Assert 
            bool isRobotLocalized = await _localizationService.RobotIsLocalized(robot.Id);
            Assert.False(isRobotLocalized);
            Assert.False(await _missionRunService.PendingOrOngoingReturnToHomeMissionRunExists(robot.Id));

        }

        [Fact]
        public async void ReturnHomeMissionCancelledIfNewMissionScheduled()
        {
            // Arrange
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Busy, installation, area);
            var returnToHomeMission = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area, true, MissionRunType.ReturnHome, MissionStatus.Ongoing, Guid.NewGuid().ToString());
            var missionRun = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area, true, MissionRunType.Normal, MissionStatus.Pending, Guid.NewGuid().ToString());

            Thread.Sleep(100);

            // Act
            var eventArgs = new MissionRunCreatedEventArgs(missionRun.Id);
            _missionRunService.RaiseEvent(nameof(MissionRunService.MissionRunCreated), eventArgs);

            Thread.Sleep(500);

            // Assert 
            var updatedReturnHomeMission = await _missionRunService.ReadById(returnToHomeMission.Id);
            Assert.True(updatedReturnHomeMission?.Status.Equals(MissionStatus.Cancelled));
            Assert.True(updatedReturnHomeMission?.Tasks.FirstOrDefault()?.Status.Equals(TaskStatus.Cancelled));
        }

        [Fact]
        public async void ReturnHomeMissionNotCancelledIfNewMissionScheduledInDifferentDeck()
        {
            // Arrange
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck1 = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area1 = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck1.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Busy, installation, area1);
            var deck2 = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode, "testDeck2");
            var area2 = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck2.Name);
            var returnToHomeMission = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area1, true, MissionRunType.ReturnHome, MissionStatus.Ongoing, Guid.NewGuid().ToString());
            var missionRun = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area2, true, MissionRunType.Normal, MissionStatus.Pending, Guid.NewGuid().ToString());

            Thread.Sleep(100);

            // Act
            var eventArgs = new MissionRunCreatedEventArgs(missionRun.Id);
            _missionRunService.RaiseEvent(nameof(MissionRunService.MissionRunCreated), eventArgs);

            Thread.Sleep(500);

            // Assert 
            var updatedReturnHomeMission = await _missionRunService.ReadById(returnToHomeMission.Id);
            Assert.True(updatedReturnHomeMission?.Status.Equals(MissionStatus.Ongoing));
        }
    }
}
