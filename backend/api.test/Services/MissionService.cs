using System;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
namespace Api.Test.Services
{
    [Collection("Database collection")]
    public class MissionServiceTest : IDisposable
    {
        private readonly FlotillaDbContext _context;
        private readonly DatabaseUtilities _databaseUtilities;
        private readonly ILogger<MissionRunService> _logger;
        private readonly MissionRunService _missionRunService;
        private readonly ISignalRService _signalRService;
        private readonly IAccessRoleService _accessRoleService;
        private readonly UserInfoService _userInfoService;

        public MissionServiceTest(DatabaseFixture fixture)
        {
            _context = fixture.NewContext;
            _logger = new Mock<ILogger<MissionRunService>>().Object;
            _signalRService = new MockSignalRService();
            _accessRoleService = new AccessRoleService(_context, new HttpContextAccessor());
            _userInfoService = new UserInfoService(_context, new HttpContextAccessor(), new Mock<ILogger<UserInfoService>>().Object);
            _missionRunService = new MissionRunService(_context, _signalRService, _logger, _accessRoleService, _userInfoService);
            _databaseUtilities = new DatabaseUtilities(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task ReadIdDoesNotExist()
        {
            var missionRun = await _missionRunService.ReadById("some_id_that_does_not_exist", readOnly: true);
            Assert.Null(missionRun);
        }

        [Fact]
        public async Task Create()
        {
            var reportsBefore = await _missionRunService.ReadAll(
                new MissionRunQueryStringParameters(),
                readOnly: true
            );
            int nReportsBefore = reportsBefore.Count;

            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation);
            var missionRun = await _databaseUtilities.NewMissionRun(installation.InstallationCode, robot, area);

            await _missionRunService.Create(missionRun);

            var reportsAfter = await _missionRunService.ReadAll(
                new MissionRunQueryStringParameters()
            );
            int nReportsAfter = reportsAfter.Count;
            Assert.Equal(nReportsBefore + 1, nReportsAfter);
        }
    }
}
