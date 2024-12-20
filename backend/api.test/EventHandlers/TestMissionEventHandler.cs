using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            var sourceServiceLogger = new Mock<ILogger<SourceService>>().Object;
            var errorHandlingServiceLogger = new Mock<ILogger<ErrorHandlingService>>().Object;
            var missionTaskServiceLogger = new Mock<ILogger<MissionTaskService>>().Object;

            var configuration = WebApplication.CreateBuilder().Configuration;

            var context = fixture.NewContext;

            var signalRService = new MockSignalRService();
            var accessRoleService = new AccessRoleService(context, new HttpContextAccessor());
            var userInfoService = new UserInfoService(context, new HttpContextAccessor(), new Mock<ILogger<UserInfoService>>().Object);

            _mqttService = new MqttService(mqttServiceLogger, configuration);

            var missionTaskService = new MissionTaskService(context, missionTaskServiceLogger);

            var missionLoader = new MockMissionLoader();
            var stidServiceMock = new MockStidService(context);
            var sourceService = new SourceService(context, sourceServiceLogger);
            var robotModelService = new RobotModelService(context);
            var taskDurationServiceMock = new MockTaskDurationService();
            var isarServiceMock = new MockIsarService();
            var installationService = new InstallationService(context, accessRoleService);
            var defaultLocalizationPoseService = new DefaultLocalizationPoseService(context);
            var plantService = new PlantService(context, installationService, accessRoleService);
            var inspectionAreaService = new InspectionAreaService(context, defaultLocalizationPoseService, installationService, plantService, accessRoleService, signalRService);
            var areaService = new AreaService(context, installationService, plantService, inspectionAreaService, defaultLocalizationPoseService, accessRoleService);
            var mapServiceMock = new MockMapService();
            _robotService = new RobotService(context, robotServiceLogger, robotModelService, signalRService, accessRoleService, installationService, inspectionAreaService);
            _missionRunService = new MissionRunService(context, signalRService, missionLogger, accessRoleService, missionTaskService, inspectionAreaService, _robotService, userInfoService);
            var missionDefinitionService = new MissionDefinitionService(context, missionLoader, signalRService, accessRoleService, missionDefinitionServiceLogger, _missionRunService, sourceService);
            _localizationService = new LocalizationService(localizationServiceLogger, _robotService, installationService, inspectionAreaService);
            var errorHandlingService = new ErrorHandlingService(errorHandlingServiceLogger, _robotService, _missionRunService);
            var returnToHomeService = new ReturnToHomeService(returnToHomeServiceLogger, _robotService, _missionRunService);
            _missionSchedulingService = new MissionSchedulingService(missionSchedulingServiceLogger, _missionRunService, _robotService,
                isarServiceMock, _localizationService, returnToHomeService, signalRService, errorHandlingService);
            var lastMissionRunService = new LastMissionRunService(missionDefinitionService);

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
        public async Task ScheduledMissionStartedWhenSystemIsAvailable()
        {
            // Arrange
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await _databaseUtilities.ReadOrNewInspectionArea(installation.InstallationCode, plant.PlantCode);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, inspectionArea);
            var missionRun = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, inspectionArea);

            // Act
            await _missionRunService.Create(missionRun);
            Thread.Sleep(100);

            // Assert
            var postTestMissionRun = await _missionRunService.ReadById(missionRun.Id, readOnly: true);
            Assert.Equal(MissionStatus.Ongoing, postTestMissionRun!.Status);
        }

        [Fact]
        public async Task SecondScheduledMissionQueuedIfRobotIsBusy()
        {
            // Arrange
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await _databaseUtilities.ReadOrNewInspectionArea(installation.InstallationCode, plant.PlantCode);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, inspectionArea);
            var missionRunOne = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, inspectionArea);
            var missionRunTwo = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, inspectionArea);

            // Act
            await _missionRunService.Create(missionRunOne);
            Thread.Sleep(100);
            await _missionRunService.Create(missionRunTwo);

            // Assert
            var postTestMissionRunOne = await _missionRunService.ReadById(missionRunOne.Id, readOnly: true);
            var postTestMissionRunTwo = await _missionRunService.ReadById(missionRunTwo.Id, readOnly: true);
            Assert.Equal(MissionStatus.Ongoing, postTestMissionRunOne!.Status);
            Assert.Equal(MissionStatus.Pending, postTestMissionRunTwo!.Status);
        }

        [Fact]
        public async Task NewMissionIsStartedWhenRobotBecomesAvailable()
        {
            // Arrange
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await _databaseUtilities.ReadOrNewInspectionArea(installation.InstallationCode, plant.PlantCode);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Busy, installation, inspectionArea);
            var missionRun = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, inspectionArea);

            await _missionRunService.Create(missionRun);
            Thread.Sleep(100);

            var mqttEventArgs = new MqttReceivedArgs(
                new IsarStatusMessage
                {
                    RobotName = robot.Name,
                    IsarId = robot.IsarId,
                    Status = RobotStatus.Available,
                    Timestamp = DateTime.UtcNow
                });

            // Act
            _mqttService.RaiseEvent(nameof(MqttService.MqttIsarStatusReceived), mqttEventArgs);
            Thread.Sleep(500);

            // Assert
            var postTestMissionRun = await _missionRunService.ReadById(missionRun.Id, readOnly: true);
            Assert.Equal(MissionStatus.Ongoing, postTestMissionRun!.Status);
        }

        [Fact]
        public async Task ReturnToHomeMissionIsStartedIfQueueIsEmptyWhenRobotBecomesAvailable()
        {
            // Arrange
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await _databaseUtilities.ReadOrNewInspectionArea(installation.InstallationCode, plant.PlantCode);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Busy, installation, inspectionArea);

            var mqttEventArgs = new MqttReceivedArgs(
                new IsarStatusMessage
                {
                    RobotName = robot.Name,
                    IsarId = robot.IsarId,
                    Status = RobotStatus.Available,
                    Timestamp = DateTime.UtcNow
                });

            // Act
            _mqttService.RaiseEvent(nameof(MqttService.MqttIsarStatusReceived), mqttEventArgs);

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
                }, readOnly: true);
            Assert.True(ongoingMission.Any());
        }

        [Fact]
        public async Task ReturnToHomeMissionIsNotStartedIfReturnToHomeIsNotSupported()
        {
            // Arrange
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await _databaseUtilities.ReadOrNewInspectionArea(installation.InstallationCode, plant.PlantCode);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Busy, installation, inspectionArea);
            robot.RobotCapabilities!.Remove(RobotCapabilitiesEnum.return_to_home);
            await _robotService.Update(robot);

            var mqttEventArgs = new MqttReceivedArgs(
                new IsarStatusMessage
                {
                    RobotName = robot.Name,
                    IsarId = robot.IsarId,
                    Status = RobotStatus.Available,
                    Timestamp = DateTime.UtcNow
                });

            // Act
            _mqttService.RaiseEvent(nameof(MqttService.MqttIsarStatusReceived), mqttEventArgs);

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
                }, readOnly: true);
            Assert.False(ongoingMission.Any());
        }

        [Fact]
        public async Task MissionRunIsStartedForOtherAvailableRobotIfOneRobotHasAnOngoingMissionRun()
        {
            // Arrange
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await _databaseUtilities.ReadOrNewInspectionArea(installation.InstallationCode, plant.PlantCode);
            var robotOne = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, inspectionArea);
            var robotTwo = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, inspectionArea);
            var missionRunOne = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robotOne, inspectionArea);
            var missionRunTwo = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robotTwo, inspectionArea);

            // Act (Ensure first mission is started)
            await _missionRunService.Create(missionRunOne);
            Thread.Sleep(100);

            // Assert
            var postStartMissionRunOne = await _missionRunService.ReadById(missionRunOne.Id, readOnly: true);
            Assert.NotNull(postStartMissionRunOne);
            Assert.Equal(MissionStatus.Ongoing, postStartMissionRunOne.Status);

            // Act (Ensure second mission is started for second robot)
            await _missionRunService.Create(missionRunTwo);
            Thread.Sleep(100);

            // Assert
            var postStartMissionRunTwo = await _missionRunService.ReadById(missionRunTwo.Id, readOnly: true);
            Assert.NotNull(postStartMissionRunTwo);
            Assert.Equal(MissionStatus.Ongoing, postStartMissionRunTwo.Status);
        }

        [Fact]
        public async Task QueuedMissionsAreNotAbortedWhenRobotAvailableHappensAtTheSameTimeAsOnIsarMissionCompleted()
        {
            // Arrange
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await _databaseUtilities.ReadOrNewInspectionArea(installation.InstallationCode, plant.PlantCode);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, null);
            var missionRun1 = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, inspectionArea, true);
            Thread.Sleep(100);
            var missionRun2 = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, inspectionArea, true);
            Thread.Sleep(100);

            var missionRunCreatedEventArgs = new MissionRunCreatedEventArgs(missionRun1.Id);
            _missionRunService.RaiseEvent(nameof(MissionRunService.MissionRunCreated), missionRunCreatedEventArgs);
            Thread.Sleep(100);

            var missionRun1PostCreation = await _missionRunService.ReadById(missionRun1.Id, readOnly: true);
            Assert.NotNull(missionRun1PostCreation);

            // Act
            var mqttIsarMissionEventArgs = new MqttReceivedArgs(
                new IsarMissionMessage
                {
                    RobotName = robot.Name,
                    IsarId = robot.IsarId,
                    MissionId = missionRun1PostCreation.IsarMissionId,
                    Status = "successful",
                    Timestamp = DateTime.UtcNow
                });

            var robotAvailableEventArgs = new RobotAvailableEventArgs(robot.Id);

            _mqttService.RaiseEvent(nameof(MqttService.MqttIsarMissionReceived), mqttIsarMissionEventArgs);
            _missionSchedulingService.RaiseEvent(nameof(MissionSchedulingService.RobotAvailable), robotAvailableEventArgs);
            Thread.Sleep(500);

            // Assert
            var postTestMissionRun1 = await _missionRunService.ReadById(missionRun1.Id, readOnly: true);
            Assert.Equal(MissionStatus.Successful, postTestMissionRun1!.Status);
            var postTestMissionRun2 = await _missionRunService.ReadById(missionRun2.Id, readOnly: true);
            Assert.Equal(MissionStatus.Pending, postTestMissionRun2!.Status);
        }

