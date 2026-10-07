using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Api.Services;
using Api.SignalRHubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Api.Test.Services
{
    // Routing reads a process-wide environment variable, so these tests must run alone.
    [CollectionDefinition(nameof(SignalRServiceTests), DisableParallelization = true)]
    public class SignalREnvironmentCollection { }

    [Collection(nameof(SignalRServiceTests))]
    public class SignalRServiceTests : IDisposable
    {
        private readonly string? _originalEnvironment = Environment.GetEnvironmentVariable(
            "ASPNETCORE_ENVIRONMENT"
        );
        private readonly Mock<IHubClients> _clients = new(MockBehavior.Strict);
        private readonly IConfiguration _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["Local:DevUserId"] = "test-user" }
            )
            .Build();

        private SignalRService CreateService()
        {
            var hub = new Mock<IHubContext<SignalRHub>>();
            hub.SetupGet(h => h.Clients).Returns(_clients.Object);
            return new SignalRService(hub.Object, _configuration);
        }

        [Theory]
        [InlineData("Production", "skipAutoMission", "AAA")]
        [InlineData("Production", "AutoScheduleFail", "AAA")]
        [InlineData("Local", "skipAutoMission", "test-userAAA")]
        [InlineData("Local", "AutoScheduleFail", "test-userAAA")]
        public void SchedulingAlertOnlyReachesItsInstallationGroup(
            string environment,
            string alertCode,
            string expectedGroup
        )
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);
            object?[]? sentArguments = null;
            var recipient = new Mock<IClientProxy>(MockBehavior.Strict);
            recipient
                .Setup(p =>
                    p.SendCoreAsync("Alert", It.IsAny<object?[]>(), It.IsAny<CancellationToken>())
                )
                .Callback<string, object?[], CancellationToken>(
                    (_, arguments, _) => sentArguments = arguments
                )
                .Returns(Task.CompletedTask);
            _clients.Setup(c => c.Group(expectedGroup)).Returns(recipient.Object);

            CreateService()
                .ReportAutoScheduleToSignalR(alertCode, "Mission name", "Schedule message", "aaa");

            _clients.Verify(c => c.Group(expectedGroup), Times.Once);
            _clients.VerifyGet(c => c.All, Times.Never);
            recipient.Verify(
                p => p.SendCoreAsync("Alert", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
            _clients.VerifyNoOtherCalls();
            Assert.NotNull(sentArguments);
            Assert.Equal(2, sentArguments.Length);
            Assert.Equal("all", sentArguments[0]);
            using var payload = JsonDocument.Parse(Assert.IsType<string>(sentArguments[1]));
            var alert = payload.RootElement;
            Assert.Equal(alertCode, alert.GetProperty("alertCode").GetString());
            Assert.Equal("Mission name", alert.GetProperty("alertTitle").GetString());
            Assert.Equal("Schedule message", alert.GetProperty("alertMessage").GetString());
            Assert.Equal("aaa", alert.GetProperty("installationCode").GetString());
            Assert.Equal(JsonValueKind.Null, alert.GetProperty("robotId").ValueKind);
        }

        [Theory]
        [InlineData("Production", null)]
        [InlineData("Production", "")]
        [InlineData("Production", " ")]
        [InlineData("Local", null)]
        [InlineData("Local", "")]
        [InlineData("Local", " ")]
        public void SchedulingAlertRejectsMissingInstallation(
            string environment,
            string? installationCode
        )
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);

            var exception = Assert.ThrowsAny<ArgumentException>(() =>
                CreateService()
                    .ReportAutoScheduleToSignalR(
                        "AutoScheduleFail",
                        "Mission name",
                        "Schedule message",
                        installationCode!
                    )
            );

            Assert.Equal("installationCode", exception.ParamName);
            _clients.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("Production", "AAA")]
        [InlineData("Local", "test-userAAA")]
        public async Task HubJoinsOnlyWriteAuthorizedInstallationGroups(
            string environment,
            string expectedGroup
        )
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);
            var user = new ClaimsPrincipal(new ClaimsIdentity("Test"));
            var roles = new Mock<IAccessRoleService>(MockBehavior.Strict);
            roles
                .Setup(r => r.GetAllowedInstallationCodes(user, AccessMode.Write))
                .ReturnsAsync(["aaa"]);
            var context = new Mock<HubCallerContext>();
            context.SetupGet(c => c.User).Returns(user);
            context.SetupGet(c => c.ConnectionId).Returns("connection");
            var groups = new Mock<IGroupManager>();
            using var hub = new SignalRHub(roles.Object, _configuration)
            {
                Context = context.Object,
                Groups = groups.Object,
            };

            await hub.OnConnectedAsync();

            roles.Verify(r => r.GetAllowedInstallationCodes(user, AccessMode.Write), Times.Once);
            groups.Verify(
                g => g.AddToGroupAsync("connection", expectedGroup, It.IsAny<CancellationToken>()),
                Times.Once
            );
            if (environment == "Local")
                groups.Verify(
                    g =>
                        g.AddToGroupAsync("connection", "test-user", It.IsAny<CancellationToken>()),
                    Times.Once
                );
            groups.VerifyNoOtherCalls();
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _originalEnvironment);
        }
    }
}
