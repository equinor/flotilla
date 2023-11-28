using System.Diagnostics;
using Api.Database.Models;
using Api.Utilities;
namespace Api.Services
{
    public interface ILocalizationService
    {
        public Task<string?> EnsureRobotIsCorrectlyLocalized(Robot robot, MissionRun missionRun);

        public Task EnsureRobotIsOnSameInstallationAsMission(Robot robot, MissionDefinition missionDefinition);

        public Task EnsureRobotWasCorrectlyLocalizedInPreviousMissionRun(string robotId);

        public Task<bool> RobotIsLocalized(string robotId);
    }

    public class LocalizationService : ILocalizationService
    {
        private readonly IInstallationService _installationService;
        private readonly ILogger<LocalizationService> _logger;
        private readonly IMissionRunService _missionRunService;
        private readonly IRobotService _robotService;
        private readonly IAreaService _areaService;

        public LocalizationService(ILogger<LocalizationService> logger, IRobotService robotService, IMissionRunService missionRunService, IInstallationService installationService, IAreaService areaService)
        {
            _logger = logger;
            _robotService = robotService;
            _missionRunService = missionRunService;
            _installationService = installationService;
            _areaService = areaService;
        }

        public async Task EnsureRobotIsOnSameInstallationAsMission(Robot robot, MissionDefinition missionDefinition)
        {
            var missionInstallation = await _installationService.ReadByName(missionDefinition.InstallationCode);
            var robotInstallation = await _installationService.ReadByName(robot.CurrentInstallation);

            if (missionInstallation is null || robotInstallation is null)
            {
                string errorMessage = $"Could not find installation for installation code {missionDefinition.InstallationCode} or the robot has no current installation";
                _logger.LogError("{Message}", errorMessage);
                throw new InstallationNotFoundException(errorMessage);
            }

            if (robotInstallation != missionInstallation)
            {
                string errorMessage = $"The robot {robot.Name} is on installation {robotInstallation.Name} which is not the same as the mission installation {missionInstallation.Name}";
                _logger.LogError("{Message}", errorMessage);
                throw new MissionException(errorMessage);
            }
        }

        public async Task<string?> EnsureRobotIsCorrectlyLocalized(Robot robot, MissionRun missionRun)
        {
            if (missionRun.Area is null)
            {
                string errorMessage = $"There was no area associated with mission run {missionRun.Id}";
                _logger.LogError("{Message}", errorMessage);
                throw new AreaNotFoundException(errorMessage);
            }

            string? localizationMissionRunId = null;

            if (!await RobotIsLocalized(robot.Id)) { localizationMissionRunId = await StartLocalizationMissionInArea(robot.Id, missionRun.Area.Id); }

            if (!await RobotIsOnSameDeckAsMission(robot.Id, missionRun.Area.Id))
            {
                string errorMessage = $"The new mission run {missionRun.Id} will not be started as the robot is not localized on the same deck as the mission";
                _logger.LogError("{Message}", errorMessage);
                throw new RobotLocalizationException(errorMessage);
            }

            _logger.LogWarning("{Message}", $"Localization mission run ID is {localizationMissionRunId}");
            
            return localizationMissionRunId;
        }

