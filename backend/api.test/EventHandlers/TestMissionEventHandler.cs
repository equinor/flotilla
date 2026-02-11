using System;
using System.Threading;
using System.Threading.Tasks;
using Api.Database.Context;
using Api.Database.Models;
using Api.Mqtt;
using Api.Mqtt.MessageModels;
using Api.Services;
using Api.Services.Events;
using Api.Test.Database;
using Api.Test.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.EventHandlers
{
    public class TestMissionEventHandler : IAsyncLifetime, IAsyncDisposable
    {
        private FlotillaDbContext Context => CreateContext();
        public required TestWebApplicationFactory<Program> Factory;
        public required IServiceProvider ServiceProvider;

        public required PostgreSqlContainer Container;
        public required string ConnectionString;

        public required DatabaseUtilities DatabaseUtilities;

        public required IMissionRunService MissionRunService;
        public required IRobotService RobotService;
        public required IMissionSchedulingService MissionSchedulingService;

        public required MqttService MqttService;
        public required EventAggregatorSingletonService EventAggregatorSingletonService;
        public required MockIsarService IsarService;

        public async ValueTask InitializeAsync()
        {
            (Container, ConnectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            Factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: ConnectionString
            );

            ServiceProvider = TestSetupHelpers.ConfigureServiceProvider(Factory);

            DatabaseUtilities = new DatabaseUtilities(Context);
            MissionRunService = ServiceProvider.GetRequiredService<IMissionRunService>();
            RobotService = ServiceProvider.GetRequiredService<IRobotService>();
            MissionSchedulingService =
                ServiceProvider.GetRequiredService<IMissionSchedulingService>();
            EventAggregatorSingletonService =
                ServiceProvider.GetRequiredService<EventAggregatorSingletonService>();
            IsarService = (MockIsarService)ServiceProvider.GetRequiredService<IIsarService>();

            var mqttServiceLogger = new Mock<ILogger<MqttService>>().Object;
            MqttService = new MqttService(
                mqttServiceLogger,
                Factory.Configuration!,
                EventAggregatorSingletonService
            );
        }

        public static async ValueTask DisposeAsync()
        {
            await Task.CompletedTask;
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            await Task.CompletedTask;
            GC.SuppressFinalize(this);
        }

        private FlotillaDbContext CreateContext()
        {
            return TestSetupHelpers.ConfigurePostgreSqlContext(ConnectionString);
        }

        [Fact]
        public async Task ScheduledMissionStartedWhenSystemIsAvailable()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var robot = await DatabaseUtilities.NewRobot(
                RobotStatus.Available,
                installation,
                inspectionArea.Id
            );
            var missionRun = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea
            );

            // Act
            await MissionRunService.Create(missionRun);
            await MissionSchedulingService.StartNextMissionRunIfSystemIsAvailable(robot);
            Thread.Sleep(1000);

            // Assert
            Assert.True(IsarService.isStartCalled);
            Assert.True(IsarService.isStarted);
        }

        [Fact]
        public async Task SecondScheduledMissionQueuedIfRobotIsBusy()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var robot = await DatabaseUtilities.NewRobot(
                RobotStatus.Busy,
                installation,
                inspectionArea.Id
            );
            var missionRunOne = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea,
                missionStatus: MissionStatus.Ongoing
            );
            await MissionRunService.Create(missionRunOne);
            await MissionSchedulingService.StartNextMissionRunIfSystemIsAvailable(robot);

            // Act
            var missionRunTwo = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea
            );
            await MissionRunService.Create(missionRunTwo);
            await MissionSchedulingService.StartNextMissionRunIfSystemIsAvailable(robot);

            // Assert
            await TestSetupHelpers.WaitFor(async () =>
            {
                return IsarService.isStartCalled;
            });
            Thread.Sleep(100);
            Assert.False(IsarService.isStarted);
            var postTestMissionRunTwo = await MissionRunService.ReadById(
                missionRunTwo.Id,
                readOnly: true
            );
            Assert.Equal(MissionStatus.Queued, postTestMissionRunTwo!.Status);
        }

        [Fact]
        public async Task NewMissionIsStartedWhenRobotBecomesAvailable()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var robot = await DatabaseUtilities.NewRobot(
                RobotStatus.Busy,
                installation,
                inspectionArea.Id
            );
            var missionRun = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea
            );

            await MissionRunService.Create(missionRun);
            await MissionSchedulingService.StartNextMissionRunIfSystemIsAvailable(robot);
            Thread.Sleep(100);

            var isarStatusMessage = new IsarStatusMessage
            {
                RobotName = robot.Name,
                IsarId = robot.IsarId,
                Status = RobotStatus.Available,
                Timestamp = DateTime.UtcNow,
            };

            // Act
            EventAggregatorSingletonService.Publish(isarStatusMessage);
            Thread.Sleep(5000); // When running all tests in VS code the test occasionally fails when this sleep too short

            // Assert
            var postTestMissionRun = await MissionRunService.ReadById(
                missionRun.Id,
                readOnly: true
            );
            // The mission should be queued until we get a status on MQTT
            Assert.Equal(MissionStatus.Queued, postTestMissionRun!.Status);
        }

        [Fact]
        public async Task MissionRunIsStartedForOtherAvailableRobotIfOneRobotHasAnOngoingMissionRun()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var robotOne = await DatabaseUtilities.NewRobot(
                RobotStatus.Available,
                installation,
                inspectionArea.Id
            );
            var robotTwo = await DatabaseUtilities.NewRobot(
                RobotStatus.Available,
                installation,
                inspectionArea.Id
            );
            var missionRunOne = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robotOne,
                inspectionArea
            );
            var missionRunTwo = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robotTwo,
                inspectionArea
            );

            // Act (Ensure first mission is started)
            await MissionRunService.Create(missionRunOne);
            await MissionSchedulingService.StartNextMissionRunIfSystemIsAvailable(
                missionRunOne.Robot
            );
            Thread.Sleep(1000);

            // Assert
            var postStartMissionRunOne = await MissionRunService.ReadById(
                missionRunOne.Id,
                readOnly: true
            );
            Assert.NotNull(postStartMissionRunOne);
            // Status is queued until we get a new status on MQTT
            Assert.Equal(MissionStatus.Queued, postStartMissionRunOne.Status);

            // Act (Ensure second mission is started for second robot)
            await MissionRunService.Create(missionRunTwo);
            await MissionSchedulingService.StartNextMissionRunIfSystemIsAvailable(
                missionRunTwo.Robot
            );
            Thread.Sleep(1000);

            // Assert
            var postStartMissionRunTwo = await MissionRunService.ReadById(
                missionRunTwo.Id,
                readOnly: true
            );
            Assert.NotNull(postStartMissionRunTwo);
            // Status is queued until we get a new status on MQTT
            Assert.Equal(MissionStatus.Queued, postStartMissionRunTwo.Status);
        }

        [Fact]
        public async Task QueuedMissionsAreNotAbortedWhenRobotReadyForMissionsHappensAtTheSameTimeAsOnIsarMissionCompleted()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);
            var missionRunOne = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea,
                true
            );
            var missionRunTwo = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea,
                true
            );

            await MissionSchedulingService.StartNextMissionRunIfSystemIsAvailable(robot);
            Thread.Sleep(1000);

            var missionRunOnePostCreation = await MissionRunService.ReadById(
                missionRunOne.Id,
                readOnly: true
            );
            Assert.NotNull(missionRunOnePostCreation);

            // Act
            var isarMissionMessage = new IsarMissionMessage
            {
                RobotName = robot.Name,
                IsarId = robot.IsarId,
                MissionId = missionRunOnePostCreation.Id,
                Status = "successful",
                Timestamp = DateTime.UtcNow,
            };

            EventAggregatorSingletonService.Publish(isarMissionMessage);
            EventAggregatorSingletonService.Publish(new RobotReadyForMissionsEventArgs(robot));

            Thread.Sleep(100);

            // Assert
            await TestSetupHelpers.WaitFor(async () =>
            {
                var postTestMissionRunOne = await MissionRunService.ReadById(
                    missionRunOne.Id,
                    readOnly: true
                );
                return postTestMissionRunOne!.Status == MissionStatus.Successful;
            });

            await TestSetupHelpers.WaitFor(async () =>
            {
                var postTestMissionRunTwo = await MissionRunService.ReadById(
                    missionRunTwo.Id,
                    readOnly: true
                );
                return postTestMissionRunTwo!.Status == MissionStatus.Queued;
            });
        }

        [Fact]
        public async Task IsarStatusTriggersNextMissionEvenIfOtherMissionIsOngoing()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var robot = await DatabaseUtilities.NewRobot(
                RobotStatus.Busy,
                installation,
                inspectionArea.Id
            );
            var missionRun1 = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea,
                writeToDatabase: true,
                missionStatus: MissionStatus.Ongoing
            );
            var missionRun2 = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea,
                writeToDatabase: true
            );
            Assert.False(IsarService.isStartCalled);
            Assert.False(IsarService.isStarted);

            // Act
            var isarStatusMessage = new IsarStatusMessage
            {
                RobotName = robot.Name,
                IsarId = robot.IsarId,
                Status = RobotStatus.Available,
                Timestamp = DateTime.UtcNow,
            };
            EventAggregatorSingletonService.Publish(isarStatusMessage);
            // EventAggregatorSingletonService.Publish(new RobotReadyForMissionsEventArgs(robot)); // Should be called by isarStatusMessage

            // Assert
            await TestSetupHelpers.WaitFor(async () =>
            {
                return IsarService.isStarted;
            });
            Assert.True(IsarService.isStartCalled);
            Assert.True(IsarService.isStarted);
        }

        [Fact]
        public async Task MissionStatusUpdate()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var robot = await DatabaseUtilities.NewRobot(
                RobotStatus.Busy,
                installation,
                inspectionArea.Id
            );
            var missionRun = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea,
                writeToDatabase: true,
                missionStatus: MissionStatus.Ongoing
            );

            // Act
            var isarMissionMessage = new IsarMissionMessage
            {
                RobotName = robot.Name,
                IsarId = robot.IsarId,
                MissionId = missionRun.Id,
                Status = "successful",
                Timestamp = DateTime.UtcNow,
            };
            EventAggregatorSingletonService.Publish(isarMissionMessage);

            // Assert
            await TestSetupHelpers.WaitFor(async () =>
            {
                var postTestMissionRun = await MissionRunService.ReadById(
                    missionRun.Id,
                    readOnly: true
                );
                return postTestMissionRun!.Status == MissionStatus.Successful;
            });
        }
    }
}