#pragma warning disable xUnit1004
        [Fact(Skip = "Skipping until issue #1767 is solved because test is unreliable")]
#pragma warning restore xUnit1004
        public async Task QueuedContinuesWhenOnIsarStatusHappensAtTheSameTimeAsOnIsarMissionCompleted()
        {
            // Arrange
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await _databaseUtilities.ReadOrNewInspectionArea(installation.InstallationCode, plant.PlantCode);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, inspectionArea);
            var missionRun1 = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, inspectionArea, true, MissionRunType.Normal, MissionStatus.Ongoing, Guid.NewGuid().ToString());
            var missionRun2 = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, inspectionArea, true);
            Thread.Sleep(100);

            var missionRunCreatedEventArgs = new MissionRunCreatedEventArgs(missionRun1.Id);
            _missionRunService.RaiseEvent(nameof(MissionRunService.MissionRunCreated), missionRunCreatedEventArgs);
            Thread.Sleep(100);

            // Act
            var mqttIsarMissionEventArgs = new MqttReceivedArgs(
                new IsarMissionMessage
                {
                    RobotName = robot.Name,
                    IsarId = robot.IsarId,
                    MissionId = missionRun1.IsarMissionId,
                    Status = "successful",
                    Timestamp = DateTime.UtcNow
                });

            var mqttIsarStatusEventArgs = new MqttReceivedArgs(
                new IsarStatusMessage
                {
                    RobotName = robot.Name,
                    IsarId = robot.IsarId,
                    Status = RobotStatus.Available,
                    Timestamp = DateTime.UtcNow
                });

            _mqttService.RaiseEvent(nameof(MqttService.MqttIsarMissionReceived), mqttIsarMissionEventArgs);
            _mqttService.RaiseEvent(nameof(MqttService.MqttIsarStatusReceived), mqttIsarStatusEventArgs);
            Thread.Sleep(2500); // Accommodate for sleep in OnIsarStatus

            // Assert
            var postTestMissionRun1 = await _missionRunService.ReadById(missionRun1.Id, readOnly: true);
            Assert.Equal(Api.Database.Models.TaskStatus.Successful, postTestMissionRun1!.Tasks[0].Status);
            var postTestMissionRun2 = await _missionRunService.ReadById(missionRun2.Id, readOnly: true);
            Assert.Equal(MissionStatus.Ongoing, postTestMissionRun2!.Status);
        }

        [Fact]
        public async Task ReturnHomeMissionAbortedIfNewMissionScheduled()
        {
            // Arrange
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await _databaseUtilities.ReadOrNewInspectionArea(installation.InstallationCode, plant.PlantCode);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Busy, installation, inspectionArea);
            var returnToHomeMission = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, inspectionArea, true, MissionRunType.ReturnHome, MissionStatus.Ongoing, Guid.NewGuid().ToString());
            var missionRun = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, inspectionArea, true, MissionRunType.Normal, MissionStatus.Pending, Guid.NewGuid().ToString());

            Thread.Sleep(100);

            // Act
            var eventArgs = new MissionRunCreatedEventArgs(missionRun.Id);
            _missionRunService.RaiseEvent(nameof(MissionRunService.MissionRunCreated), eventArgs);
            Thread.Sleep(500);

            // Assert
            var updatedReturnHomeMission = await _missionRunService.ReadById(returnToHomeMission.Id, readOnly: true);
            Assert.True(updatedReturnHomeMission?.Status.Equals(MissionStatus.Aborted));

            // Act
            var mqttIsarMissionEventArgs = new MqttReceivedArgs(
                new IsarMissionMessage
                {
                    RobotName = robot.Name,
                    IsarId = robot.IsarId,
                    MissionId = returnToHomeMission.IsarMissionId,
                    Status = "cancelled",
                    Timestamp = DateTime.UtcNow
                });

            var mqttIsarStatusEventArgs = new MqttReceivedArgs(
                new IsarStatusMessage
                {
                    RobotName = robot.Name,
                    IsarId = robot.IsarId,
                    Status = RobotStatus.Available,
                    Timestamp = DateTime.UtcNow
                });

            _mqttService.RaiseEvent(nameof(MqttService.MqttIsarMissionReceived), mqttIsarMissionEventArgs);
            _mqttService.RaiseEvent(nameof(MqttService.MqttIsarStatusReceived), mqttIsarStatusEventArgs);
            Thread.Sleep(500);

            var updatedMissionRun = await _missionRunService.ReadById(missionRun.Id, readOnly: true);
            Assert.True(updatedMissionRun?.Status.Equals(MissionStatus.Ongoing));
        }
    }
}
