using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.Models;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Services
{
    public class MissionSchedulingServiceTest : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required PostgreSqlContainer Container;
        public required IMissionSchedulingService MissionSchedulingService;

        public required IMissionRunService MissionRunService;

        public async Task InitializeAsync()
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
            var robot = await DatabaseUtilities.NewRobot(
                RobotStatus.Available,
                installation,
                inspectionArea
            );
            await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea,
                writeToDatabase: true
            );

            var reportsBefore = await MissionRunService.ReadAll(
                new MissionRunQueryStringParameters()
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

        [Fact]
        public async Task CheckThatReturnHomeIsNotDeletedWhenPreviousMissionWasOutsideInspectionArea()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea1 = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode,
                inspectionAreaName: "Inspection area 1",
                new InspectionAreaPolygon
                {
                    ZMin = 0,
                    ZMax = 10,
                    Positions =
                    [
                        new XYPosition(x: 0, y: 0),
                        new XYPosition(x: 0, y: 10),
                        new XYPosition(x: 10, y: 10),
                        new XYPosition(x: 10, y: 0),
                    ],
                }
            );
            var inspectionArea2 = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode,
                inspectionAreaName: "Inspection area 2",
                new InspectionAreaPolygon
                {
                    ZMin = 0,
                    ZMax = 10,
                    Positions =
                    [
                        new XYPosition(x: 0, y: 0),
                        new XYPosition(x: 0, y: -10),
                        new XYPosition(x: -10, y: -10),
                        new XYPosition(x: -10, y: 0),
                    ],
                }
            );

            var taskOutsideInspectionArea = new MissionTask(
                new Pose(-5, -5, 0, 0, 0, 0, 0),
                MissionTaskType.Inspection
            );
            var returnHomeTask = new MissionTask(
                new Pose(0, 0, 0, 0, 0, 0, 0),
                MissionTaskType.Inspection
            );

            var robot = await DatabaseUtilities.NewRobot(
                RobotStatus.Available,
                installation,
                inspectionArea2
            );
            await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea1,
                writeToDatabase: true,
                missionRunType: MissionRunType.ReturnHome,
                tasks: [returnHomeTask]
            );
            await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea1,
                writeToDatabase: true,
                tasks: [taskOutsideInspectionArea]
            );

            // Act
            await MissionSchedulingService.StartNextMissionRunIfSystemIsAvailable(robot);

            // Assert
            var reportsAfter = await MissionRunService.ReadAll(
                new MissionRunQueryStringParameters()
            );

            // TODO: the mission still exists, it is however aborted. Need to check why no return home mission scheduled

            Assert.Equal(2, reportsAfter.Count);
            Assert.False(reportsAfter[0].IsReturnHomeMission());
            Assert.Equal(MissionStatus.Aborted, reportsAfter[0].Status);
            Assert.True(reportsAfter[1].IsReturnHomeMission());
            Assert.Equal(MissionStatus.Ongoing, reportsAfter[1].Status);
        }

        [Fact]
        public async Task CheckThatReturnHomeIsNotScheduledWhenFailingToStartFirstMission()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea1 = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode,
                inspectionAreaName: "Inspection area 1",
                new InspectionAreaPolygon
                {
                    ZMin = 0,
                    ZMax = 10,
                    Positions =
                    [
                        new XYPosition(x: 0, y: 0),
                        new XYPosition(x: 0, y: 10),
                        new XYPosition(x: 10, y: 10),
                        new XYPosition(x: 10, y: 0),
                    ],
                }
            );
            var inspectionArea2 = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode,
                inspectionAreaName: "Inspection area 2",
                new InspectionAreaPolygon
                {
                    ZMin = 0,
                    ZMax = 10,
                    Positions =
                    [
                        new XYPosition(x: 0, y: 0),
                        new XYPosition(x: 0, y: -10),
                        new XYPosition(x: -10, y: -10),
                        new XYPosition(x: -10, y: 0),
                    ],
                }
            );

            var taskOutsideInspectionArea = new MissionTask(
                new Pose(-5, -5, 0, 0, 0, 0, 0),
                MissionTaskType.Inspection
            );

            var robot = await DatabaseUtilities.NewRobot(
                RobotStatus.Available,
                installation,
                inspectionArea2
            );
            await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea1,
                writeToDatabase: true,
                tasks: [taskOutsideInspectionArea]
            );

            // Act
            await MissionSchedulingService.StartNextMissionRunIfSystemIsAvailable(robot);

            // Assert
            var reportsAfter = await MissionRunService.ReadAll(
                new MissionRunQueryStringParameters()
            );

            Assert.Single(reportsAfter);
            Assert.False(reportsAfter[0].IsReturnHomeMission());
            Assert.Equal(MissionStatus.Aborted, reportsAfter[0].Status);
        }
    }
}
