using Api.Database.Models;
using Api.Utilities;

namespace Api.Services
{
    public interface IReturnToHomeService
    {
        public Task<MissionRun?> ScheduleReturnToHomeMissionRunIfNotAlreadyScheduled(
            Robot robot,
            bool shouldTriggerMissionCreatedEvent = false
        );
        public Task<MissionRun?> GetActiveReturnToHomeMissionRun(
            string robotId,
            bool readOnly = true
        );
    }

    public class ReturnToHomeService(
        ILogger<ReturnToHomeService> logger,
        IMissionRunService missionRunService
    ) : IReturnToHomeService
    {
        public async Task<MissionRun?> ScheduleReturnToHomeMissionRunIfNotAlreadyScheduled(
            Robot robot,
            bool shouldTriggerMissionCreatedEvent = false
        )
        {
            logger.LogInformation(
                "Scheduling return home mission if not already scheduled and the robot is not home for Robot {RobotName} with Id {RobotId}",
                robot.Name,
                robot.Id
            );

            if (await IsReturnToHomeMissionAlreadyScheduled(robot.Id))
            {
                logger.LogInformation(
                    "Return Home Mission already scheduled for Robot {RobotName} with Id {RobotId}",
                    robot.Name,
                    robot.Id
                );
                return null;
            }

            MissionRun missionRun;
            try
            {
                missionRun = await ScheduleReturnToHomeMissionRun(
                    robot,
                    shouldTriggerMissionCreatedEvent
                );
            }
            catch (Exception ex)
                when (ex
                        is RobotNotFoundException
                            or AreaNotFoundException
                            or InspectionAreaNotFoundException
                            or PoseNotFoundException
                            or UnsupportedRobotCapabilityException
                            or MissionRunNotFoundException
                )
            {
                throw new ReturnToHomeMissionFailedToScheduleException(ex.Message);
            }

            return missionRun;
        }

        private async Task<bool> IsReturnToHomeMissionAlreadyScheduled(string robotId)
        {
            return await missionRunService.PendingOrOngoingReturnToHomeMissionRunExists(robotId);
        }

        private async Task<MissionRun> ScheduleReturnToHomeMissionRun(
            Robot robot,
            bool shouldTriggerMissionCreatedEvent = false
        )
        {
            Pose? returnToHomePose;
            InspectionArea? currentInspectionArea;
            if (
                robot.RobotCapabilities is not null
                && robot.RobotCapabilities.Contains(RobotCapabilitiesEnum.auto_return_to_home)
            )
            {
                var previousMissionRun = await missionRunService.ReadLastExecutedMissionRunByRobot(
                    robot.Id,
                    readOnly: true
                );
                currentInspectionArea = previousMissionRun?.InspectionArea;
                returnToHomePose = new Pose();
            }
            else
            {
                currentInspectionArea = robot.CurrentInspectionArea;
                returnToHomePose = new Pose();
            }

            if (currentInspectionArea == null)
            {
                string errorMessage =
                    $"Robot with ID {robot.Id} could not return to home because it does not have an inspection area";
                logger.LogError("Message: {Message}", errorMessage);
                throw new InspectionAreaNotFoundException(errorMessage);
            }

            var returnToHomeMissionRun = new MissionRun
            {
                Name = "Return Home",
                Robot = robot,
                InstallationCode = robot.CurrentInstallation.InstallationCode,
                MissionRunType = MissionRunType.ReturnHome,
                InspectionArea = currentInspectionArea!,
                Status = MissionStatus.Pending,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = [new(returnToHomePose, MissionTaskType.ReturnHome)],
            };

            var missionRun = await missionRunService.Create(
                returnToHomeMissionRun,
                shouldTriggerMissionCreatedEvent
            );
            logger.LogInformation(
                "Scheduled Return to Home mission for robot {RobotName} on Inspection Area {InspectionAreaName}",
                robot.Name,
                currentInspectionArea?.Name
            );
            return missionRun;
        }

        public async Task<MissionRun?> GetActiveReturnToHomeMissionRun(
            string robotId,
            bool readOnly = true
        )
        {
            IList<MissionStatus> activeMissionStatuses =
            [
                MissionStatus.Ongoing,
                MissionStatus.Paused,
            ];
            var activeReturnToHomeMissions = await missionRunService.ReadMissionRuns(
                robotId,
                MissionRunType.ReturnHome,
                activeMissionStatuses,
                readOnly: readOnly
            );

            if (activeReturnToHomeMissions.Count == 0)
            {
                return null;
            }

            if (activeReturnToHomeMissions.Count > 1)
            {
                logger.LogError(
                    "Two Return home missions should not be queued or ongoing simoultaneously for robot with Id {robotId}.",
                    robotId
                );
            }

            return activeReturnToHomeMissions.FirstOrDefault();
        }
    }
}
