using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
namespace Api.Test.Database
{
    public class DatabaseUtilities : IDisposable
    {
        private readonly AccessRoleService _accessRoleService;
        private readonly AreaService _areaService;
        private readonly DeckService _deckService;
        private readonly InstallationService _installationService;
        private readonly MissionRunService _missionRunService;
        private readonly PlantService _plantService;
        private readonly RobotModelService _robotModelService;
        private readonly RobotService _robotService;

        public DatabaseUtilities(FlotillaDbContext context)
        {
            var defaultLocalizationPoseService = new DefaultLocalizationPoseService(context);

            _accessRoleService = new AccessRoleService(context, new HttpContextAccessor());
            _installationService = new InstallationService(context, _accessRoleService);
            _plantService = new PlantService(context, _installationService, _accessRoleService);
            _deckService = new DeckService(context, defaultLocalizationPoseService, _installationService, _plantService, _accessRoleService);
            _areaService = new AreaService(context, _installationService, _plantService, _deckService, defaultLocalizationPoseService, _accessRoleService);
            _missionRunService = new MissionRunService(context, new MockSignalRService(), new Mock<ILogger<MissionRunService>>().Object, _accessRoleService);
            _robotModelService = new RobotModelService(context);
            _robotService = new RobotService(context, new Mock<ILogger<RobotService>>().Object, _robotModelService, new MockSignalRService(), _accessRoleService, _installationService, _areaService, _missionRunService);
        }

        public void Dispose()
        {
            _robotService.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task<MissionRun> NewMissionRun(
            string installationCode,
            Robot robot,
            Area area,
            bool writeToDatabase = true
        )
        {
            var missionRun = new MissionRun
            {
                Name = "testMission",
                Robot = robot,
                MissionId = null,
                MissionRunPriority = MissionRunPriority.Normal,
                Status = MissionStatus.Pending,
                DesiredStartTime = DateTime.Now,
                Area = area,
                Map = new MapMetadata(),
                InstallationCode = installationCode
            };
            if (writeToDatabase)
            {
                return await _missionRunService.Create(missionRun);
            }
            return missionRun;
        }

        public async Task<Installation> NewInstallation()
        {
            var createInstallationQuery = new CreateInstallationQuery
            {
                InstallationCode = "testInstallationCode",
                Name = "testInstallation"
            };

            return await _installationService.Create(createInstallationQuery);
        }

        public async Task<Plant> NewPlant(string installationCode)
        {
            var createPlantQuery = new CreatePlantQuery
            {
                InstallationCode = installationCode,
                PlantCode = "testPlantCode",
                Name = "testPlant"
            };

            return await _plantService.Create(createPlantQuery);
        }

        public async Task<Deck> NewDeck(string installationCode, string plantCode)
        {
            var createDeckQuery = new CreateDeckQuery
            {
                InstallationCode = installationCode,
                PlantCode = plantCode,
                Name = "testDeck"
            };

            return await _deckService.Create(createDeckQuery);
        }

        public async Task<Area> NewArea(string installationCode, string plantCode, string deckName)
        {
            var createAreaQuery = new CreateAreaQuery
            {
                InstallationCode = installationCode,
                PlantCode = plantCode,
                DeckName = deckName,
                AreaName = "testArea",
                DefaultLocalizationPose = new Pose()
            };

            return await _areaService.Create(createAreaQuery);
        }

        public async Task<Robot> NewRobot(RobotStatus status, Installation installation, Area? area = null)
        {
            var createRobotQuery = new CreateRobotQuery
            {
                Name = "TestBot",
                IsarId = Guid.NewGuid().ToString(),
                RobotType = RobotType.Robot,
                SerialNumber = "0001",
                CurrentInstallationCode = installation.InstallationCode,
                CurrentAreaName = area?.Name,
                VideoStreams = new List<CreateVideoStreamQuery>(),
                Host = "localhost",
                Port = 3000,
                Enabled = true,
                Status = status
            };

            var robotModel = await _robotModelService.ReadByRobotType(createRobotQuery.RobotType);
            var robot = new Robot(createRobotQuery, installation, area)
            {
                Model = robotModel!
            };
            return await _robotService.Create(robot);
        }
    }
}
