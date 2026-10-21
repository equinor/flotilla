using System;
using System.Threading.Tasks;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Api.Test.Security
{
    public class AutoScheduleAuthorizationTests
    {
        [Theory]
        [InlineData("reconcile")]
        [InlineData("update")]
        [InlineData("skip")]
        public async Task UnauthorizedSchedulingHasNoSideEffects(string operation)
        {
            var roles = new Mock<IAccessRoleService>(MockBehavior.Strict);
            roles.Setup(r => r.GetAllowedInstallationCodes(AccessMode.Write)).ReturnsAsync(["AAA"]);
            var definitions = new Mock<IMissionDefinitionService>(MockBehavior.Strict);
            var jobs = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
            using var context = new FlotillaDbContext(
                new DbContextOptionsBuilder<FlotillaDbContext>()
                    .UseNpgsql("Host=localhost;Database=unused")
                    .Options
            );
            var service = new AutoScheduleService(
                NullLogger<AutoScheduleService>.Instance,
                definitions.Object,
                Mock.Of<IRobotService>(),
                Mock.Of<IMissionRunService>(),
                Mock.Of<IMissionSchedulingService>(),
                Mock.Of<ISignalRService>(),
                roles.Object,
                jobs.Object,
                context,
                TimeProvider.System
            );
            var definition = new MissionDefinition { Id = "mission", InstallationCode = "BBB" };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                operation switch
                {
                    "reconcile" => service.Reconcile(definition),
                    "update" => service.UpdateAutoScheduleFrequency(definition, []),
                    _ => service.SkipOccurrence(definition, "occurrence"),
                }
            );
            jobs.VerifyNoOtherCalls();
            definitions.VerifyNoOtherCalls();
        }
    }
}
