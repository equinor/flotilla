using System;
using System.Linq;
using System.Threading.Tasks;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Api.Test.Services
{
    [Collection("Database collection")]
    public class MissionServiceTest : IDisposable
    {
        private readonly FlotillaDbContext _context;
        private readonly ILogger<MissionService> _logger;
        private readonly MissionService _missionService;

        public MissionServiceTest(DatabaseFixture fixture)
        {
            _context = fixture.NewContext;
            _logger = new Mock<ILogger<MissionService>>().Object;
            _missionService = new MissionService(_context, _logger);
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task ReadIdDoesNotExist()
        {
            var mission = await _missionService.ReadById("some_id_that_does_not_exist");
            Assert.Null(mission);
        }

        [Fact]
        public async Task Create()
        {
            var robot = _context.Robots.First();
            int nReportsBefore = _missionService.ReadAll().Result.Count;

            Mission mission =
                new()
                {
                    Name = "testMission",
                    Robot = robot,
                    EchoMissionId = 0,
                    Map = new MissionMap() { MapName = "testMap" },
                    StartTime = DateTime.Now
                };

            await _missionService.Create(mission);
            int nReportsAfter = _missionService.ReadAll().Result.Count;

            Assert.Equal(nReportsBefore + 1, nReportsAfter);
        }
    }
}
