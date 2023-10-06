using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Controllers.Models;
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
        private readonly ILogger<MissionRunService> _logger;
        private readonly ISignalRService _signalRService;
        private readonly MissionRunService _missionRunService;

        public MissionServiceTest(DatabaseFixture fixture)
        {
            _context = fixture.NewContext;
            _logger = new Mock<ILogger<MissionRunService>>().Object;
            _signalRService = new MockSignalRService();
            _missionRunService = new MissionRunService(_context, _signalRService, _logger);
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task ReadIdDoesNotExist()
        {
            var missionRun = await _missionRunService.ReadById("some_id_that_does_not_exist");
            Assert.Null(missionRun);
        }

#pragma warning disable xUnit1004
        [Fact(Skip = "Awaiting fix for testing with database")]
#pragma warning restore xUnit1004
        public async Task Create()
        {
            var robot = _context.Robots.First();
            var reportsBefore = await _missionRunService.ReadAll(new MissionRunQueryStringParameters());
            int nReportsBefore = reportsBefore.Count;
            var testInstallation = new Installation
            {
                InstallationCode = "test",
                Name = "test test"
            };
            var testPlant = new Plant
            {
                PlantCode = "test",
                Name = "test test",
                Installation = testInstallation
            };

            MissionRun missionRun =
                new()
                {
                    Name = "testMission",
                    Robot = robot,
                    MissionId = Guid.NewGuid().ToString(),
                    Map = new MapMetadata { MapName = "testMap" },
                    Area = new Area
                    {
                        Deck = new Deck
                        {
                            Plant = testPlant,
                            Installation = testInstallation,
                            Name = "testDeck"
                        },
                        Installation = testInstallation,
                        Plant = testPlant,
                        Name = "testArea",
                        MapMetadata = new MapMetadata { MapName = "testMap" },
                        DefaultLocalizationPose = null,
                        SafePositions = new List<SafePosition>()
                    },
                    InstallationCode = "testInstallation",
                    DesiredStartTime = DateTime.Now
                };

            await _missionRunService.Create(missionRun);
            var reportsAfter = await _missionRunService.ReadAll(new MissionRunQueryStringParameters());
            int nReportsAfter = reportsAfter.Count;
            Assert.Equal(nReportsBefore + 1, nReportsAfter);
        }
    }
}
