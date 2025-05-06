using System;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.HostedServices;
using Api.Services;
using Api.Services.Models;
using Api.Test.Mocks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Api.Test.Database
{
    public class DatabaseUtilities
    {
        private readonly AccessRoleService _accessRoleService;
        private readonly MissionTaskService _missionTaskService;
        private readonly InspectionAreaService _inspectionAreaService;
        private readonly InstallationService _installationService;
        private readonly MissionRunService _missionRunService;
        private readonly MissionDefinitionService _missionDefinitionService;
        private readonly PlantService _plantService;
        private readonly RobotModelService _robotModelService;
        private readonly RobotService _robotService;
        private readonly UserInfoService _userInfoService;
        private readonly SourceService _sourceService;
        private readonly AutoScheduleService _autoScheduleService;
        private readonly string _testInstallationCode = "InstCode";
        private readonly string _testInstallationName = "Installation";
        private readonly string _testPlantCode = "PlantCode";
        private readonly string _testInspectionAreaName = "InspectionArea";

        public DatabaseUtilities(FlotillaDbContext context)
        {
            _accessRoleService = new AccessRoleService(context, new HttpContextAccessor());
            _installationService = new InstallationService(
                context,
                _accessRoleService,
                new Mock<ILogger<InstallationService>>().Object
            );
            _missionTaskService = new MissionTaskService(
                context,
                new Mock<ILogger<MissionTaskService>>().Object
            );
            _plantService = new PlantService(context, _installationService, _accessRoleService);
            _inspectionAreaService = new InspectionAreaService(
                context,
                _installationService,
                _plantService,
                _accessRoleService,
                new MockSignalRService(),
                new Mock<ILogger<InspectionAreaService>>().Object
            );
            _userInfoService = new UserInfoService(
                context,
                new HttpContextAccessor(),
                new Mock<ILogger<UserInfoService>>().Object
            );
            _robotModelService = new RobotModelService(context);
            _robotService = new RobotService(
                context,
                new Mock<ILogger<RobotService>>().Object,
                _robotModelService,
                new MockSignalRService(),
                _accessRoleService,
                _installationService,
                _inspectionAreaService
            );
            _missionRunService = new MissionRunService(
                context,
                new MockSignalRService(),
                new Mock<ILogger<MissionRunService>>().Object,
                _accessRoleService,
                _missionTaskService,
                _inspectionAreaService,
                _robotService,
                _userInfoService
            );
            _sourceService = new SourceService(context, new Mock<ILogger<SourceService>>().Object);
            _autoScheduleService = new AutoScheduleService(
                new Mock<ILogger<AutoScheduleService>>().Object,
                new MockSignalRService()
            );
            _missionDefinitionService = new MissionDefinitionService(
                context,
                new MockSignalRService(),
                _accessRoleService,
                new Mock<ILogger<MissionDefinitionService>>().Object,
                _missionRunService,
                _sourceService,
                _autoScheduleService
            );
        }

        public async Task<MissionRun> NewMissionRun(
            string installationCode,
            Robot robot,
            InspectionArea inspectionArea,
            bool writeToDatabase = false,
            MissionRunType missionRunType = MissionRunType.Normal,
            MissionStatus missionStatus = MissionStatus.Pending,
            string isarMissionId = "",
            MissionTask[] tasks = null!
        )
        {
            tasks ??= [];

            if (string.IsNullOrEmpty(isarMissionId))
                isarMissionId = Guid.NewGuid().ToString();
            var missionRun = new MissionRun
            {
                Name = "testMission",
                Robot = robot,
                MissionId = null,
                IsarMissionId = isarMissionId,
                MissionRunType = missionRunType,
                Status = missionStatus,
                DesiredStartTime = DateTime.UtcNow,
                InspectionArea = inspectionArea,
                Tasks = tasks,
                InstallationCode = installationCode,
            };

            if (missionRunType == MissionRunType.ReturnHome)
            {
                missionRun.Tasks = [new(new Pose(), MissionTaskType.ReturnHome)];
            }
            else
            {
                missionRun.Tasks = [new(new Pose(), MissionTaskType.Inspection)];
            }
            if (writeToDatabase)
            {
                return await _missionRunService.Create(missionRun, false);
            }
            return missionRun;
        }

        public async Task<MissionDefinition> NewMissionDefinition(
            string? id,
            string installationCode,
            InspectionArea inspectionArea,
            MissionRun? lastSuccessfulRun = null,
            bool writeToDatabase = false
        )
        {
            var dateNow = DateTime.UtcNow;
            var timeOfDay = TimeOnly.FromDateTime(dateNow).Add(new TimeSpan(3, 0, 0));

            if (string.IsNullOrEmpty(id))
                id = Guid.NewGuid().ToString();

            var source = await _sourceService.Create(new Source { SourceId = $"{id}" });
            var missionDefinition = new MissionDefinition
            {
                Id = id,
                Name = "testMissionDefinition",
                InspectionArea = inspectionArea,
                InstallationCode = installationCode,
                Source = source,
                InspectionFrequency = new DateTime().AddDays(7) - new DateTime(),
                LastSuccessfulRun = lastSuccessfulRun,
                AutoScheduleFrequency = new AutoScheduleFrequency
                {
                    TimesOfDayCET = [timeOfDay],
                    DaysOfWeek = [dateNow.DayOfWeek],
                },
            };

            if (writeToDatabase)
            {
                return await _missionDefinitionService.Create(missionDefinition);
            }
            return missionDefinition;
        }

        public async Task<Installation> NewInstallation(string installationCode = "")
        {
            if (string.IsNullOrEmpty(installationCode))
                installationCode = _testInstallationCode;
            var createInstallationQuery = new CreateInstallationQuery
            {
                InstallationCode = installationCode,
                Name = _testInstallationName,
            };
            return await _installationService.Create(createInstallationQuery);
        }

        public async Task<Plant> NewPlant(string installationCode)
        {
            var createPlantQuery = new CreatePlantQuery
            {
                InstallationCode = installationCode,
                PlantCode = _testPlantCode,
                Name = "testPlant",
            };

            return await _plantService.Create(createPlantQuery);
        }

        public async Task<InspectionArea> NewInspectionArea(
            string installationCode,
            string plantCode,
            string inspectionAreaName = "",
            InspectionAreaPolygon? areaPolygonJson = null
        )
        {
            if (string.IsNullOrEmpty(inspectionAreaName))
                inspectionAreaName = _testInspectionAreaName;
            var createInspectionAreaQuery = new CreateInspectionAreaQuery
            {
                InstallationCode = installationCode,
                PlantCode = plantCode,
                Name = inspectionAreaName,
                AreaPolygonJson = areaPolygonJson,
            };

            return await _inspectionAreaService.Create(createInspectionAreaQuery);
        }

        public async Task<Robot> NewRobot(
            RobotStatus status,
            Installation installation,
            string? inspectionAreaId = null
        )
        {
            var createRobotQuery = new CreateRobotQuery
            {
                Name = Guid.NewGuid().ToString(),
                IsarId = Guid.NewGuid().ToString(),
                RobotType = RobotType.Robot,
                SerialNumber = "0001",
                CurrentInstallationCode = installation.InstallationCode,
                Documentation = [],
                Host = "localhost",
                Port = 3000,
                Status = status,
                RobotCapabilities = [RobotCapabilitiesEnum.take_image],
            };

            var robotModel = await _robotModelService.ReadByRobotType(
                createRobotQuery.RobotType,
                readOnly: true
            );
            var robot = new Robot(createRobotQuery, installation, robotModel!, inspectionAreaId);
            return await _robotService.Create(robot);
        }

        public async Task<Source> NewSource(string sourceId = "TestId")
        {
            return await _sourceService.Create(new Source { SourceId = sourceId });
        }
    }
}
