﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Mqtt;
using Api.Mqtt.Events;
using Api.Mqtt.MessageModels;
using Api.Services;
using Api.Services.Events;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Api.Test.EventHandlers
{
    public class TestMissionEventHandler : IAsyncLifetime, IAsyncDisposable
    {
        private FlotillaDbContext Context => CreateContext();
        public required TestWebApplicationFactory<Program> Factory;
        public required IServiceProvider ServiceProvider;

        public required string ConnectionString;

        public required DatabaseUtilities DatabaseUtilities;

        public required IMissionRunService MissionRunService;
        public required IRobotService RobotService;
        public required IMissionSchedulingService MissionSchedulingService;

        public required MqttService MqttService;

        public async Task InitializeAsync()
        {
            string databaseName = Guid.NewGuid().ToString();
            (ConnectionString, _) = await TestSetupHelpers.ConfigureSqLiteDatabase(databaseName);

            DatabaseUtilities = new DatabaseUtilities(Context);

            Factory = TestSetupHelpers.ConfigureWebApplicationFactory(databaseName);
            ServiceProvider = TestSetupHelpers.ConfigureServiceProvider(Factory);

            MissionRunService = ServiceProvider.GetRequiredService<IMissionRunService>();
            RobotService = ServiceProvider.GetRequiredService<IRobotService>();
            MissionSchedulingService =
                ServiceProvider.GetRequiredService<IMissionSchedulingService>();

            var mqttServiceLogger = new Mock<ILogger<MqttService>>().Object;
            MqttService = new MqttService(mqttServiceLogger, Factory.Configuration!);
        }

        public async Task DisposeAsync()
        {
            await Context.Database.EnsureDeletedAsync();
            await Context.Database.EnsureCreatedAsync();
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            await Task.CompletedTask;
        }

        private FlotillaDbContext CreateContext()
        {
            return TestSetupHelpers.ConfigureSqLiteContext(ConnectionString);
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
                inspectionArea
            );
            var missionRun = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea
            );

            // Act
            await MissionRunService.Create(missionRun);
            Thread.Sleep(100);

            // Assert
            var postTestMissionRun = await MissionRunService.ReadById(
                missionRun.Id,
                readOnly: true
            );
            Assert.Equal(MissionStatus.Ongoing, postTestMissionRun!.Status);
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
                RobotStatus.Available,
                installation,
                inspectionArea
            );
            var missionRunOne = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea
            );
            var missionRunTwo = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea
            );

            // Act
            await MissionRunService.Create(missionRunOne);
            Thread.Sleep(100);
            await MissionRunService.Create(missionRunTwo);

            // Assert
            var postTestMissionRunOne = await MissionRunService.ReadById(
                missionRunOne.Id,
                readOnly: true
            );
            var postTestMissionRunTwo = await MissionRunService.ReadById(
                missionRunTwo.Id,
                readOnly: true
            );
            Assert.Equal(MissionStatus.Ongoing, postTestMissionRunOne!.Status);
            Assert.Equal(MissionStatus.Pending, postTestMissionRunTwo!.Status);
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
                inspectionArea
            );
            var missionRun = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea
            );

            await MissionRunService.Create(missionRun);
            Thread.Sleep(100);

            var mqttEventArgs = new MqttReceivedArgs(
                new IsarStatusMessage
                {
                    RobotName = robot.Name,
                    IsarId = robot.IsarId,
                    Status = RobotStatus.Available,
                    Timestamp = DateTime.UtcNow,
                }
            );

            // Act
            MqttService.RaiseEvent(nameof(MqttService.MqttIsarStatusReceived), mqttEventArgs);
            Thread.Sleep(500);

            // Assert
            var postTestMissionRun = await MissionRunService.ReadById(
                missionRun.Id,
                readOnly: true
            );
            Assert.Equal(MissionStatus.Ongoing, postTestMissionRun!.Status);
        }

        [Fact]
        public async Task ReturnToHomeMissionIsNotStartedIfReturnToHomeIsNotSupported()
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
                inspectionArea
            );
            robot.RobotCapabilities!.Remove(RobotCapabilitiesEnum.return_to_home);
            await RobotService.Update(robot);

            var mqttEventArgs = new MqttReceivedArgs(
                new IsarStatusMessage
                {
                    RobotName = robot.Name,
                    IsarId = robot.IsarId,
                    Status = RobotStatus.Available,
                    Timestamp = DateTime.UtcNow,
                }
            );

            // Act
            MqttService.RaiseEvent(nameof(MqttService.MqttIsarStatusReceived), mqttEventArgs);

            // Assert
            Thread.Sleep(1000);
            var ongoingMission = await MissionRunService.ReadAll(
                new MissionRunQueryStringParameters
                {
                    Statuses = [MissionStatus.Ongoing],
                    OrderBy = "DesiredStartTime",
                    PageSize = 100,
                },
                readOnly: true
            );
            Assert.False(ongoingMission.Any());
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
                inspectionArea
            );
            var robotTwo = await DatabaseUtilities.NewRobot(
                RobotStatus.Available,
                installation,
                inspectionArea
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
            Thread.Sleep(100);

            // Assert
            var postStartMissionRunOne = await MissionRunService.ReadById(
                missionRunOne.Id,
                readOnly: true
            );
            Assert.NotNull(postStartMissionRunOne);
            Assert.Equal(MissionStatus.Ongoing, postStartMissionRunOne.Status);

            // Act (Ensure second mission is started for second robot)
            await MissionRunService.Create(missionRunTwo);
            Thread.Sleep(100);

            // Assert
            var postStartMissionRunTwo = await MissionRunService.ReadById(
                missionRunTwo.Id,
                readOnly: true
            );
            Assert.NotNull(postStartMissionRunTwo);
            Assert.Equal(MissionStatus.Ongoing, postStartMissionRunTwo.Status);
        }

        [Fact]
        public async Task QueuedMissionsAreNotAbortedWhenRobotAvailableHappensAtTheSameTimeAsOnIsarMissionCompleted()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation, null);
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

            var missionRunCreatedEventArgs = new MissionRunCreatedEventArgs(missionRunOne);
            MissionRunService.RaiseEvent(
                nameof(Api.Services.MissionRunService.MissionRunCreated),
                missionRunCreatedEventArgs
            );
            Thread.Sleep(1000);

            var missionRunOnePostCreation = await MissionRunService.ReadById(
                missionRunOne.Id,
                readOnly: true
            );
            Assert.NotNull(missionRunOnePostCreation);

            // Act
            var mqttIsarMissionEventArgs = new MqttReceivedArgs(
                new IsarMissionMessage
                {
                    RobotName = robot.Name,
                    IsarId = robot.IsarId,
                    MissionId = missionRunOnePostCreation.IsarMissionId,
                    Status = "successful",
                    Timestamp = DateTime.UtcNow,
                }
            );

            var robotAvailableEventArgs = new RobotAvailableEventArgs(robot);

            MqttService.RaiseEvent(
                nameof(MqttService.MqttIsarMissionReceived),
                mqttIsarMissionEventArgs
            );
            MissionSchedulingService.RaiseEvent(
                nameof(Api.Services.MissionSchedulingService.RobotAvailable),
                robotAvailableEventArgs
            );

            // Assert
            var postTestMissionRunOne = await MissionRunService.ReadById(
                missionRunOne.Id,
                readOnly: true
            );
            Assert.Equal(MissionStatus.Successful, postTestMissionRunOne!.Status);
            var postTestMissionRunTwo = await MissionRunService.ReadById(
                missionRunTwo.Id,
                readOnly: true
            );
            Assert.Equal(MissionStatus.Pending, postTestMissionRunTwo!.Status);
        }

