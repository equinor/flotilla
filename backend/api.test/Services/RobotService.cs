using System;
using System.Linq;
using System.Threading.Tasks;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Api.Test.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Api.Test.Services
{
    [Collection("Database collection")]
    public class RobotServiceTest(DatabaseFixture fixture) : IAsyncLifetime
    {
        private readonly DatabaseUtilities _databaseUtilities = new(fixture.Context);
        private readonly IRobotService _robotService = fixture.ServiceProvider.GetRequiredService<IRobotService>();

        private readonly Func<Task> _resetDatabase = fixture.ResetDatabase;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await _resetDatabase();
        }

        [Fact]
        public async Task CheckThatReadAllRobotsReturnCorrectNumberOfRobots()
        {
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robotOne = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);
            var robotTwo = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);

            var robots = await _robotService.ReadAll();

            Assert.Equal(2, robots.Count());
        }

        [Fact]
        public async Task CheckThatReadOfSpecificRobotWorks()
        {
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);

            var robotById = await _robotService.ReadById(robot.Id, noTracking: true);

            Assert.Equal(robot.Id, robotById!.Id);
        }

        [Fact]
        public async Task CheckThatNullIsReturnedWhenInvalidIdIsProvided()
        {
            var robot = await _robotService.ReadById("invalid_id");
            Assert.Null(robot);
        }

        [Fact]
        public async Task CheckThatRobotIsCreated()
        {
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);

            var robotFromDatabase = await _robotService.ReadById(robot.Id);
            Assert.Equal(robot.Id, robotFromDatabase!.Id);
        }

        [Fact]
        public async Task TestThatRobotStatusIsCorrectlyUpdated()
        {
            // Arrange
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);

            // Act
            robot.Status = RobotStatus.Busy;
            await _robotService.UpdateRobotStatus(robot.Id, RobotStatus.Busy);

            // Assert
            var postTestRobot = await _robotService.ReadById(robot.Id, noTracking: true);
            Assert.Equal(RobotStatus.Busy, postTestRobot!.Status);
        }
    }
}
