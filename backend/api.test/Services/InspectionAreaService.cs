using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        public required IAreaPolygonService AreaPolygonService;

        public async ValueTask InitializeAsync()
        {
            (Container, string connectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);

            DatabaseUtilities = serviceProvider.GetRequiredService<DatabaseUtilities>();
            MissionRunService = serviceProvider.GetRequiredService<IMissionRunService>();
            InspectionAreaService = serviceProvider.GetRequiredService<IInspectionAreaService>();
            AreaPolygonService = serviceProvider.GetRequiredService<IAreaPolygonService>();
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        [Fact]
        public void TestTasksInsidePolygon()
        {
            var areaPolygon = new AreaPolygon
            {
                ZMin = 0,
                ZMax = 10,
                Positions =
                [
                    new PolygonPoint { X = 0, Y = 0 },
                    new PolygonPoint { X = 0, Y = 10 },
                    new PolygonPoint { X = 10, Y = 10 },
                    new PolygonPoint { X = 10, Y = 0 },
                ],
            };

            List<MissionTask> missionTasks =
            [
                new(new Pose(1, 1, 1, 0, 0, 0, 1)),
                new(new Pose(2, 2, 2, 0, 0, 0, 1)),
            ];

            var testBool = AreaPolygonService.MissionTasksAreInsideAreaPolygon(
                missionTasks,
                areaPolygon
            );
            Assert.True(testBool);
        }

        [Fact]
        public void TestTasksOutsidePolygon()
        {
            var areaPolygon = new AreaPolygon
            {
                ZMin = 0,
                ZMax = 10,
                Positions =
                [
                    new PolygonPoint { X = 0, Y = 0 },
                    new PolygonPoint { X = 0, Y = 10 },
                    new PolygonPoint { X = 10, Y = 10 },
                    new PolygonPoint { X = 10, Y = 0 },
                ],
            };
            List<MissionTask> missionTasks =
            [
                new(new Pose(1, 1, 1, 0, 0, 0, 1)),
                new(new Pose(11, 11, 11, 0, 0, 0, 1)),
            ];

            var testBool = AreaPolygonService.MissionTasksAreInsideAreaPolygon(
                missionTasks,
                areaPolygon
            );
            Assert.False(testBool);
        }
    }
}
