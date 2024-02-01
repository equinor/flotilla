using System;
using System.Linq;
using System.Threading;
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
using Api.Test.Database;
using Api.Test.Mocks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Org.BouncyCastle.Asn1.Cms;
using Xunit;

namespace Api.Test.EventHandlers
{
    [Collection("Database collection")]
    public class TestMissionEventHandler : IAsyncLifetime
    {
        private readonly IMissionRunService _missionRunService;
        private readonly MqttService _mqttService;
        private readonly IRobotService _robotService;
        private readonly IEmergencyActionService _emergencyActionService;
        private readonly DatabaseUtilities _databaseUtilities;
        private readonly EmergencyActionController _emergencyActionController;

        private readonly FlotillaDbContext _context;

        private readonly Func<Task> _resetDatabase;

        public TestMissionEventHandler(DatabaseFixture fixture)
        {
            var client = new TestWebApplicationFactory<Program>(fixture.ConnectionString);
            var serviceProvider = client.Services;

            _missionRunService = serviceProvider.GetRequiredService<IMissionRunService>();
            _robotService = serviceProvider.GetRequiredService<IRobotService>();
            _emergencyActionService = serviceProvider.GetRequiredService<IEmergencyActionService>();

            _emergencyActionController = serviceProvider.GetRequiredService<EmergencyActionController>();
            //_mqttService = serviceProvider.GetRequiredService<MqttService>();

            _databaseUtilities = new DatabaseUtilities(fixture.Context);
            _resetDatabase = fixture.ResetDatabase;
            _context = fixture.Context;

            /*var missionEventHandlerLogger = new Mock<ILogger<MissionEventHandler>>().Object;
            var mqttServiceLogger = new Mock<ILogger<MqttService>>().Object;
            var mqttEventHandlerLogger = new Mock<ILogger<MqttEventHandler>>().Object;
            var missionLogger = new Mock<ILogger<MissionRunService>>().Object;
            var missionSchedulingServiceLogger = new Mock<ILogger<MissionSchedulingService>>().Object;
            var robotServiceLogger = new Mock<ILogger<RobotService>>().Object;
            var localizationServiceLogger = new Mock<ILogger<LocalizationService>>().Object;

            var configuration = WebApplication.CreateBuilder().Configuration;

            var context = fixture.Context;
            _resetDatabase = fixture.ResetDatabase;

            var signalRService = new MockSignalRService();
            var accessRoleService = new AccessRoleService(context, new HttpContextAccessor());

            _mqttService = new MqttService(mqttServiceLogger, configuration);
            _missionRunService = new MissionRunService(context, signalRService, missionLogger, accessRoleService);

            var robotModelService = new RobotModelService(context);
            var isarServiceMock = new MockIsarService();
            var installationService = new InstallationService(context, accessRoleService);
            var defaultLocalisationPoseService = new DefaultLocalizationPoseService(context);
            var plantService = new PlantService(context, installationService, accessRoleService);
            var deckService = new DeckService(context, defaultLocalisationPoseService, installationService, plantService, accessRoleService);
            var areaService = new AreaService(context, installationService, plantService, deckService, defaultLocalisationPoseService, accessRoleService);

            _robotService = new RobotService(context, robotServiceLogger, robotModelService, signalRService, accessRoleService, installationService, areaService, _missionRunService);
            _emergencyActionService = new EmergencyActionService();

            var missionSchedulingService = new MissionSchedulingService(missionSchedulingServiceLogger, _missionRunService, _robotService, areaService,
                isarServiceMock);
            var localizationService = new LocalizationService(localizationServiceLogger, _robotService, _missionRunService, installationService, areaService);

            _databaseUtilities = new DatabaseUtilities(context);

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
                .Returns(missionSchedulingService);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(FlotillaDbContext)))
                .Returns(context);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(ILocalizationService)))
                .Returns(localizationService);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IAreaService)))
                .Returns(areaService);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(ISignalRService)))
                .Returns(signalRService);

            // Mock service injector
            var mockScope = new Mock<IServiceScope>();
            mockScope.Setup(scope => scope.ServiceProvider).Returns(mockServiceProvider.Object);
            var mockFactory = new Mock<IServiceScopeFactory>();
            mockFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

            // Instantiating the event handlers are required for the event subscribers to be activated
            _missionEventHandler = new MissionEventHandler(missionEventHandlerLogger, mockFactory.Object);
            _mqttEventHandler = new MqttEventHandler(mqttEventHandlerLogger, mockFactory.Object);*/
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await _resetDatabase();
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
            var missionRun = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area, false);

            // Act
            await _missionRunService.Create(missionRun);
            await Task.Delay(1000);

