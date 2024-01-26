using System;
using System.Collections.Generic;
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
    public class RobotServiceTest : IAsyncLifetime
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
        private readonly DatabaseUtilities _databaseUtilities;

        private readonly IRobotService _robotService;

        private readonly Func<Task> _resetDatabase;

        public RobotServiceTest(DatabaseFixture fixture)
        {
            _context = fixture.Context;
            _resetDatabase = fixture.ResetDatabase;
            _logger = new Mock<ILogger<RobotService>>().Object;
            _robotModelService = new RobotModelService(_context);
            _signalRService = new MockSignalRService();
            _accessRoleService = new AccessRoleService(_context, new HttpContextAccessor());
            _installationService = new InstallationService(_context, _accessRoleService);
            _plantService = new PlantService(_context, _installationService, _accessRoleService);
            _defaultLocalizationPoseService = new DefaultLocalizationPoseService(_context);
            _deckService = new DeckService(_context, _defaultLocalizationPoseService, _installationService, _plantService, _accessRoleService);
            _areaService = new AreaService(_context, _installationService, _plantService, _deckService, _defaultLocalizationPoseService, _accessRoleService);
            _missionRunService = new MissionRunService(_context, _signalRService, new Mock<ILogger<MissionRunService>>().Object, _accessRoleService);
            _databaseUtilities = new DatabaseUtilities(_context);

            _robotService = new RobotService(_context, _logger, _robotModelService, _signalRService, _accessRoleService, _installationService, _areaService, _missionRunService);
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await _resetDatabase();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task CheckThatReadAllRobotsReturnCorrectNumberOfRobots()
        {
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robotOne = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);
            var robotTwo = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);

            var robotService = new RobotService(_context, _logger, _robotModelService, _signalRService, _accessRoleService, _installationService, _areaService, _missionRunService);
            var robots = await robotService.ReadAll();

            Assert.Equal(2, robots.Count());
        }

        [Fact]
        public async Task CheckThatReadOfSpecificRobotWorks()
        {
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);

            var robotService = new RobotService(_context, _logger, _robotModelService, _signalRService, _accessRoleService, _installationService, _areaService, _missionRunService);
            var robotById = await robotService.ReadById(robot.Id);

            Assert.Equal(robot, robotById);
        }

        [Fact]
        public async Task CheckThatNullIsReturnedWhenInvalidIdIsProvided()
        {
            var robotService = new RobotService(_context, _logger, _robotModelService, _signalRService, _accessRoleService, _installationService, _areaService, _missionRunService);
            var robot = await robotService.ReadById("invalid_id");
            Assert.Null(robot);
        }

        [Fact]
        public async Task CheckThatRobotIsCreated()
        {
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);

            var robotService = new RobotService(_context, _logger, _robotModelService, _signalRService, _accessRoleService, _installationService, _areaService, _missionRunService);

            var robotFromDatabase = await robotService.ReadById(robot.Id);
            Assert.Equal(robot, robotFromDatabase);
        }

        [Fact]
        public async Task TestThatRobotStatusIsCorrectlyUpdated()
        {
            // Arrange
            var installation = await _databaseUtilities.NewInstallation();
            var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);

            // Act
            robot.Status = RobotStatus.Busy;
            await _robotService.UpdateRobotStatus(robot.Id, RobotStatus.Busy);

            // Assert
            var postTestRobot = await _robotService.ReadById(robot.Id);
            Assert.Equal(RobotStatus.Busy, postTestRobot!.Status);
        }
    }
}
