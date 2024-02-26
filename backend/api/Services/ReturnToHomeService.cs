using Api.Database.Models;
using Api.Utilities;
namespace Api.Services
{
    public interface IReturnToHomeService
    {
        public Task ScheduleReturnToHomeMissionRunIfRobotIsNotHome(string robotId);
    }

    public class ReturnToHomeService(ILogger<ReturnToHomeService> logger, IRobotService robotService, IMissionRunService missionRunService, IMapService mapService) : IReturnToHomeService
    {
        public async Task ScheduleReturnToHomeMissionRunIfRobotIsNotHome(string robotId)
        {
            logger.LogInformation("Scheduling return to home mission if not home for robot {RobotId}", robotId);
            if (await IsRobotHome(robotId))
            {
                logger.LogInformation("Robot {RobotId} is home, setting current area to null", robotId);
                await robotService.UpdateCurrentArea(robotId, null);
                return;
            }

            try { await ScheduleReturnToHomeMissionRun(robotId); }
            catch (Exception ex) when (ex is RobotNotFoundException or AreaNotFoundException or DeckNotFoundException or PoseNotFoundException)
            {
                throw new ReturnToHomeMissionFailedToScheduleException(ex.Message);
            }
        }
        private async Task<bool> IsRobotHome(string robotId)
        {
            var lastExecutedMissionRun = await missionRunService.ReadLastExecutedMissionRunByRobot(robotId);
            if (lastExecutedMissionRun is null)
            {
                logger.LogInformation("Could not find last executed mission run for robot {RobotId}, can not guarantee that the robot is in its home", robotId);
                return false;
            }

            return lastExecutedMissionRun.IsDriveToMission();
        }
        private async Task<MissionRun?> ScheduleReturnToHomeMissionRun(string robotId)
        {
            var robot = await robotService.ReadById(robotId);
            if (robot is null)
            {
                string errorMessage = $"Robot with ID {robotId} could not be retrieved from the database";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            if (robot.CurrentArea is null)
            {
                string errorMessage = $"Unable to schedule a return to home mission as the robot {robot.Id} is not linked to an area";
                logger.LogError("{Message}", errorMessage);
                throw new AreaNotFoundException(errorMessage);
            }

            if (robot.CurrentArea.Deck is null)
            {
                string errorMessage = $"Unable to schedule a return to home mission as the current area {robot.CurrentArea.Id} for robot {robot.Id} is not linked to a deck";
                logger.LogError("{Message}", errorMessage);
                throw new DeckNotFoundException(errorMessage);
            }

            if (robot.CurrentArea.Deck.DefaultLocalizationPose is null)
            {
                logger.LogError(
                    "Unable to schedule a return to home mission as the current area {AreaId} for robot {RobotId} is linked to the deck {DeckId} which has no default pose",
                    robot.CurrentArea.Id, robot.Id, robot.CurrentArea.Deck.Id);
                string errorMessage =
                    $"Unable to schedule a return to home mission as the current area {robot.CurrentArea.Id} for robot {robot.Id} "
                    + $"is linked to the deck {robot.CurrentArea.Deck.Id} which has no default pose";
                logger.LogError("{Message}", errorMessage);
                throw new PoseNotFoundException(errorMessage);
            }

            var returnToHomeMissionRun = new MissionRun
            {
                Name = "Return to home mission",
                Robot = robot,
                InstallationCode = robot.CurrentInstallation.InstallationCode,
                MissionRunPriority = MissionRunPriority.Normal,
                Area = robot.CurrentArea,
                Status = MissionStatus.Pending,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = new List<MissionTask>
                {
                    new(new Pose(robot.CurrentArea.Deck.DefaultLocalizationPose.Pose), MissionTaskType.DriveTo)
                },
                Map = new MapMetadata()
            };
            await mapService.AssignMapToMission(returnToHomeMissionRun);

            var missionRun = await missionRunService.Create(returnToHomeMissionRun);
            logger.LogInformation(
                "Scheduled a mission for the robot {RobotName} to return to home location on deck {DeckName}",
                robot.Name, robot.CurrentArea.Deck.Name);
            return missionRun;
        }
    }
}
