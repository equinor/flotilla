using System;
using System.Linq;
using System.Threading.Tasks;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Services
{
    public class RobotServiceTest : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required PostgreSqlContainer Container;
        public required IRobotService RobotService;
        public required IInstallationService InstallationService;
        private IServiceProvider _services = null!;

        public async ValueTask InitializeAsync()
        {
            (Container, string connectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);
            _services = serviceProvider;

            DatabaseUtilities = serviceProvider.GetRequiredService<DatabaseUtilities>();

            RobotService = serviceProvider.GetRequiredService<IRobotService>();
            InstallationService = serviceProvider.GetRequiredService<IInstallationService>();
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        [Fact]
        public async Task CheckThatReadAllRobotsReturnsCorrectNumberOfRobots()
        {
            var installation = await DatabaseUtilities.NewInstallation();
            _ = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);
            _ = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);
            var robots = await RobotService.ReadAll();

            Assert.Equal(2, robots.Count());
        }

        [Fact]
        public async Task CheckThatReadByIdReturnsCorrectRobot()
        {
            var installation = await DatabaseUtilities.NewInstallation();
            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);

            var robotById = await RobotService.ReadById(robot.Id, readOnly: true);

            Assert.Equal(robot.Id, robotById!.Id);
        }

        [Fact]
        public async Task CheckThatReadByUnknownIdReturnsNull()
        {
            var robot = await RobotService.ReadById("IDoNotExists", readOnly: true);
            Assert.Null(robot);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(false, true)]
        public async Task ReadByIdSeparatesAccessFromTracking(bool readOnly, bool canWrite)
        {
            var installation = await DatabaseUtilities.NewInstallation("BBB");
            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);
            var roles = new Mock<IAccessRoleService>(MockBehavior.Strict);
            roles.Setup(s => s.GetAllowedInstallationCodes(AccessMode.Read)).ReturnsAsync(["BBB"]);
            roles
                .Setup(s => s.GetAllowedInstallationCodes(AccessMode.Write))
                .ReturnsAsync(canWrite ? ["BBB"] : ["AAA"]);
            var context = _services.GetRequiredService<FlotillaDbContext>();
            var service = ActivatorUtilities.CreateInstance<Api.Services.RobotService>(
                _services,
                context,
                roles.Object
            );
            context.ChangeTracker.Clear();

            var readable = await service.ReadById(robot.Id, readOnly);
            Assert.NotNull(readable);
            Assert.Equal(!readOnly, context.ChangeTracker.Entries<Robot>().Any());
            context.ChangeTracker.Clear();

            var writable = await service.ReadById(robot.Id, readOnly, AccessMode.Write);
            Assert.Equal(canWrite, writable != null);
            Assert.Equal(canWrite && !readOnly, context.ChangeTracker.Entries<Robot>().Any());
            roles.VerifyAll();
        }

        [Fact]
        public async Task CheckTheNumberOfRobotsIncreaseWhenAddingNewRobotToDatabase()
        {
            var installation = await DatabaseUtilities.NewInstallation();

            var robotsBefore = await RobotService.ReadAll(readOnly: true);
            int nRobotsBefore = robotsBefore.Count();

            _ = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);

            var robotsAfter = await RobotService.ReadAll(readOnly: true);
            int nRobotsAfter = robotsAfter.Count();

            Assert.Equal(nRobotsBefore + 1, nRobotsAfter);
        }
    }
}
