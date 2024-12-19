using System;
using System.Linq;
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
        private readonly IInspectionAreaService _inspectionAreaService;
        private readonly IAreaService _areaService;
        private readonly DatabaseUtilities _databaseUtilities;

        public RobotServiceTest(DatabaseFixture fixture)
        {
            _context = fixture.NewContext;
            _databaseUtilities = new DatabaseUtilities(_context);
            _logger = new Mock<ILogger<RobotService>>().Object;
            _robotModelService = new RobotModelService(_context);
            _signalRService = new MockSignalRService();
            _accessRoleService = new AccessRoleService(_context, new HttpContextAccessor());
            _installationService = new InstallationService(_context, _accessRoleService);
            _plantService = new PlantService(_context, _installationService, _accessRoleService);
            _defaultLocalizationPoseService = new DefaultLocalizationPoseService(_context);
            _inspectionAreaService = new InspectionAreaService(_context, _defaultLocalizationPoseService, _installationService, _plantService, _accessRoleService, _signalRService);
            _areaService = new AreaService(_context, _installationService, _plantService, _inspectionAreaService, _defaultLocalizationPoseService, _accessRoleService);
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task ReadAll()
        {
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var _ = await _databaseUtilities.NewRobot(RobotStatus.Available, installation);
            var robotService = new RobotService(_context, _logger, _robotModelService, _signalRService, _accessRoleService, _installationService, _inspectionAreaService);
            var robots = await robotService.ReadAll();

            Assert.True(robots.Any());
        }

        [Fact]
        public async Task Read()
        {
            var robotService = new RobotService(_context, _logger, _robotModelService, _signalRService, _accessRoleService, _installationService, _inspectionAreaService);
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation);
            var robotById = await robotService.ReadById(robot.Id, readOnly: false);
            Assert.NotNull(robotById);
            Assert.Equal(robot.Id, robotById.Id);
        }

        [Fact]
        public async Task ReadIdDoesNotExist()
        {
            var robotService = new RobotService(_context, _logger, _robotModelService, _signalRService, _accessRoleService, _installationService, _inspectionAreaService);
            var robot = await robotService.ReadById("some_id_that_does_not_exist", readOnly: true);
            Assert.Null(robot);
        }

        [Fact]
        public async Task Create()
        {
            var robotService = new RobotService(_context, _logger, _robotModelService, _signalRService, _accessRoleService, _installationService, _inspectionAreaService);
            var installationService = new InstallationService(_context, _accessRoleService);

            var installation = await installationService.Create(new CreateInstallationQuery
            {
                Name = "Johan Sverdrup",
                InstallationCode = "JSV"
            });

            var robotsBefore = await robotService.ReadAll(readOnly: true);
            int nRobotsBefore = robotsBefore.Count();
            var documentationQuery = new CreateDocumentationQuery
            {
                Name = "Some document",
                Url = "someURL",
            };
            var robotQuery = new CreateRobotQuery
            {
                Name = "",
                IsarId = "",
                SerialNumber = "",
                Documentation =
                [
                    documentationQuery
                ],
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
