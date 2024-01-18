using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Api.Controllers;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Mqtt;
using Api.Mqtt.Events;
using Api.Mqtt.MessageModels;
using Api.Services;
using Api.Services.Events;
using Api.Test.Database;
using Api.Test.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Api.Test.EventHandlers
{
    [Collection("Database collection")]
    public class TestMissionEventHandler : IAsyncLifetime
    {
        private readonly IMissionRunService _missionRunService;
        private readonly MqttService _mqttService;
        private readonly IRobotService _robotService;
        private readonly ILocalizationService _localizationService;
        private readonly IEmergencyActionService _emergencyActionService;
        private readonly DatabaseUtilities _databaseUtilities;
        private readonly EmergencyActionController _emergencyActionController;

        private readonly Func<Task> _resetDatabase;

        public TestMissionEventHandler(DatabaseFixture fixture)
        {
            var factory = fixture.Factory;
            var serviceProvider = fixture.ServiceProvider;

            _missionRunService = serviceProvider.GetRequiredService<IMissionRunService>();
            _robotService = serviceProvider.GetRequiredService<IRobotService>();
            _localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            _emergencyActionService = serviceProvider.GetRequiredService<IEmergencyActionService>();

            _emergencyActionController = serviceProvider.GetRequiredService<EmergencyActionController>();

            _databaseUtilities = new DatabaseUtilities(fixture.Context);
            _resetDatabase = fixture.ResetDatabase;

            var mqttServiceLogger = new Mock<ILogger<MqttService>>().Object;
            _mqttService = new MqttService(mqttServiceLogger, factory.Configuration!);
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync() { await _resetDatabase(); }

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
            var missionRunOne = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area);
            var missionRunTwo = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area);

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
            var missionRun = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area);

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

        [Fact(Skip = "Test")]
        public async void NoMissionIsStartedIfQueueIsEmptyWhenRobotBecomesAvailable()
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
            var missionRunOne = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robotOne, area);
            var missionRunTwo = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robotTwo, area);

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
        public async void QueuedMissionsAreAbortedWhenLocalizationFails()
        {
            // Arrange
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);
            var localizationMissionRun = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area, true, MissionRunPriority.Localization, MissionStatus.Ongoing, Guid.NewGuid().ToString());
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
            await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area, true, MissionRunPriority.Localization, MissionStatus.Ongoing, Guid.NewGuid().ToString());

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

#pragma warning disable xUnit1004
        [Fact(Skip = "Awaiting fix to use of execute update in tests")]
#pragma warning restore xUnit1004
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

#pragma warning disable xUnit1004
        [Fact(Skip = "Skipping as there is as issue with the context not reading the updated value of frozen queue")]
#pragma warning restore xUnit1004
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
            Thread.Sleep(10000);

            // Assert
            var ongoingMissionRun = await _missionRunService.GetOngoingMissionRunForRobot(robot.Id, noTracking: true);
            var postTestMissionRun = await _missionRunService.ReadById(missionRun.Id, noTracking: true);
            var postTestRobot = await _robotService.ReadById(robot.Id, noTracking: true);

            Assert.True(postTestRobot!.MissionQueueFrozen);
            Assert.Equal(MissionRunPriority.Emergency, ongoingMissionRun!.MissionRunPriority);
            Assert.Equal(MissionStatus.Pending, postTestMissionRun!.Status);
        }
    }
}
