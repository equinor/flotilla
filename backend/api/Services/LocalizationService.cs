using System.Diagnostics;
using Api.Database.Models;
using Api.Utilities;
namespace Api.Services
{
    public interface ILocalizationService
    {
        public Task<MissionRun> CreateLocalizationMissionInArea(string robotId, string areaId);
        public Task EnsureRobotIsOnSameInstallationAsMission(Robot robot, MissionDefinition missionDefinition);
        public Task<bool> RobotIsLocalized(string robotId);
        public Task<bool> RobotIsOnSameDeckAsMission(string robotId, string areaId);
    }

    public class LocalizationService(ILogger<LocalizationService> logger, IRobotService robotService, IMissionRunService missionRunService, IInstallationService installationService, IAreaService areaService, IMapService mapService) : ILocalizationService
    {

        public async Task EnsureRobotIsOnSameInstallationAsMission(Robot robot, MissionDefinition missionDefinition)
        {
            var missionInstallation = await installationService.ReadByName(missionDefinition.InstallationCode);

            if (missionInstallation is null)
            {
                string errorMessage = $"Could not find installation for installation code {missionDefinition.InstallationCode}";
                logger.LogError("{Message}", errorMessage);
                throw new InstallationNotFoundException(errorMessage);
            }

            if (robot.CurrentInstallation.Id != missionInstallation.Id)
            {
                string errorMessage = $"The robot {robot.Name} is on installation {robot.CurrentInstallation.Name} which is not the same as the mission installation {missionInstallation.Name}";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotInSameInstallationAsMissionException(errorMessage);
            }
        }

        public async Task<bool> RobotIsLocalized(string robotId)
        {
            var robot = await robotService.ReadById(robotId);
            if (robot is null)
            {
                string errorMessage = $"Robot with ID: {robotId} was not found in the database";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            return robot.CurrentArea is not null;
        }

        public async Task<MissionRun> CreateLocalizationMissionInArea(string robotId, string areaId)
        {
            var robot = await robotService.ReadById(robotId);
            if (robot is null)
            {
                string errorMessage = $"The robot with ID {robotId} was not found";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            var area = await areaService.ReadById(areaId);
            if (area is null)
            {
                string errorMessage = $"The area with ID {areaId} was not found";
                logger.LogError("{Message}", errorMessage);
                throw new AreaNotFoundException(errorMessage);
            }

            if (area.Deck?.DefaultLocalizationPose?.Pose is null)
            {
                const string ErrorMessage = "The mission area is not associated with any deck or that deck does not have a localization pose";
                logger.LogError("{Message}", ErrorMessage);
                throw new DeckNotFoundException(ErrorMessage);
            }
            if (robot.Status is not RobotStatus.Available)
            {
                string errorMessage = $"Robot '{robot.Id}' is not available as the status is {robot.Status}";
                logger.LogWarning("{Message}", errorMessage);
                throw new RobotNotAvailableException(errorMessage);
            }

            var localizationMissionRun = new MissionRun
            {
                Name = "Localization mission",
                Robot = robot,
                MissionRunPriority = MissionRunPriority.Localization,
                InstallationCode = area.Installation.InstallationCode,
                Area = area,
                Status = MissionStatus.Pending,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = new List<MissionTask>
                {
                    new(area.Deck.DefaultLocalizationPose.Pose, MissionTaskType.Localization)
                },
                Map = new MapMetadata()
            };
            await mapService.AssignMapToMission(localizationMissionRun);

            logger.LogWarning("Starting localization mission");
            await missionRunService.Create(localizationMissionRun, triggerCreatedMissionRunEvent: false);
            return localizationMissionRun;
        }

        public async Task<bool> RobotIsOnSameDeckAsMission(string robotId, string areaId)
        {
            var robot = await robotService.ReadById(robotId);
            if (robot is null)
            {
                string errorMessage = $"The robot with ID {robotId} was not found";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            if (robot.CurrentArea is null)
            {
                const string ErrorMessage = "The robot is not associated with an area and a mission may not be started";
                logger.LogError("{Message}", ErrorMessage);
                throw new RobotCurrentAreaMissingException(ErrorMessage);
            }

            var missionArea = await areaService.ReadById(areaId);
            if (missionArea is null)
            {
                const string ErrorMessage = "The robot is not located on the same deck as the mission as the area has not been set";
                logger.LogError("{Message}", ErrorMessage);
                throw new AreaNotFoundException(ErrorMessage);
            }

            if (robot.CurrentArea?.Deck is null)
            {
                const string ErrorMessage = "The robot area is not associated with any deck";
                logger.LogError("{Message}", ErrorMessage);
                throw new DeckNotFoundException(ErrorMessage);
            }
            if (missionArea.Deck is null)
            {
                const string ErrorMessage = "The mission area is not associated with any deck";
                logger.LogError("{Message}", ErrorMessage);
                throw new DeckNotFoundException(ErrorMessage);
            }

            return robot.CurrentArea.Deck.Id == missionArea.Deck.Id;
        }
    }
}
