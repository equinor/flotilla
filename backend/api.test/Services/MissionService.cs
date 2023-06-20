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
        private readonly MissionRunService _missionService;

        public MissionServiceTest(DatabaseFixture fixture)
        {
            _context = fixture.NewContext;
            _logger = new Mock<ILogger<MissionRunService>>().Object;
            _missionService = new MissionRunService(_context, _logger);
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
            int nReportsBefore = _missionService
                .ReadAll(new MissionRunQueryStringParameters())
                .Result.Count;

            var testAsset = new Asset
            {
                ShortName = "test",
                Name = "test test"
            };
            var testInstallation = new Installation
            {
                ShortName = "test",
                Name = "test test",
                Asset = testAsset
            };

            MissionRun mission =
                new()
                {
                    Name = "testMission",
                    Robot = robot,
                    MapMetadata = new MapMetadata() { MapName = "testMap" },
                    Area = new Area
                    {
                        Deck = new Deck
                        {
                            Installation = testInstallation,
                            Asset = testAsset,
                            Name = "testDeck"
                        },
                        Asset = testAsset,
                        Installation = testInstallation,
                        Name = "testArea",
                        MapMetadata = new MapMetadata() { MapName = "testMap" },
                        DefaultLocalizationPose = new Pose(),
                        SafePositions = new List<SafePosition>()
                    },
                    AssetCode = "testAsset",
                    DesiredStartTime = DateTime.Now
                };

            await _missionService.Create(mission);
            int nReportsAfter = _missionService
                .ReadAll(new MissionRunQueryStringParameters())
                .Result.Count;

            Assert.Equal(nReportsBefore + 1, nReportsAfter);
        }
    }
}
