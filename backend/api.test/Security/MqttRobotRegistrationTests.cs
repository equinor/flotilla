using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.EventHandlers;
using Api.Mqtt.MessageModels;
using Api.Services;
using Api.Services.Events;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Api.Test.Security
{
    public class MqttRobotRegistrationTests
    {
        [Theory]
        [InlineData("Local", false)]
        [InlineData("Local", true)]
        [InlineData("Development", false)]
        [InlineData("Development", true)]
        [InlineData("Staging", false)]
        [InlineData("Staging", true)]
        [InlineData("Production", false)]
        [InlineData("Production", true)]
        [InlineData("IntegrationTest", false)]
        [InlineData("IntegrationTest", true)]
        [InlineData("Test", false)]
        [InlineData("Test", true)]
        public void OnlyLocalCanRegisterOrRedirectRobots(string environmentName, bool registered)
        {
            bool local = environmentName == "Local";
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(e => e.EnvironmentName).Returns(environmentName);
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
            else if (local)
                robots
                    .Setup(s =>
                        s.CreateFromQuery(
                            It.Is<CreateRobotQuery>(q =>
                                q.IsarId == robot.IsarId
                                && q.Host == "unapproved.example"
                                && q.Port == 8080
                                && q.CurrentInstallationCode == "BBB"
                            )
                        )
                    )
                    .ReturnsAsync(robot);

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
                events,
                environment.Object
            );

            // All mocked tasks are already completed, so the event callback finishes inline.
            events.Publish(
                new IsarRobotInfoMessage
                {
                    IsarId = robot.IsarId,
                    RobotName = robot.Name,
                    CurrentInstallation = "BBB",
                    DocumentationQueries = [],
                    SerialNumber = robot.SerialNumber,
                    Host = "unapproved.example",
                    Port = 8080,
                    Capabilities = [RobotCapabilitiesEnum.take_image],
                }
            );

            Assert.Equal(
                local && registered ? "unapproved.example" : "approved.example",
                robot.Host
            );
            Assert.Equal(local && registered ? 8080 : 3000, robot.Port);
            robots.Verify(s => s.ReadByIsarId(robot.IsarId, true), Times.Once);
            if (registered)
            {
                Assert.Collection(
                    robot.RobotCapabilities!,
                    capability => Assert.Equal(RobotCapabilitiesEnum.take_image, capability)
                );
                robots.Verify(s => s.Update(robot), Times.Once);
            }
            else if (local)
                robots.Verify(s => s.CreateFromQuery(It.IsAny<CreateRobotQuery>()), Times.Once);
            robots.VerifyAll();
            robots.VerifyNoOtherCalls();
        }
    }
}
