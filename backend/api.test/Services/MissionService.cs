using System;
using System.Linq;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Services;
using Api.Test.Mocks;
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
        private readonly RobotService _robotService;
        private readonly IEchoService _echoService;
        private readonly IMapService _mapService;
        private readonly IStidService _stidService;

        public MissionServiceTest(DatabaseFixture fixture)
        {
            _context = fixture.NewContext;
            _logger = new Mock<ILogger<MissionService>>().Object;
            _robotService = new RobotService(_context);
            _echoService = new MockEchoService();
            _mapService = new MockMapService();
            _stidService = new Mock<IStidService>().Object;
            _missionService = new MissionService(
                _context,
                _logger,
                _mapService,
                _robotService,
                _echoService,
                _stidService
            );
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
            ScheduledMissionQuery scheduledMission =
                new()
                {
                    RobotId = robot.Id,
                    EchoMissionId = 95,
                    StartTime = DateTimeOffset.Now
                };

            await _missionService.Create(scheduledMission);
            int nReportsAfter = _missionService.ReadAll().Result.Count;

            Assert.Equal(nReportsBefore + 1, nReportsAfter);
        }
    }
}
