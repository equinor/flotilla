using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using Api.Database.Models;
using Api.EventHandlers;
using Api.Mqtt.MessageModels;
using Api.Services;
using Api.Services.Events;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Api.Test.Security
{
    public class MqttRobotRegistrationTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RobotInformationCannotRegisterOrRedirectRobots(bool registered)
        {
            var robot = new Robot
            {
                Id = "robot",
                IsarId = "isar",
                Host = "approved.example",
                Port = 3000,
                CurrentInstallation = new Installation { InstallationCode = "BBB" },
                RobotCapabilities = [],
            };
            var robots = new Mock<IRobotService>(MockBehavior.Strict);
            robots
                .Setup(s => s.ReadByIsarId(robot.IsarId, true))
                .ReturnsAsync(registered ? robot : null);
            if (registered)
                robots.Setup(s => s.Update(robot)).Returns(Task.CompletedTask);

            using var services = new ServiceCollection()
                .AddScoped<IRobotService>(_ => robots.Object)
                .BuildServiceProvider();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            using var meter = new Meter(nameof(MqttRobotRegistrationTests));
            var events = new EventAggregatorSingletonService();
            using var handler = new MqttEventHandler(
                NullLogger<MqttEventHandler>.Instance,
                services.GetRequiredService<IServiceScopeFactory>(),
                cache,
                meter,
                events
            );

            // All mocked tasks are already completed, so the event callback finishes inline.
            events.Publish(
                new IsarRobotInfoMessage
                {
                    IsarId = robot.IsarId,
                    RobotName = robot.Name,
                    CurrentInstallation = "BBB",
                    Host = "unapproved.example",
                    Port = 8080,
                    Capabilities = [RobotCapabilitiesEnum.take_image],
                }
            );

            Assert.Equal("approved.example", robot.Host);
            Assert.Equal(3000, robot.Port);
            robots.Verify(s => s.ReadByIsarId(robot.IsarId, true), Times.Once);
            if (registered)
            {
                Assert.Collection(
                    robot.RobotCapabilities!,
                    capability => Assert.Equal(RobotCapabilitiesEnum.take_image, capability)
                );
                robots.Verify(s => s.Update(robot), Times.Once);
            }
            robots.VerifyNoOtherCalls();
        }
    }
}
