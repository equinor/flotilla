using System;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Database.Context;
using Api.Database.Models;
using Api.Mqtt;
using Api.Mqtt.MessageModels;
using Api.Services;
using Api.Services.Events;
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

        public async ValueTask InitializeAsync()
        {
            (Container, ConnectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            Factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: ConnectionString
            );
            ServiceProvider = TestSetupHelpers.ConfigureServiceProvider(Factory);
            Context = TestSetupHelpers.ConfigurePostgreSqlContext(ConnectionString);

            DatabaseUtilities = new DatabaseUtilities(Context);
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
    }
}
