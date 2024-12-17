﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Test.Services
{
    public class RobotServiceTest : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required IRobotService RobotService;
        public required IInstallationService InstallationService;

        public async Task InitializeAsync()
        {
            string databaseName = Guid.NewGuid().ToString();
            (string connectionString, var connection) = await TestSetupHelpers.ConfigureDatabase(
                databaseName
            );
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(databaseName);
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);

            DatabaseUtilities = new DatabaseUtilities(
                TestSetupHelpers.ConfigureFlotillaDbContext(connectionString)
            );

            RobotService = serviceProvider.GetRequiredService<IRobotService>();
            InstallationService = serviceProvider.GetRequiredService<IInstallationService>();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task ReadAll()
        {
            var installation = await DatabaseUtilities.NewInstallation();
            _ = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);
            var robots = await RobotService.ReadAll();

            Assert.True(robots.Any());
        }

        [Fact]
        public async Task Read()
        {
            var installation = await DatabaseUtilities.NewInstallation();
            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);
            var robotById = await RobotService.ReadById(robot.Id, readOnly: false);
            Assert.NotNull(robotById);
            Assert.Equal(robot.Id, robotById.Id);
        }

        [Fact]
        public async Task ReadIdDoesNotExist()
        {
            var robot = await RobotService.ReadById("some_id_that_does_not_exist", readOnly: true);
            Assert.Null(robot);
        }

        [Fact]
        public async Task Create()
        {
            var installation = await InstallationService.Create(
                new CreateInstallationQuery { Name = "Johan Sverdrup", InstallationCode = "JSV" }
            );

            var robotsBefore = await RobotService.ReadAll(readOnly: true);
            int nRobotsBefore = robotsBefore.Count();

            _ = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);

            var robotsAfter = await RobotService.ReadAll(readOnly: true);
            int nRobotsAfter = robotsAfter.Count();

            Assert.Equal(nRobotsBefore + 1, nRobotsAfter);
        }
    }
}
