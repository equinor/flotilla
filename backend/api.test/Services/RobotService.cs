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
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task ReadAll()
        {
            var robotService = new RobotService(_context, _logger, _robotModelService, _signalRService, _accessRoleService, _installationService, _areaService);
            var robots = await robotService.ReadAll();

            Assert.True(robots.Any());
        }

        [Fact]
        public async Task Read()
        {
            var robotService = new RobotService(_context, _logger, _robotModelService, _signalRService, _accessRoleService, _installationService, _areaService);
            var robots = await robotService.ReadAll(readOnly: false);
            var firstRobot = robots.First();
            var robotById = await robotService.ReadById(firstRobot.Id, readOnly: false);

            Assert.Equal(firstRobot, robotById); // To compare the objects directly, we need to use readOnly = false. Otherwise we will read in a new object
        }

        [Fact]
        public async Task ReadIdDoesNotExist()
        {
            var robotService = new RobotService(_context, _logger, _robotModelService, _signalRService, _accessRoleService, _installationService, _areaService);
            var robot = await robotService.ReadById("some_id_that_does_not_exist", readOnly: true);
            Assert.Null(robot);
        }

        [Fact]
        public async Task Create()
        {
            var robotService = new RobotService(_context, _logger, _robotModelService, _signalRService, _accessRoleService, _installationService, _areaService);
            var installationService = new InstallationService(_context, _accessRoleService);

            var installation = await installationService.Create(new CreateInstallationQuery
            {
                Name = "Johan Sverdrup",
                InstallationCode = "JSV"
            });

            var robotsBefore = await robotService.ReadAll(readOnly: true);
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
                Status = RobotStatus.Available
            };

            var robotModel = _context.RobotModels.First();
            var robot = new Robot(robotQuery, installation, robotModel);

            await robotService.Create(robot);
            var robotsAfter = await robotService.ReadAll(readOnly: true);
            int nRobotsAfter = robotsAfter.Count();

            Assert.Equal(nRobotsBefore + 1, nRobotsAfter);
        }
    }
}
