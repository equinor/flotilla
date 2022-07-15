using System;
using System.Linq;
using System.Threading.Tasks;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services;
using Xunit;

namespace Api.Test.Services
{
    [Collection("Database collection")]
    public class ScheduledMissionServiceTest : IDisposable
    {
        private readonly FlotillaDbContext _context;

        public ScheduledMissionServiceTest(DatabaseFixture fixture)
        {
            _context = fixture.NewContext;
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task ReadAll()
        {
            var scheduledMissionService = new ScheduledMissionService(_context);
            var scheduledMissions = await scheduledMissionService.ReadAll();

            Assert.True(scheduledMissions.Any());
        }

        [Fact]
        public async Task Read()
        {
            var scheduledMissionService = new ScheduledMissionService(_context);
            var scheduledMissions = await scheduledMissionService.ReadAll();
            var firstScheduledMission = scheduledMissions.First();
            var scheduledMissionById = await scheduledMissionService.ReadById(firstScheduledMission.Id);

            Assert.Equal(firstScheduledMission, scheduledMissionById);
        }

        [Fact]
        public async Task ReadIdDoesNotExist()
        {
            var scheduledMissionService = new ScheduledMissionService(_context);
            var scheduledMission = await scheduledMissionService.ReadById("some_id_that_does_not_exist");
            Assert.Null(scheduledMission);
        }

        [Fact]
        public async Task Create()
        {
            var scheduledMissionService = new ScheduledMissionService(_context);
            var robotService = new RobotService(_context);
            int nScheduledMissionsBefore = scheduledMissionService.ReadAll().Result.Count();
            var robots = await robotService.ReadAll();
            var robot = robots.First();
            await scheduledMissionService.Create(new ScheduledMission()
            {
                Robot = robot,
                EchoMissionId = 49385643,
                StartTime = DateTimeOffset.UtcNow
            });
            int nScheduledMissionsAfter = scheduledMissionService.ReadAll().Result.Count();

            Assert.Equal(nScheduledMissionsBefore + 1, nScheduledMissionsAfter);
        }
    }
}
