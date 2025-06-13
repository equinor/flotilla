using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Services
{
    public class InspectionAreaServiceTest : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required PostgreSqlContainer Container;
        public required IMissionRunService MissionRunService;
        public required IInspectionAreaService InspectionAreaService;

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
            MissionRunService = serviceProvider.GetRequiredService<IMissionRunService>();
            InspectionAreaService = serviceProvider.GetRequiredService<IInspectionAreaService>();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task TestTasksInsidePolygon()
        {
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            inspectionArea.AreaPolygonJson =
                @"{
                    ""zmin"": 0,
                    ""zmax"": 10,
                    ""positions"": [
                        { ""x"": 0, ""y"": 0 },
                        { ""x"": 0, ""y"": 10 },
                        { ""x"": 10, ""y"": 10 },
                        { ""x"": 10, ""y"": 0 }
                    ]
                }";

            List<MissionTask> missionTasks =
            [
                new(new Pose(1, 1, 1, 0, 0, 0, 1)),
                new(new Pose(2, 2, 2, 0, 0, 0, 1)),
            ];

            var testBool = InspectionAreaService.MissionTasksAreInsideInspectionAreaPolygon(
                missionTasks,
                inspectionArea
            );
            Assert.True(testBool);
        }

        [Fact]
        public async Task TestTasksOutsidePolygon()
        {
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            inspectionArea.AreaPolygonJson =
                @"{
                    ""zmin"": 0,
                    ""zmax"": 10,
                    ""positions"": [
                        { ""x"": 0, ""y"": 0 },
                        { ""x"": 0, ""y"": 10 },
                        { ""x"": 10, ""y"": 10 },
                        { ""x"": 10, ""y"": 0 }
                    ]
                }";
            List<MissionTask> missionTasks =
            [
                new(new Pose(1, 1, 1, 0, 0, 0, 1)),
                new(new Pose(11, 11, 11, 0, 0, 0, 1)),
            ];

            var testBool = InspectionAreaService.MissionTasksAreInsideInspectionAreaPolygon(
                missionTasks,
                inspectionArea
            );
            Assert.False(testBool);
        }
    }
}
