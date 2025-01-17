using System;
using System.Threading;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Test.Services
{
    public class MissionSchedulingServiceTest : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required IMissionSchedulingService MissionSchedulingService;

        public required IMissionRunService MissionRunService;

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
            MissionSchedulingService =
                serviceProvider.GetRequiredService<IMissionSchedulingService>();
            MissionRunService = serviceProvider.GetRequiredService<IMissionRunService>();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task CheckThatReturnHomeIsCreatedWhenRunningMission()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);
            await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea,
                writeToDatabase: true
            );

            var reportsBefore = await MissionRunService.ReadAll(
                new MissionRunQueryStringParameters(),
                readOnly: true
            );
            int nReportsBefore = reportsBefore.Count;

            // Act
            await MissionSchedulingService.StartNextMissionRunIfSystemIsAvailable(robot);

            // Assert
            var reportsAfter = await MissionRunService.ReadAll(
                new MissionRunQueryStringParameters()
            );
            int nReportsAfter = reportsAfter.Count;

            // We expect two new missions since a return home mission will also be scheduled
            Assert.Equal(nReportsBefore + 1, nReportsAfter);
        }
    }
}
