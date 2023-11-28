using Api.Database.Models;
using Api.Utilities;
namespace Api.Services
{
    public interface IReturnToHomeService
    {
        public Task<MissionRun?> ScheduleReturnToHomeMissionRun(string robotId);
    }

    public class ReturnToHomeService : IReturnToHomeService
    {
        private readonly ILogger<ReturnToHomeService> _logger;
        private readonly IMissionRunService _missionRunService;
        private readonly IRobotService _robotService;

        public ReturnToHomeService(ILogger<ReturnToHomeService> logger, IRobotService robotService, IMissionRunService missionRunService)
        {
            _logger = logger;
            _robotService = robotService;
            _missionRunService = missionRunService;
        }

        public async Task<MissionRun?> ScheduleReturnToHomeMissionRun(string robotId)
        {
            var robot = await _robotService.ReadById(robotId);
            if (robot is null)
            {
                string errorMessage = $"Robot with ID {robotId} could not be retrieved from the database";
                _logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            if (robot.CurrentArea is null)
            {
                string errorMessage = $"Unable to schedule a return to home mission as the robot {robot.Id} is not linked to an area";
                _logger.LogError("{Message}", errorMessage);
                throw new AreaNotFoundException(errorMessage);
            }

            if (robot.CurrentArea.Deck is null)
            {
                string errorMessage = $"Unable to schedule a return to home mission as the current area {robot.CurrentArea.Id} for robot {robot.Id} is not linked to a deck";
                _logger.LogError("{Message}", errorMessage);
                throw new DeckNotFoundException(errorMessage);
            }

            if (robot.CurrentArea.Deck.DefaultLocalizationPose is null)
            {
                _logger.LogError(
                    "Unable to schedule a return to home mission as the current area {AreaId} for robot {RobotId} is linked to the deck {DeckId} which has no default pose",
                    robot.CurrentArea.Id, robot.Id, robot.CurrentArea.Deck.Id);
                string errorMessage =
                    $"Unable to schedule a return to home mission as the current area {robot.CurrentArea.Id} for robot {robot.Id} "
                    + $"is linked to the deck {robot.CurrentArea.Deck.Id} which has no default pose";
                _logger.LogError("{Message}", errorMessage);
                throw new PoseNotFoundException(errorMessage);
            }

            var returnToHomeMissionRun = new MissionRun
            {
                Name = "Return to home mission",
                Robot = robot,
                InstallationCode = robot.CurrentInstallation,
                MissionRunPriority = MissionRunPriority.Normal,
                Area = robot.CurrentArea,
                Status = MissionStatus.Pending,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = new List<MissionTask>
                {
                    new(robot.CurrentArea.Deck.DefaultLocalizationPose.Pose, "drive_to")
                },
                Map = new MapMetadata()
            };

            var missionRun = await _missionRunService.Create(returnToHomeMissionRun);
            _logger.LogInformation(
                "Scheduled a mission for the robot {RobotName} to return to home location on deck {DeckName}",
                robot.Name, robot.CurrentArea.Deck.Name);
            return missionRun;
        }
    }
}