        public async Task<bool> RobotIsLocalized(string robotId)
        {
            var robot = await _robotService.ReadById(robotId);
            if (robot is null)
            {
                string errorMessage = $"Robot with ID: {robotId} was not found in the database";
                _logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            return robot.CurrentArea is not null;
        }

        public async Task EnsureRobotWasCorrectlyLocalizedInPreviousMissionRun(string robotId)
        {
            var robot = await _robotService.ReadById(robotId);
            if (robot == null)
            {
                string errorMessage = $"Robot with ID: {robotId} was not found in the database";
                _logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            if (await _missionRunService.OngoingMission(robot.Id)) { await WaitForLocalizationMissionStatusToBeUpdated(robot); }

            var lastExecutedMissionRun = await _missionRunService.ReadLastExecutedMissionRunByRobot(robot.Id);
            if (lastExecutedMissionRun is null)
            {
                string errorMessage = $"Could not find last executed mission run for robot with ID {robot.Id}";
                _logger.LogError("{Message}", errorMessage);
                throw new MissionNotFoundException(errorMessage);
            }

            if (lastExecutedMissionRun.Status != MissionStatus.Successful)
            {
                string errorMessage =
                    $"The localization mission {lastExecutedMissionRun.Id} failed and thus subsequent scheduled missions for deck {lastExecutedMissionRun.Area?.Deck} wil be cancelled";
                _logger.LogError("{Message}", errorMessage);
                throw new LocalizationFailedException(errorMessage);
            }

            await _robotService.UpdateCurrentArea(robot.Id, lastExecutedMissionRun.Area);
        }

        private async Task WaitForLocalizationMissionStatusToBeUpdated(Robot robot)
        {
            if (robot.CurrentMissionId is null)
            {
                string errorMessage = $"Could not find current mission for robot {robot.Id}";
                _logger.LogError("{Message}", errorMessage);
                throw new MissionNotFoundException(errorMessage);
            }

            string ongoingMissionRunId = robot.CurrentMissionId;
            var ongoingMissionRun = await _missionRunService.ReadById(robot.CurrentMissionId);
            if (ongoingMissionRun is null)
            {
                string errorMessage = $"Could not find ongoing mission with ID {robot.CurrentMissionId}";
                _logger.LogError("{Message}", errorMessage);
                throw new MissionNotFoundException(errorMessage);
            }

            if (!ongoingMissionRun.IsLocalizationMission())
            {
                string errorMessage = $"The currently executing mission for robot {robot.CurrentMissionId} is not a localization mission";
                _logger.LogError("{Message}", errorMessage);
                throw new MissionException(errorMessage);
            }

            _logger.LogWarning(
                "The RobotAvailable event was triggered before the OnMissionUpdate event and we have to wait to see that the localization mission is set to successful");

            const int Timeout = 5;
            var timer = new Stopwatch();
            ongoingMissionRun = await _missionRunService.ReadById(ongoingMissionRunId);

            timer.Start();
            while (timer.Elapsed.TotalSeconds < Timeout)
            {
                if (ongoingMissionRun is null) { continue; }
                if (ongoingMissionRun.Status == MissionStatus.Successful) { return; }

                ongoingMissionRun = await _missionRunService.ReadById(ongoingMissionRunId);
            }

            const string Message = "Timed out while waiting for the localization mission to get an updated status";
            _logger.LogError("{Message}", Message);
            throw new TimeoutException(Message);
        }

        private async Task<string> StartLocalizationMissionInArea(string robotId, string areaId)
        {
            var robot = await _robotService.ReadById(robotId);
            if (robot is null){
                string errorMessage = $"The robot with ID {robotId} was not found";
                _logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            var area = await _areaService.ReadById(areaId);
            if (area is null){
                string errorMessage = $"The area with ID {areaId} was not found";
                _logger.LogError("{Message}", errorMessage);
                throw new AreaNotFoundException(errorMessage);
            }

            if (area.Deck?.DefaultLocalizationPose?.Pose is null)
            {
                const string ErrorMessage = "The mission area is not associated with any deck or that deck does not have a localization pose";
                _logger.LogError("{Message}", ErrorMessage);
                throw new DeckNotFoundException(ErrorMessage);
            }
            if (robot.Status is not RobotStatus.Available)
            {
                string errorMessage = $"Robot '{robot.Id}' is not available as the status is {robot.Status.ToString()}";
                _logger.LogWarning("{Message}", errorMessage);
                throw new RobotNotAvailableException(errorMessage);
            }

            var localizationMissionRun = new MissionRun
            {
                Name = "Localization mission",
                Robot = robot,
                InstallationCode = area.Installation.InstallationCode,
                Area = area,
                Status = MissionStatus.Pending,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = new List<MissionTask>
                {
                    new(area.Deck.DefaultLocalizationPose.Pose, "localization")
                },
                Map = new MapMetadata()
            };
            _logger.LogWarning("Starting localization mission");
            await _missionRunService.Create(localizationMissionRun, triggerCreatedMissionRunEvent: false);
            await _robotService.UpdateCurrentArea(robot.Id, localizationMissionRun.Area);
            return localizationMissionRun.Id;
        }

        private async Task<bool> RobotIsOnSameDeckAsMission(string robotId, string areaId)
        {
            var robot = await _robotService.ReadById(robotId);
            if (robot is null){
                string errorMessage = $"The robot with ID {robotId} was not found";
                _logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            if (robot.CurrentArea is null)
            {
                const string ErrorMessage = "The robot is not associated with an area and a mission may not be started";
                _logger.LogError("{Message}", ErrorMessage);
                throw new AreaNotFoundException(ErrorMessage);
            }

            var missionArea = await _areaService.ReadById(areaId);
            if (missionArea is null)
            {
                const string ErrorMessage = "The robot is not located on the same deck as the mission as the area has not been set";
                _logger.LogError("{Message}", ErrorMessage);
                throw new AreaNotFoundException(ErrorMessage);
            }

            if (robot.CurrentArea?.Deck is null)
            {
                const string ErrorMessage = "The robot area is not associated with any deck";
                _logger.LogError("{Message}", ErrorMessage);
                throw new DeckNotFoundException(ErrorMessage);
            }
            if (missionArea.Deck is null)
            {
                const string ErrorMessage = "The mission area is not associated with any deck";
                _logger.LogError("{Message}", ErrorMessage);
                throw new DeckNotFoundException(ErrorMessage);
            }

            return robot.CurrentArea.Deck == missionArea.Deck;
        }
    }
}
