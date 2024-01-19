using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
namespace Api.Test.Services
{
    [Collection("Database collection")]
    public class RobotServiceTest : IDisposable
    {
        private readonly FlotillaDbContext _context;
        private readonly ILogger<RobotService> _logger;
        private readonly RobotModelService _robotModelService;
        private readonly ISignalRService _signalRService;
        private readonly IAccessRoleService _accessRoleService;
        private readonly IInstallationService _installationService;
        private readonly IPlantService _plantService;
        private readonly IDefaultLocalizationPoseService _defaultLocalizationPoseService;
        private readonly IDeckService _deckService;
        private readonly IAreaService _areaService;
        private readonly IMissionRunService _missionRunService;

        public RobotServiceTest(DatabaseFixture fixture)
        {
            _context = fixture.NewContext;
            _logger = new Mock<ILogger<RobotService>>().Object;
            _robotModelService = new RobotModelService(_context);
            _signalRService = new MockSignalRService();
            _accessRoleService = new AccessRoleService(_context, new HttpContextAccessor());
            _installationService = new InstallationService(_context, _accessRoleService);
            _plantService = new PlantService(_context, _installationService, _accessRoleService);
            _defaultLocalizationPoseService = new DefaultLocalizationPoseService(_context);
            _deckService = new DeckService(_context, _defaultLocalizationPoseService, _installationService, _plantService, _accessRoleService, _signalRService);
            _areaService = new AreaService(_context, _installationService, _plantService, _deckService, _defaultLocalizationPoseService, _accessRoleService);
            _missionRunService = new MissionRunService(_context, _signalRService, new Mock<ILogger<MissionRunService>>().Object, _accessRoleService);
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task ReadAll()
        {
            var robotService = new RobotService(_context, _logger, _robotModelService, _signalRService, _accessRoleService, _installationService, _areaService, _missionRunService);
            var robots = await robotService.ReadAll();

            Assert.True(robots.Any());
        }

        [Fact]
        public async Task Read()
        {
            var robotService = new RobotService(_context, _logger, _robotModelService, _signalRService, _accessRoleService, _installationService, _areaService, _missionRunService);
            var robots = await robotService.ReadAll();
            var firstRobot = robots.First();
            var robotById = await robotService.ReadById(firstRobot.Id);

            Assert.Equal(firstRobot, robotById);
        }

        [Fact]
        public async Task ReadIdDoesNotExist()
        {
            var robotService = new RobotService(_context, _logger, _robotModelService, _signalRService, _accessRoleService, _installationService, _areaService, _missionRunService);
            var robot = await robotService.ReadById("some_id_that_does_not_exist");
            Assert.Null(robot);
        }

        [Fact]
        public async Task Create()
        {
            var robotService = new RobotService(_context, _logger, _robotModelService, _signalRService, _accessRoleService, _installationService, _areaService, _missionRunService);
            var installationService = new InstallationService(_context, _accessRoleService);

            var installation = await installationService.Create(new CreateInstallationQuery
            {
                Name = "Johan Sverdrup",
                InstallationCode = "JSV"
            });

            var robotsBefore = await robotService.ReadAll();
            int nRobotsBefore = robotsBefore.Count();
            var videoStreamQuery = new CreateVideoStreamQuery
            {
                Name = "Front Camera",
                Url = "localhost:5000",
                Type = "mjpeg"
            };
            var robotQuery = new CreateRobotQuery
            {
                Name = "",
                IsarId = "",
                SerialNumber = "",
                VideoStreams = new List<CreateVideoStreamQuery>
                {
                    videoStreamQuery
                },
                CurrentInstallationCode = installation.InstallationCode,
                RobotType = RobotType.Robot,
                Host = "",
                Port = 1,
                Enabled = true,
                Status = RobotStatus.Available
            };

            var robot = new Robot(robotQuery, installation);
            var robotModel = _context.RobotModels.First();
            robot.Model = robotModel;

            await robotService.Create(robot);
            var robotsAfter = await robotService.ReadAll();
            int nRobotsAfter = robotsAfter.Count();

            Assert.Equal(nRobotsBefore + 1, nRobotsAfter);
        }
    }
}
