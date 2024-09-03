using Api.Database.Models;
using Api.Utilities;
namespace Api.Services
{
    public interface IReturnToHomeService
    {
        public Task<MissionRun?> ScheduleReturnToHomeMissionRunIfNotAlreadyScheduledOrRobotIsHome(string robotId);
        public Task<MissionRun?> GetActiveReturnToHomeMissionRun(string robotId, bool readOnly = true);
    }

    public class ReturnToHomeService(ILogger<ReturnToHomeService> logger, IRobotService robotService, IMissionRunService missionRunService, IMapService mapService) : IReturnToHomeService
    {
        public async Task<MissionRun?> ScheduleReturnToHomeMissionRunIfNotAlreadyScheduledOrRobotIsHome(string robotId)
        {
            logger.LogInformation("Scheduling return to home mission if not already scheduled or the robot is home for robot {RobotId}", robotId);

            if (await IsReturnToHomeMissionAlreadyScheduled(robotId))
            {
                logger.LogInformation("ReturnToHomeMission is already scheduled for Robot {RobotId}", robotId);
                return null;
            }

            if (await IsRobotHome(robotId))
            {
                logger.LogInformation("Robot {RobotId} is home, setting current area to null", robotId);
                await robotService.UpdateCurrentArea(robotId, null);
                return null;
            }

            MissionRun missionRun;
            try { missionRun = await ScheduleReturnToHomeMissionRun(robotId); }
            catch (Exception ex) when (ex is RobotNotFoundException or AreaNotFoundException or DeckNotFoundException or PoseNotFoundException or UnsupportedRobotCapabilityException)
            {
                // TODO: if we make ISAR aware of return to home missions, we can avoid scheduling them when the robot does not need them
                throw new ReturnToHomeMissionFailedToScheduleException(ex.Message);
            }

            return missionRun;
        }
        private async Task<bool> IsRobotHome(string robotId)
        {
            var lastExecutedMissionRun = await missionRunService.ReadLastExecutedMissionRunByRobot(robotId, readOnly: true);
            if (lastExecutedMissionRun is null)
            {
                logger.LogInformation("Could not find last executed mission run for robot {RobotId}, can not guarantee that the robot is in its home", robotId);
                return false;
            }

            return lastExecutedMissionRun.IsReturnHomeMission();
        }
        private async Task<bool> IsReturnToHomeMissionAlreadyScheduled(string robotId)
        {
            return await missionRunService.PendingOrOngoingReturnToHomeMissionRunExists(robotId);
        }
        private async Task<MissionRun> ScheduleReturnToHomeMissionRun(string robotId)
        {
            var robot = await robotService.ReadById(robotId, readOnly: true);
            if (robot is null)
            {
                string errorMessage = $"Robot with ID {robotId} could not be retrieved from the database";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            if (robot.CurrentArea is null)
            {
                string errorMessage = $"Unable to schedule a return to home mission as the robot {robot.Id} is not localized.";
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
                MissionRunType = MissionRunType.ReturnHome,
                Area = robot.CurrentArea,
                Status = MissionStatus.Pending,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = new List<MissionTask>
                {
                    new(new Pose(robot.CurrentArea.Deck.DefaultLocalizationPose.Pose), MissionTaskType.ReturnHome)
                },
                Map = new MapMetadata()
            };
            await mapService.AssignMapToMission(returnToHomeMissionRun);

            var missionRun = await missionRunService.Create(returnToHomeMissionRun, false);
            logger.LogInformation(
                "Scheduled a mission for the robot {RobotName} to return to home location on deck {DeckName}",
                robot.Name, robot.CurrentArea.Deck.Name);
            return missionRun;
        }

        public async Task<MissionRun?> GetActiveReturnToHomeMissionRun(string robotId, bool readOnly = true)
        {
            IList<MissionStatus> missionStatuses = [MissionStatus.Ongoing, MissionStatus.Pending, MissionStatus.Paused];
            var activeReturnToHomeMissions = await missionRunService.ReadMissionRuns(robotId, MissionRunType.ReturnHome, missionStatuses, readOnly: readOnly);

            if (activeReturnToHomeMissions.Count == 0) { return null; }

            if (activeReturnToHomeMissions.Count > 1) { logger.LogError($"Two Return to Home missions should not be queued or ongoing simoultaneously for robot with Id {robotId}."); }

            return activeReturnToHomeMissions.FirstOrDefault();
        }
    }
}
