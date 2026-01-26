using System;
using System.Linq;
using System.Threading.Tasks;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
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

        public async ValueTask InitializeAsync()
        {
            (Container, string connectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);

            DatabaseUtilities = new DatabaseUtilities(
                TestSetupHelpers.ConfigurePostgreSqlContext(connectionString)
            );

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
