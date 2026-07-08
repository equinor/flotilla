using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Test.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;
using TaskStatus = Api.Database.Models.TaskStatus;

namespace Api.Test.Controllers
{
    public class StatisticsControllerTests : IAsyncLifetime
    {
        private const long SecondsPerHour = 3600;
        private const long SecondsPerDay = 24 * SecondsPerHour;
        private const long SecondsPerWeek = 7 * SecondsPerDay;

        public required DatabaseUtilities DatabaseUtilities;
        public required PostgreSqlContainer Container;
        public required string ConnectionString;
        public required HttpClient Client;
        public required JsonSerializerOptions SerializerOptions;
        public required FlotillaDbContext Context;

        public async ValueTask InitializeAsync()
        {
            (Container, ConnectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: ConnectionString
            );
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);

            Client = TestSetupHelpers.ConfigureHttpClient(factory);
            SerializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();
            Context = TestSetupHelpers.ConfigurePostgreSqlContext(ConnectionString);

            DatabaseUtilities = serviceProvider.GetRequiredService<DatabaseUtilities>();
        }

        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            await Task.CompletedTask;
        }

        private async Task<(Installation, InspectionArea, Robot)> SetupInfrastructure()
        {
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);
            return (installation, inspectionArea, robot);
        }

        private Task<MissionRun> CreateRun(
            Installation installation,
            Robot robot,
            InspectionArea inspectionArea,
            MissionStatus status,
            MissionTask[]? tasks = null
        ) =>
            DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea,
                writeToDatabase: true,
                missionStatus: status,
                tasks: tasks ?? []
            );

        private static MissionTask NewTask(TaskStatus status, int order) =>
            new()
            {
                TagId = $"tag-{order}",
                Description = "Task",
                RobotPose = new Pose(),
                Status = status,
                TaskOrder = order,
            };

        private static DateTime DaysAgo(int days) => DateTime.UtcNow.AddDays(-days);

        private async Task SetCreationTime(string missionRunId, DateTime creationTime)
        {
            await Context
                .MissionRuns.Where(m => m.Id == missionRunId)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(m => m.CreationTime, creationTime)
                );
        }

        private async Task<RobotStatisticsResponse> GetStatistics(
            string robotId,
            long minCreationTime,
            long maxCreationTime
        )
        {
            var response = await Client.GetAsync(
                $"statistics/robots/{robotId}/missions?minCreationTime={minCreationTime}&maxCreationTime={maxCreationTime}",
                TestContext.Current.CancellationToken
            );
            response.EnsureSuccessStatusCode();
            var statistics = await response.Content.ReadFromJsonAsync<RobotStatisticsResponse>(
                SerializerOptions,
                cancellationToken: TestContext.Current.CancellationToken
            );
            return statistics!;
        }

        [Fact]
        public async Task GetRobotMissionStatistics_CountsCompletedRunsAndSuccessRate()
        {
            var (installation, inspectionArea, robot) = await SetupInfrastructure();
            await CreateRun(installation, robot, inspectionArea, MissionStatus.Successful);
            await CreateRun(installation, robot, inspectionArea, MissionStatus.PartiallySuccessful);
            await CreateRun(installation, robot, inspectionArea, MissionStatus.Failed);
            await CreateRun(installation, robot, inspectionArea, MissionStatus.Aborted);
            await CreateRun(installation, robot, inspectionArea, MissionStatus.Cancelled);
            // In-flight run must be excluded from the completed-run counts.
            await CreateRun(installation, robot, inspectionArea, MissionStatus.Queued);

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var statistics = await GetStatistics(
                robot.Id,
                now - SecondsPerDay,
                now + SecondsPerDay
            );

            Assert.Equal(5, statistics.Missions.Total);
            Assert.Equal(1, statistics.Missions.Successful);
            Assert.Equal(1, statistics.Missions.PartiallySuccessful);
            Assert.Equal(1, statistics.Missions.Failed);
            Assert.Equal(0.4, statistics.Missions.SuccessRate, 3);
        }

        [Fact]
        public async Task GetRobotMissionStatistics_AggregatesTaskCountsForCompletedRuns()
        {
            var (installation, inspectionArea, robot) = await SetupInfrastructure();
            await CreateRun(
                installation,
                robot,
                inspectionArea,
                MissionStatus.Successful,
                [
                    NewTask(TaskStatus.Successful, 0),
                    NewTask(TaskStatus.Successful, 1),
                    NewTask(TaskStatus.PartiallySuccessful, 2),
                    NewTask(TaskStatus.Failed, 3),
                ]
            );
            // Tasks belonging to an in-flight run must not be counted.
            await CreateRun(
                installation,
                robot,
                inspectionArea,
                MissionStatus.Queued,
                [NewTask(TaskStatus.Successful, 0)]
            );

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var statistics = await GetStatistics(
                robot.Id,
                now - SecondsPerDay,
                now + SecondsPerDay
            );

            Assert.Equal(4, statistics.Tasks.Total);
            Assert.Equal(2, statistics.Tasks.Successful);
            Assert.Equal(1, statistics.Tasks.PartiallySuccessful);
            Assert.Equal(0.75, statistics.Tasks.SuccessRate, 3);
        }

        [Fact]
        public async Task GetRobotMissionStatistics_ExcludesRunsOutsideTimeWindow()
        {
            var (installation, inspectionArea, robot) = await SetupInfrastructure();
            var insideRun = await CreateRun(
                installation,
                robot,
                inspectionArea,
                MissionStatus.Successful
            );
            var outsideRun = await CreateRun(
                installation,
                robot,
                inspectionArea,
                MissionStatus.Successful
            );
            await SetCreationTime(insideRun.Id, DaysAgo(1));
            await SetCreationTime(outsideRun.Id, DaysAgo(10));

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var statistics = await GetStatistics(
                robot.Id,
                now - 2 * SecondsPerDay,
                now + SecondsPerDay
            );

            Assert.Equal(1, statistics.Missions.Total);
        }

        [Fact]
        public async Task GetRobotMissionStatistics_GroupsCompletedRunsIntoWeeklyBuckets()
        {
            var (installation, inspectionArea, robot) = await SetupInfrastructure();
            var thisWeek = await CreateRun(
                installation,
                robot,
                inspectionArea,
                MissionStatus.Successful
            );
            var lastWeek = await CreateRun(
                installation,
                robot,
                inspectionArea,
                MissionStatus.Successful
            );
            var twoWeeksAgo = await CreateRun(
                installation,
                robot,
                inspectionArea,
                MissionStatus.Successful
            );
            await SetCreationTime(thisWeek.Id, DaysAgo(2));
            await SetCreationTime(lastWeek.Id, DaysAgo(9));
            await SetCreationTime(twoWeeksAgo.Id, DaysAgo(16));

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var statistics = await GetStatistics(robot.Id, now - 4 * SecondsPerWeek, now);

            // Buckets are oldest-first; the last covers the most recent week.
            Assert.Equal(4, statistics.MissionsPerWeek.Count);
            Assert.Equal(0, statistics.MissionsPerWeek[0].Count);
            Assert.Equal(1, statistics.MissionsPerWeek[1].Count);
            Assert.Equal(1, statistics.MissionsPerWeek[2].Count);
            Assert.Equal(1, statistics.MissionsPerWeek[3].Count);
        }

        [Fact]
        public async Task GetRobotMissionStatistics_WithNoRuns_ReturnsZeroedStatistics()
        {
            var (_, _, robot) = await SetupInfrastructure();

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var statistics = await GetStatistics(robot.Id, now - 2 * SecondsPerWeek, now);

            Assert.Equal(0, statistics.Missions.Total);
            Assert.Equal(0, statistics.Tasks.Total);
            Assert.Equal(0, statistics.Missions.SuccessRate);
            Assert.Equal(2, statistics.MissionsPerWeek.Count);
        }

        [Fact]
        public async Task GetRobotMissionStatistics_WithoutTimeParameters_ReturnsBadRequest()
        {
            var response = await Client.GetAsync(
                "statistics/robots/any-robot/missions",
                TestContext.Current.CancellationToken
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetRobotMissionStatistics_WithMaxBeforeMin_ReturnsBadRequest()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var response = await Client.GetAsync(
                $"statistics/robots/any-robot/missions?minCreationTime={now}&maxCreationTime={now - SecondsPerHour}",
                TestContext.Current.CancellationToken
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
