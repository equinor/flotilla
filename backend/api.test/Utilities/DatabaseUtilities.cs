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
namespace Api.Test.Utilities
{
    public class DatabaseUtilities : IDisposable
    {
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

            _installationService = new InstallationService(context);
            _plantService = new PlantService(context, _installationService);
            _deckService = new DeckService(context, defaultLocalizationPoseService, _installationService, _plantService);
            _areaService = new AreaService(context, _installationService, _plantService, _deckService, defaultLocalizationPoseService);
            _missionRunService = new MissionRunService(context, new MockSignalRService(), new Mock<ILogger<MissionRunService>>().Object);
            _robotModelService = new RobotModelService(context);
            _robotService = new RobotService(context, new Mock<ILogger<RobotService>>().Object, _robotModelService, new MockSignalRService());
        }

        public void Dispose()
        {
            _robotService.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task<MissionRun> NewMissionRun(
            string installationCode,
            Robot robot,
            Area? area,
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

        public async Task<Robot> NewRobot(RobotStatus status, Area area)
        {
            var createRobotQuery = new CreateRobotQuery
            {
                Name = "TestBot",
                IsarId = Guid.NewGuid().ToString(),
                RobotType = RobotType.Robot,
                SerialNumber = "0001",
                CurrentInstallation = "kaa",
                CurrentArea = area,
                VideoStreams = new List<CreateVideoStreamQuery>(),
                Host = "localhost",
                Port = 3000,
                Enabled = true,
                Status = status
            };

            var robotModel = await _robotModelService.ReadByRobotType(createRobotQuery.RobotType);
            var robot = new Robot(createRobotQuery)
            {
                Model = robotModel!
            };
            return await _robotService.Create(robot);
        }

        public async Task VerifyNonDuplicateAreaDbNames(string installationCode, string plantCode, string deckName, string areaName)
        {
            var areaResponses = (await _areaService.ReadAll()).ToList();
            Assert.True(areaResponses != null);
            Assert.False(areaResponses.Where((a) => a.Name == areaName).Any(), $"Duplicate area name detected: {areaName}");

            var deckResponses = (await _deckService.ReadAll()).ToList();
            Assert.True(deckResponses != null);
            Assert.False(deckResponses.Where((d) => d.Name == deckName).Any(), $"Duplicate deck name detected: {deckName}");

            var plantResponses = (await _plantService.ReadAll()).ToList();
            Assert.True(plantResponses != null);
            Assert.False(plantResponses.Where((p) => p.PlantCode == plantCode).Any(), $"Duplicate plant code detected: {plantCode}");

            var installationResponses = (await _installationService.ReadAll()).ToList();
            Assert.True(installationResponses != null);
            Assert.False(installationResponses.Where((i) => i.InstallationCode == installationCode).Any(), $"Duplicate installation name detected: {installationCode}");
        }

        public async Task<(string installationId, string plantId, string deckId, string areaId)> PostAssetInformationToDb(string installationCode, string plantCode, string deckName, string areaName)
        {
            await VerifyNonDuplicateAreaDbNames(installationCode, plantCode, deckName, areaName);

            var testPose = new Pose
            {
                Position = new Position
                {
                    X = 1,
                    Y = 2,
                    Z = 2
                },
                Orientation = new Orientation
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                    W = 1
                }
            };

            var installationQuery = new CreateInstallationQuery
            {
                InstallationCode = installationCode,
                Name = installationCode
            };

            var plantQuery = new CreatePlantQuery
            {
                InstallationCode = installationCode,
                PlantCode = plantCode,
                Name = plantCode
            };

            var deckQuery = new CreateDeckQuery
            {
                InstallationCode = installationCode,
                PlantCode = plantCode,
                Name = deckName
            };

            var areaQuery = new CreateAreaQuery
            {
                InstallationCode = installationCode,
                PlantCode = plantCode,
                DeckName = deckName,
                AreaName = areaName,
                DefaultLocalizationPose = testPose
            };

            string installationId = (await _installationService.Create(installationQuery)).Id;
            string plantId = (await _plantService.Create(plantQuery)).Id;
            string deckId = (await _deckService.Create(deckQuery)).Id;
            string areaId = (await _areaService.Create(areaQuery)).Id;

            return (installationId, plantId, deckId, areaId);
        }
    }
}
