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

            latestRobot = await RobotService.ReadById(robot.Id);
            DateTime startTime = DateTime.UtcNow;
            while (DateTime.UtcNow < startTime.AddSeconds(5))
            {
                latestRobot = await RobotService.ReadById(robot.Id);
                if (latestRobot!.Status == RobotStatus.Available)
                    break;
                var cts = new CancellationTokenSource();
                await Task.Delay(1000, cts.Token);
            }
            Assert.Equal(RobotStatus.Available, latestRobot!.Status);
        }
    }
}
