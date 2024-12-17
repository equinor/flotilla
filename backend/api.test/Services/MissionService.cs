using System;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Test.Services
{
    public class MissionServiceTest : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
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
            MissionRunService = serviceProvider.GetRequiredService<IMissionRunService>();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task ReadIdDoesNotExist()
        {
            var missionRun = await MissionRunService.ReadById(
                "some_id_that_does_not_exist",
                readOnly: true
            );
            Assert.Null(missionRun);
        }

        [Fact]
        public async Task Create()
        {
            var reportsBefore = await MissionRunService.ReadAll(
                new MissionRunQueryStringParameters(),
                readOnly: true
            );
            int nReportsBefore = reportsBefore.Count;

            var installation = await DatabaseUtilities.ReadOrNewInstallation();
            var plant = await DatabaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.ReadOrNewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);
            var missionRun = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea
            );

            await MissionRunService.Create(missionRun);

            var reportsAfter = await MissionRunService.ReadAll(
                new MissionRunQueryStringParameters()
            );
            int nReportsAfter = reportsAfter.Count;
            Assert.Equal(nReportsBefore + 1, nReportsAfter);
        }
    }
}
