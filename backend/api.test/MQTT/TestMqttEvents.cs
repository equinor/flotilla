using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Api.Database.Context;
using Api.Database.Models;
using Api.Mqtt;
using Api.Mqtt.MessageModels;
using Api.Services;
using Api.Services.Events;
using Api.Services.Models;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.MQTT
{
    public class TestMqttEvents : IAsyncLifetime, IAsyncDisposable
    {
        public required FlotillaDbContext Context;
        public required PostgreSqlContainer Container;
        public required string ConnectionString;
        public required TestWebApplicationFactory<Program> Factory;
        public required IServiceProvider ServiceProvider;
        public required EventAggregatorSingletonService EventAggregatorSingletonService;
        public required MqttService MqttService;
        public required DatabaseUtilities DatabaseUtilities;
        public required IRobotService RobotService;
        public required IMissionRunService MissionRunService;
        public required IInspectionService InspectionService;

        public async ValueTask InitializeAsync()
        {
            (Container, ConnectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            Factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: ConnectionString
            );
            ServiceProvider = TestSetupHelpers.ConfigureServiceProvider(Factory);
            Context = TestSetupHelpers.ConfigurePostgreSqlContext(ConnectionString);

            DatabaseUtilities = ServiceProvider.GetRequiredService<DatabaseUtilities>();
            var mqttServiceLogger = new Mock<ILogger<MqttService>>().Object;
            EventAggregatorSingletonService =
                ServiceProvider.GetRequiredService<EventAggregatorSingletonService>();
            MqttService = new MqttService(
                mqttServiceLogger,
                Factory.Configuration!,
                EventAggregatorSingletonService
            );
            RobotService = ServiceProvider.GetRequiredService<IRobotService>();
            MissionRunService = ServiceProvider.GetRequiredService<IMissionRunService>();
            InspectionService = ServiceProvider.GetRequiredService<IInspectionService>();
        }

        public async ValueTask DisposeAsync()
        {
            await Task.CompletedTask;
        }

        [Fact]
        public async Task TestMQTTUpdatesIsarStatus()
        {
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
            IsarStatusMessage message = new()
            {
                RobotName = robot.Name,
                IsarId = robot.IsarId,
                Status = RobotStatus.Available,
                Timestamp = DateTime.UtcNow,
            };
            var messageString = JsonSerializer.Serialize(message);
            var latestRobot = await RobotService.ReadById(robot.Id);
            Assert.Equal(RobotStatus.Busy, latestRobot!.Status);

            await MqttService.PublishMessageBasedOnTopic($"isar/{robot.Id}/status", messageString);

            await TestSetupHelpers.WaitFor(async () =>
            {
                latestRobot = await RobotService.ReadById(robot.Id);
                return latestRobot!.Status == RobotStatus.Available;
            });
        }

        [Fact]
        public async Task TestMQTTUpdateRobotInfo()
        {
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var robot = new Robot { Name = "TestRobot", IsarId = Guid.NewGuid().ToString() };
            var latestRobot = await RobotService.ReadById(robot.Id);
            Assert.Null(latestRobot);

            IsarRobotInfoMessage message = new()
            {
                RobotName = robot.Name,
                IsarId = robot.IsarId,
                Timestamp = DateTime.UtcNow,
                CurrentInstallation = installation.InstallationCode,
                DocumentationQueries = [],
                SerialNumber = robot.SerialNumber,
                Host = robot.Host,
                Port = robot.Port,
            };
            var messageString = JsonSerializer.Serialize(message);
            await MqttService.PublishMessageBasedOnTopic(
                $"isar/{robot.Id}/robot_info",
                messageString
            );

            await TestSetupHelpers.WaitFor(async () =>
            {
                latestRobot = await RobotService.ReadByIsarId(robot.IsarId);
                return latestRobot != null;
            });
        }

        [Fact]
        public async Task TestMQTTMissionAborted()
        {
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

            var message = new IsarMissionAbortedMessage
            {
                RobotName = robot.Name,
                IsarId = robot.IsarId,
                MissionId = missionRun.Id,
                Timestamp = DateTime.UtcNow,
            };
            var messageString = JsonSerializer.Serialize(message);
            await MqttService.PublishMessageBasedOnTopic(
                $"isar/{robot.Id}/aborted_mission",
                messageString
            );

            await TestSetupHelpers.WaitFor(async () =>
            {
                var postTestMissionRun = await MissionRunService.ReadById(
                    missionRun.Id,
                    readOnly: true
                );
                return postTestMissionRun!.Status == MissionStatus.Queued;
            });
        }

        [Fact]
        public async Task TestMQTTMissionStatus()
        {
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

            var message = new IsarMissionMessage
            {
                RobotName = robot.Name,
                IsarId = robot.IsarId,
                MissionId = missionRun.Id,
                Timestamp = DateTime.UtcNow,
                Status = "successful",
            };
            var messageString = JsonSerializer.Serialize(message);
            await MqttService.PublishMessageBasedOnTopic(
                $"isar/{robot.Id}/mission/{missionRun.Id}",
                messageString
            );

            await TestSetupHelpers.WaitFor(async () =>
            {
                var postTestMissionRun = await MissionRunService.ReadById(
                    missionRun.Id,
                    readOnly: true
                );
                return postTestMissionRun!.Status == MissionStatus.Successful;
            });
        }

        [Fact]
        public async Task TestMQTTTask()
        {
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
            MissionTask task = new MissionTask { RobotPose = new Pose { } };
            var missionRun = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea,
                writeToDatabase: true,
                missionStatus: MissionStatus.Ongoing,
                tasks: new MissionTask[] { task }
            );
            task = (await MissionRunService.ReadById(missionRun.Id))!.Tasks[0];
            var message = new IsarTaskMessage
            {
                RobotName = robot.Name,
                IsarId = robot.IsarId,
                MissionId = missionRun.Id,
                Timestamp = DateTime.UtcNow,
                Status = "successful",
                TaskId = task.Id,
                TaskType = "take_image",
            };
            var messageString = JsonSerializer.Serialize(message);
            await MqttService.PublishMessageBasedOnTopic(
                $"isar/{robot.Id}/task/{task.Id}",
                messageString
            );

            await TestSetupHelpers.WaitFor(async () =>
            {
                var postTestMissionRun = await MissionRunService.ReadById(
                    missionRun.Id,
                    readOnly: true
                );
                return postTestMissionRun!.Tasks[0].Status
                    == Api.Database.Models.TaskStatus.Successful;
            });
        }

        [Fact]
        public async Task TestMQTTBattery()
        {
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

            Thread.Sleep(5);
            Assert.Single(Factory.MockSignalRService.LatestMessages);
            Assert.Equal(
                "InspectionArea created",
                (Factory.MockSignalRService.LatestMessages[0] as dynamic).Label
            );

            var message = new IsarBatteryMessage
            {
                RobotName = robot.Name,
                IsarId = robot.IsarId,
                Timestamp = DateTime.UtcNow,
                BatteryLevel = 0.5f,
                BatteryState = BatteryState.Charging,
            };
            var messageString = JsonSerializer.Serialize(message);
            await MqttService.PublishMessageBasedOnTopic($"isar/{robot.Id}/battery", messageString);

            await TestSetupHelpers.WaitFor(async () =>
            {
                var latestMessages = Factory.MockSignalRService.LatestMessages;
                if (latestMessages.Count != 3)
                    return false;
                var m1 = (dynamic)latestMessages[1];
                var m2 = (dynamic)latestMessages[2];
                if (m1.Label != "Robot telemetry updated" || m2.Label != "Robot telemetry updated")
                    return false;
                var telemetry1 = (UpdateRobotTelemetryMessage)m1.Message;
                var telemetry2 = (UpdateRobotTelemetryMessage)m2.Message;
                UpdateRobotTelemetryMessage batteryStateMessage;
                UpdateRobotTelemetryMessage batteryLevelMessage;
                if (telemetry1.TelemetryName == "batteryState")
                {
                    batteryStateMessage = telemetry1;
                    batteryLevelMessage = telemetry2;
                }
                else
                {
                    batteryStateMessage = telemetry2;
                    batteryLevelMessage = telemetry1;
                }

                return (float)batteryLevelMessage.TelemetryValue! == 0.5f
                    && (BatteryState)batteryStateMessage.TelemetryValue! == BatteryState.Charging;
            });
        }

        [Fact]
        public async Task TestMQTTPressure()
        {
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

            Thread.Sleep(5);
            Assert.Single(Factory.MockSignalRService.LatestMessages);
            Assert.Equal(
                "InspectionArea created",
                (Factory.MockSignalRService.LatestMessages[0] as dynamic).Label
            );

            const float PRESSURE_LEVEL = 20.1f;
            var message = new IsarPressureMessage
            {
                RobotName = robot.Name,
                IsarId = robot.IsarId,
                Timestamp = DateTime.UtcNow,
                PressureLevel = PRESSURE_LEVEL,
            };
            var messageString = JsonSerializer.Serialize(message);
            await MqttService.PublishMessageBasedOnTopic(
                $"isar/{robot.Id}/pressure",
                messageString
            );

            await TestSetupHelpers.WaitFor(async () =>
            {
                var latestMessages = Factory.MockSignalRService.LatestMessages;
                if (latestMessages.Count != 2)
                    return false;
                var m = (dynamic)latestMessages[1];
                if (m.Label != "Robot telemetry updated")
                    return false;
                var telemetry = (UpdateRobotTelemetryMessage)m.Message;

                return (float)telemetry.TelemetryValue! == PRESSURE_LEVEL;
            });
        }

        [Fact]
        public async Task TestMQTTPose()
        {
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

            Thread.Sleep(5);
            Assert.Single(Factory.MockSignalRService.LatestMessages);
            Assert.Equal(
                "InspectionArea created",
                (Factory.MockSignalRService.LatestMessages[0] as dynamic).Label
            );

            var frame = new IsarFrame { Name = "map" };
            var pose = new IsarPoseMqtt
            {
                Position = new Api.Mqtt.MessageModels.IsarPosition
                {
                    X = 1,
                    Y = 2,
                    Z = 3,
                    Frame = frame,
                },
                Orientation = new Api.Mqtt.MessageModels.IsarOrientation
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                    W = 1,
                    Frame = frame,
                },
                Frame = frame,
            };
            var message = new IsarPoseMessage
            {
                RobotName = robot.Name,
                IsarId = robot.IsarId,
                Timestamp = DateTime.UtcNow,
                Pose = pose,
            };
            var messageString = JsonSerializer.Serialize(message);
            await MqttService.PublishMessageBasedOnTopic($"isar/{robot.Id}/pose", messageString);

            await TestSetupHelpers.WaitFor(async () =>
            {
                var latestMessages = Factory.MockSignalRService.LatestMessages;
                if (latestMessages.Count != 2)
                    return false;
                var m = (dynamic)latestMessages[1];
                if (m.Label != "Robot telemetry updated")
                    return false;
                var telemetry = (UpdateRobotTelemetryMessage)m.Message;

                var readPose = (Pose)telemetry.TelemetryValue!;
                return readPose.Position.Z == 3;
            });
        }

        [Fact]
        public async Task TestMQTTStartup()
        {
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

            var message = new IsarStartupMessage
            {
                IsarId = robot.IsarId,
                InstallationCode = installation.InstallationCode,
                Timestamp = DateTime.UtcNow,
            };
            var messageString = JsonSerializer.Serialize(message);
            await MqttService.PublishMessageBasedOnTopic($"isar/{robot.Id}/startup", messageString);

            await TestSetupHelpers.WaitFor(async () =>
            {
                var postTestMissionRun = await MissionRunService.ReadById(
                    missionRun.Id,
                    readOnly: true
                );
                Robot postTestRobot = (await RobotService.ReadById(robot.Id))!;
                return postTestMissionRun!.Status == MissionStatus.Queued
                    && postTestRobot.CurrentMissionId == null;
            });
        }

        [Fact]
        public async Task TestMQTTSaraInspectionResult()
        {
            var installation = await DatabaseUtilities.NewInstallation();

            var message = new SaraInspectionResultMessage
            {
                InspectionId = Guid.NewGuid().ToString(),
                StorageAccount = "testaccount",
                BlobContainer = installation.InstallationCode,
                BlobName = "testblob",
            };
            var messageString = JsonSerializer.Serialize(message);
            await MqttService.PublishMessageBasedOnTopic(
                $"sara/visualization_available",
                messageString
            );

            await TestSetupHelpers.WaitFor(async () =>
            {
                var latestMessages = Factory.MockSignalRService.LatestMessages;
                if (latestMessages.Count != 1)
                    return false;
                var m = (dynamic)latestMessages[0];
                if (m.Label != "Inspection Visulization Ready")
                    return false;
                var result = (InspectionResultMessage)m.Message;

                return result.InspectionId == message.InspectionId;
            });
        }

        [Fact]
        public async Task TestMQTTSaraAnalysisResult()
        {
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
            MissionTask task = new MissionTask { RobotPose = new Pose { } };
            var missionRun = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea,
                writeToDatabase: true,
                missionStatus: MissionStatus.Ongoing,
                tasks: new MissionTask[] { task }
            );
            var isarInspectionId = missionRun.Tasks[0].Inspection!.IsarInspectionId;

            const string VALUE = "testvalue";
            var message = new SaraAnalysisResultMessage
            {
                InspectionId = isarInspectionId,
                AnalysisType = "test_analysis",
                StorageAccount = "testaccount",
                BlobContainer = installation.InstallationCode,
                BlobName = "testblob",
                Value = VALUE,
            };
            var messageString = JsonSerializer.Serialize(message);
            await MqttService.PublishMessageBasedOnTopic(
                $"sara/analysis_result_available",
                messageString
            );

            await TestSetupHelpers.WaitFor(async () =>
            {
                var inspection = await InspectionService.ReadByIsarInspectionId(
                    isarInspectionId,
                    readOnly: true
                );
                return inspection?.AnalysisResult?.Value == VALUE;
            });
        }
    }
}