#pragma warning disable xUnit1004
        [Fact(Skip = "Skipping until issue #1767 is solved because test is unreliable")]
#pragma warning restore xUnit1004
        public async Task QueuedContinuesWhenOnIsarStatusHappensAtTheSameTimeAsOnIsarMissionCompleted()
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
                inspectionArea
            );
            var missionRun1 = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea,
                true,
                MissionRunType.Normal,
                MissionStatus.Ongoing,
                Guid.NewGuid().ToString()
            );
            var missionRun2 = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea,
                true
            );
            Thread.Sleep(100);

            var missionRunCreatedEventArgs = new MissionRunCreatedEventArgs(missionRun1);
            MissionRunService.RaiseEvent(
                nameof(Api.Services.MissionRunService.MissionRunCreated),
                missionRunCreatedEventArgs
            );
            Thread.Sleep(100);

            // Act
            var mqttIsarMissionEventArgs = new MqttReceivedArgs(
                new IsarMissionMessage
                {
                    RobotName = robot.Name,
                    IsarId = robot.IsarId,
                    MissionId = missionRun1.IsarMissionId,
                    Status = "successful",
                    Timestamp = DateTime.UtcNow,
                }
            );

            var mqttIsarStatusEventArgs = new MqttReceivedArgs(
                new IsarStatusMessage
                {
                    RobotName = robot.Name,
                    IsarId = robot.IsarId,
                    Status = RobotStatus.Available,
                    Timestamp = DateTime.UtcNow,
                }
            );

            MqttService.RaiseEvent(
                nameof(MqttService.MqttIsarMissionReceived),
                mqttIsarMissionEventArgs
            );
            MqttService.RaiseEvent(
                nameof(MqttService.MqttIsarStatusReceived),
                mqttIsarStatusEventArgs
            );
            Thread.Sleep(2500); // Accommodate for sleep in OnIsarStatus

            // Assert
            var postTestMissionRun1 = await MissionRunService.ReadById(
                missionRun1.Id,
                readOnly: true
            );
            Assert.Equal(
                Api.Database.Models.TaskStatus.Successful,
                postTestMissionRun1!.Tasks[0].Status
            );
            var postTestMissionRun2 = await MissionRunService.ReadById(
                missionRun2.Id,
                readOnly: true
            );
            Assert.Equal(MissionStatus.Ongoing, postTestMissionRun2!.Status);
        }

        [Fact]
        public async Task ReturnHomeMissionAbortedIfNewMissionScheduled()
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
                inspectionArea
            );
            var returnToHomeMission = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea,
                true,
                MissionRunType.ReturnHome,
                MissionStatus.Ongoing,
                Guid.NewGuid().ToString()
            );
            var missionRun = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea,
                true,
                MissionRunType.Normal,
                MissionStatus.Pending,
                Guid.NewGuid().ToString()
            );

            Thread.Sleep(100);

            // Act
            var eventArgs = new MissionRunCreatedEventArgs(missionRun);
            MissionRunService.RaiseEvent(
                nameof(Api.Services.MissionRunService.MissionRunCreated),
                eventArgs
            );
            Thread.Sleep(500);

            // Assert
            var updatedReturnHomeMission = await MissionRunService.ReadById(
                returnToHomeMission.Id,
                readOnly: true
            );
            Assert.True(updatedReturnHomeMission?.Status.Equals(MissionStatus.Aborted));

            // Act
            var mqttIsarMissionEventArgs = new MqttReceivedArgs(
                new IsarMissionMessage
                {
                    RobotName = robot.Name,
                    IsarId = robot.IsarId,
                    MissionId = returnToHomeMission.IsarMissionId,
                    Status = "cancelled",
                    Timestamp = DateTime.UtcNow,
                }
            );

            var mqttIsarStatusEventArgs = new MqttReceivedArgs(
                new IsarStatusMessage
                {
                    RobotName = robot.Name,
                    IsarId = robot.IsarId,
                    Status = RobotStatus.Available,
                    Timestamp = DateTime.UtcNow,
                }
            );

            MqttService.RaiseEvent(
                nameof(MqttService.MqttIsarMissionReceived),
                mqttIsarMissionEventArgs
            );
            MqttService.RaiseEvent(
                nameof(MqttService.MqttIsarStatusReceived),
                mqttIsarStatusEventArgs
            );
            Thread.Sleep(500);

            var updatedMissionRun = await MissionRunService.ReadById(missionRun.Id, readOnly: true);
            Assert.True(updatedMissionRun?.Status.Equals(MissionStatus.Ongoing));
        }
    }
}
