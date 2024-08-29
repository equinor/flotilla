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
    public class DatabaseUtilities
    {
        private readonly AccessRoleService _accessRoleService;
        private readonly MissionTaskService _missionTaskService;
        private readonly AreaService _areaService;
        private readonly DeckService _deckService;
        private readonly InstallationService _installationService;
        private readonly MissionRunService _missionRunService;
        private readonly PlantService _plantService;
        private readonly RobotModelService _robotModelService;
        private readonly RobotService _robotService;
        private readonly UserInfoService _userInfoService;

        public DatabaseUtilities(FlotillaDbContext context)
        {
            var defaultLocalizationPoseService = new DefaultLocalizationPoseService(context);

            _accessRoleService = new AccessRoleService(context, new HttpContextAccessor());
            _installationService = new InstallationService(context, _accessRoleService);
            _missionTaskService = new MissionTaskService(context, new Mock<ILogger<MissionTaskService>>().Object);
            _plantService = new PlantService(context, _installationService, _accessRoleService);
            _deckService = new DeckService(context, defaultLocalizationPoseService, _installationService, _plantService, _accessRoleService, new MockSignalRService());
            _areaService = new AreaService(context, _installationService, _plantService, _deckService, defaultLocalizationPoseService, _accessRoleService);
            _userInfoService = new UserInfoService(context, new HttpContextAccessor(), new Mock<ILogger<UserInfoService>>().Object);
            _robotModelService = new RobotModelService(context);
            _robotService = new RobotService(context, new Mock<ILogger<RobotService>>().Object, _robotModelService, new MockSignalRService(), _accessRoleService, _installationService, _areaService);
            _missionRunService = new MissionRunService(context, new MockSignalRService(), new Mock<ILogger<MissionRunService>>().Object, _accessRoleService, _missionTaskService, _areaService, _robotService, _userInfoService);
        }

        public async Task<MissionRun> NewMissionRun(
            string installationCode,
            Robot robot,
            Area area,
            bool writeToDatabase = false,
            MissionRunType missionRunType = MissionRunType.Normal,
            MissionStatus missionStatus = MissionStatus.Pending,
            string? isarMissionId = null,
            Api.Database.Models.TaskStatus taskStatus = Api.Database.Models.TaskStatus.Successful
        )
        {
            var missionRun = new MissionRun
            {
                Name = "testMission",
                Robot = robot,
                MissionId = null,
                IsarMissionId = isarMissionId,
                MissionRunType = missionRunType,
                Status = missionStatus,
                DesiredStartTime = DateTime.Now,
                Area = area,
                Tasks = [],
                Map = new MapMetadata(),
                InstallationCode = installationCode
            };
            if (missionRunType == MissionRunType.Localization)
            {
                missionRun.Tasks = new List<MissionTask>
                {
                    new(new Pose(), MissionTaskType.Localization)
                };
                missionRun.Tasks[0].Status = taskStatus;
            }
            else if (missionRunType == MissionRunType.ReturnHome)
            {
                missionRun.Tasks = new List<MissionTask>
                {
                    new(new Pose(), MissionTaskType.ReturnHome)
                };
            }
            else
            {
                missionRun.Tasks = new List<MissionTask>
                {
                    new(new Pose(), MissionTaskType.Inspection)
                };
            }
            if (writeToDatabase)
            {
                return await _missionRunService.Create(missionRun, false);
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

        public async Task<Deck> NewDeck(string installationCode, string plantCode, string deckName = "testDeck")
        {
            var createDeckQuery = new CreateDeckQuery
            {
                InstallationCode = installationCode,
                PlantCode = plantCode,
                Name = deckName,
                DefaultLocalizationPose = new CreateDefaultLocalizationPose()
                {
                    Pose = new Pose()
                }
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
                Documentation = new List<CreateDocumentationQuery>(),
                Host = "localhost",
                Port = 3000,
                Status = status,
                RobotCapabilities = [RobotCapabilitiesEnum.drive_to_pose, RobotCapabilitiesEnum.take_image, RobotCapabilitiesEnum.return_to_home, RobotCapabilitiesEnum.localize]
            };

            var robotModel = await _robotModelService.ReadByRobotType(createRobotQuery.RobotType, readOnly: true);
            var robot = new Robot(createRobotQuery, installation, robotModel!, area);
            return await _robotService.Create(robot);
        }
    }
}
