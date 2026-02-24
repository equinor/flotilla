using System;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;

namespace Api.Test.Database
{
    public class DatabaseUtilities(
        IMissionRunService _missionRunService,
        ISourceService _sourceService,
        IMissionDefinitionService _missionDefinitionService,
        IInstallationService _installationService,
        IPlantService _plantService,
        IInspectionAreaService _inspectionAreaService,
        IRobotService _robotService
    )
    {
        private readonly string _testInstallationCode = "InstCode";
        private readonly string _testInstallationName = "Installation";
        private readonly string _testPlantCode = "PlantCode";
        private readonly string _testInspectionAreaName = "InspectionArea";

        public async Task<MissionRun> NewMissionRun(
            string installationCode,
            Robot robot,
            InspectionArea inspectionArea,
            bool writeToDatabase = false,
            MissionStatus missionStatus = MissionStatus.Queued,
            MissionTask[] tasks = null!
        )
        {
            tasks ??= [];

            var missionDefinition = await NewMissionDefinition(
                null,
                installationCode,
                inspectionArea,
                null,
                writeToDatabase
            );

            var missionRun = new MissionRun
            {
                Name = "testMission",
                Robot = robot,
                MissionId = missionDefinition.Id,
                Status = missionStatus,
                CreationTime = DateTime.UtcNow,
                InspectionArea = inspectionArea,
                Tasks = tasks,
                InstallationCode = installationCode,
            };

            missionRun.Tasks = [new(new Pose())];
            if (writeToDatabase)
            {
                missionRun = await _missionRunService.Create(missionRun);
                await _robotService.UpdateCurrentMissionId(robot.Id, missionRun.Id);
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
                    SchedulingTimesCETperWeek = [new TimeAndDay(dateNow.DayOfWeek, timeOfDay)],
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
            AreaPolygon? areaPolygon = null
        )
        {
            if (string.IsNullOrEmpty(inspectionAreaName))
                inspectionAreaName = _testInspectionAreaName;
            var createInspectionAreaQuery = new CreateInspectionAreaQuery
            {
                InstallationCode = installationCode,
                PlantCode = plantCode,
                Name = inspectionAreaName,
                AreaPolygon = areaPolygon,
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

            var robot = new Robot(createRobotQuery, installation, inspectionAreaId);
            return await _robotService.Create(robot);
        }

        public async Task<Source> NewSource(string sourceId = "TestId")
        {
            return await _sourceService.Create(new Source { SourceId = sourceId });
        }
    }
}
