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
        private readonly Mock<ILogger<MissionService>> _logger;
        private readonly MissionService _missionService;

        public MissionServiceTest(DatabaseFixture fixture)
        {
            _context = fixture.NewContext;
            _logger = new Mock<ILogger<MissionService>>();
            _missionService = new MissionService(_context, _logger.Object);
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task ReadAll()
        {
            var missions = await _missionService.ReadAll();
            Assert.True(missions.Any());
        }

        [Fact]
        public async Task Read()
        {
            var missions = await _missionService.ReadAll();
            var firstReport = missions.First();
            var missionById = await _missionService.ReadById(firstReport.Id);

            Assert.Equal(firstReport, missionById);
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
            await _missionService.Create(
                isarMissionId: "",
                echoMissionId: 0,
                log: "",
                status: MissionStatus.Ongoing,
                robot: robot
            );
            int nReportsAfter = _missionService.ReadAll().Result.Count;

            Assert.Equal(nReportsBefore + 1, nReportsAfter);
        }
    }
}
