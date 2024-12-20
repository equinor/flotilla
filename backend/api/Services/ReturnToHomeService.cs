using Api.Database.Models;
using Api.Utilities;
namespace Api.Services
{
    public interface IReturnToHomeService
    {
        public Task<MissionRun?> ScheduleReturnToHomeMissionRunIfNotAlreadyScheduledOrRobotIsHome(string robotId);
        public Task<MissionRun?> GetActiveReturnToHomeMissionRun(string robotId, bool readOnly = true);
    }

    public class ReturnToHomeService(ILogger<ReturnToHomeService> logger, IRobotService robotService, IMissionRunService missionRunService) : IReturnToHomeService
    {
        public async Task<MissionRun?> ScheduleReturnToHomeMissionRunIfNotAlreadyScheduledOrRobotIsHome(string robotId)
        {
            logger.LogInformation("Scheduling return to home mission if not already scheduled or the robot is home for robot {RobotId}", robotId);
            var lastMissionRun = await missionRunService.ReadLastExecutedMissionRunByRobot(robotId);

            if (await IsReturnToHomeMissionAlreadyScheduled(robotId) || (lastMissionRun != null && (lastMissionRun.IsReturnHomeMission() || lastMissionRun.IsEmergencyMission())))
            {
                logger.LogInformation("ReturnToHomeMission is already scheduled for Robot {RobotId}", robotId);
                return null;
            }

            MissionRun missionRun;
            try { missionRun = await ScheduleReturnToHomeMissionRun(robotId); }
            catch (Exception ex) when (ex is RobotNotFoundException or AreaNotFoundException or InspectionAreaNotFoundException or PoseNotFoundException or UnsupportedRobotCapabilityException or MissionRunNotFoundException)
            {
                // TODO: if we make ISAR aware of return to home missions, we can avoid scheduling them when the robot does not need them
                throw new ReturnToHomeMissionFailedToScheduleException(ex.Message);
            }

            return missionRun;
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
            Pose? return_to_home_pose;
            InspectionArea? currentInspectionArea;
            if (robot.RobotCapabilities is not null && robot.RobotCapabilities.Contains(RobotCapabilitiesEnum.auto_return_to_home))
            {
                var previousMissionRun = await missionRunService.ReadLastExecutedMissionRunByRobot(robot.Id, readOnly: true);
                currentInspectionArea = previousMissionRun?.InspectionArea;
                return_to_home_pose = previousMissionRun?.InspectionArea?.DefaultLocalizationPose?.Pose == null ? new Pose() : new Pose(previousMissionRun.InspectionArea.DefaultLocalizationPose.Pose);
            }
            else
            {
                currentInspectionArea = robot.CurrentInspectionArea;
                return_to_home_pose = robot.CurrentInspectionArea?.DefaultLocalizationPose?.Pose == null ? new Pose() : new Pose(robot.CurrentInspectionArea.DefaultLocalizationPose.Pose);
            }

            if (currentInspectionArea == null)
            {
                string errorMessage = $"Robot with ID {robotId} could return home as it did not have an inspection area";
                logger.LogError("{Message}", errorMessage);
                throw new InspectionAreaNotFoundException(errorMessage);
            }

            var returnToHomeMissionRun = new MissionRun
            {
                Name = "Return to home mission",
                Robot = robot,
                InstallationCode = robot.CurrentInstallation.InstallationCode,
                MissionRunType = MissionRunType.ReturnHome,
                InspectionArea = currentInspectionArea!,
                Status = MissionStatus.Pending,
                DesiredStartTime = DateTime.UtcNow,
                Tasks =
                [
                    new(return_to_home_pose, MissionTaskType.ReturnHome)
                ]
            };

            var missionRun = await missionRunService.Create(returnToHomeMissionRun, false);
            logger.LogInformation(
                "Scheduled a mission for the robot {RobotName} to return to home location on inspection area {InspectionAreaName}",
                robot.Name, currentInspectionArea?.Name);
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