            // Assert
            var postTestMissionRun = await _missionRunService.ReadById(missionRun.Id, noTracking: true);
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
            var missionRunOne = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area, false);
            var missionRunTwo = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area, false);

            // Act
            await _missionRunService.Create(missionRunOne);
            Thread.Sleep(1000);
            await _missionRunService.Create(missionRunTwo);
            Thread.Sleep(1000);

            // Assert
            var postTestMissionRunOne = await _missionRunService.ReadById(missionRunOne.Id, noTracking: true);
            var postTestMissionRunTwo = await _missionRunService.ReadById(missionRunTwo.Id, noTracking: true);
            Assert.Equal(MissionStatus.Ongoing, postTestMissionRunOne!.Status);
            Assert.Equal(MissionStatus.Pending, postTestMissionRunTwo!.Status);
        }

        [Fact]
        public async void NewMissionIsStartedWhenRobotBecomesAvailable()
        {
            // Arrange
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Busy, installation, area);
            var missionRun = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area, false);

            await _missionRunService.Create(missionRun);
            Thread.Sleep(1000);

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
            Thread.Sleep(1000);

            // Assert
            var postTestMissionRun = await _missionRunService.ReadById(missionRun.Id, noTracking: true);
            Assert.Equal(MissionStatus.Ongoing, postTestMissionRun!.Status);
        }

        [Fact]
        public async void NoMissionIsStartedIfQueueIsEmptyWhenRobotBecomesAvailable()
        {
            // Arrange
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Busy, installation);

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
            Thread.Sleep(1000);

            // Assert
            var ongoingMission = await _missionRunService.ReadAll(
                new MissionRunQueryStringParameters
                {
                    Statuses = [
                        MissionStatus.Ongoing
                    ],
                    OrderBy = "DesiredStartTime",
                    PageSize = 100
                });
            Assert.False(ongoingMission.Any());
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
            var missionRunOne = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robotOne, area, false);
            var missionRunTwo = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robotTwo, area, false);

            // Act (Ensure first mission is started)
            await _missionRunService.Create(missionRunOne);
            Thread.Sleep(1000);

            // Assert
            var postStartMissionRunOne = await _missionRunService.ReadById(missionRunOne.Id, noTracking: true);
            Assert.NotNull(postStartMissionRunOne);
            Assert.Equal(MissionStatus.Ongoing, postStartMissionRunOne.Status);

            // Act (Ensure second mission is started for second robot)
            await _missionRunService.Create(missionRunTwo);
            Thread.Sleep(1000);

            // Assert
            var postStartMissionRunTwo = await _missionRunService.ReadById(missionRunTwo.Id, noTracking: true);
            Assert.NotNull(postStartMissionRunTwo);
            Assert.Equal(MissionStatus.Ongoing, postStartMissionRunTwo.Status);
        }

        [Fact]
        public async void LocalizationMissionStartedWhenNewMissionScheduledForNonLocalizedRobot()
        {
            // Arrange
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation);
            var missionRun = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area, false);

            // Act
            await _missionRunService.Create(missionRun);
            Thread.Sleep(1000);

            // Assert
            var ongoingMissionRun = await _missionRunService.GetOngoingMissionRunForRobot(robot.Id, noTracking: true);
            var postTestMissionRun = await _missionRunService.ReadById(missionRun.Id, noTracking: true);
            Assert.Equal(MissionStatus.Ongoing, ongoingMissionRun!.Status);
            Assert.Equal(MissionStatus.Pending, postTestMissionRun!.Status);
        }

        [Fact]
        public async void MissionIsCancelledWhenAttemptingToStartOnARobotWhichIsLocalizedOnADifferentDeck()
        {
            // Arrange
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck1 = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode, name: "TestDeckOne");
            var deck2 = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode, name: "TestDeckTwo");
            var area1 = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck1.Name, name: "TestAreaOne");
            var area2 = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck2.Name, name: "TestAreaTwo");
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area1);
            var missionRun = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area2, false);

            // Act
            await _missionRunService.Create(missionRun);
            Thread.Sleep(100);

            // Assert
            var postTestMissionRun = await _missionRunService.ReadById(missionRun.Id, noTracking: true);
            Assert.Equal(MissionStatus.Cancelled, postTestMissionRun!.Status);
        }

        [Fact]
        public async void RobotQueueIsFrozenAndOngoingMissionsMovedToPendingWhenPressingTheEmergencyButton()
        {
            // Arrange
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);
            var missionRun = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area, false);

            await _missionRunService.Create(missionRun);
            Thread.Sleep(1000);

            // Act
            await _emergencyActionController.AbortCurrentMissionAndSendAllRobotsToSafeZone(installation.InstallationCode);
            Thread.Sleep(1000);

            // Assert
            var ongoingMissionRun = await _missionRunService.GetOngoingMissionRunForRobot(robot.Id, noTracking: true);
            var postTestMissionRun = await _missionRunService.ReadById(missionRun.Id, noTracking: true);
            //var postTestRobot = await _robotService.ReadById(robot.Id, noTracking: true);


            var postTestRobot = await _context.Robots.AsNoTracking()
                .Include(r => r.VideoStreams)
                .Include(r => r.Model)
                .Include(r => r.CurrentInstallation)
                .Include(r => r.CurrentArea)
                .ThenInclude(area => area != null ? area.Deck : null)
                .Include(r => r.CurrentArea)
                .ThenInclude(area => area != null ? area.Plant : null)
                .Include(r => r.CurrentArea)
                .ThenInclude(area => area != null ? area.Installation : null)
                .Include(r => r.CurrentArea)
                .ThenInclude(area => area != null ? area.SafePositions : null)
                .Include(r => r.CurrentArea)
                .ThenInclude(area => area != null ? area.Deck : null)
                .ThenInclude(deck => deck != null ? deck.DefaultLocalizationPose : null)
                .ThenInclude(defaultLocalizationPose => defaultLocalizationPose != null ? defaultLocalizationPose.Pose : null).FirstOrDefaultAsync(robot => robot.Id.Equals(robot.Id));

            Assert.True(postTestRobot!.MissionQueueFrozen);
            Assert.Equal(MissionRunPriority.Emergency, ongoingMissionRun!.MissionRunPriority);
            Assert.Equal(MissionStatus.Pending, postTestMissionRun!.Status);
        }
    }
}
