using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services.Events;
using Api.Services.Models;
using Api.Utilities;
namespace Api.Services
{
    public interface IMissionSchedulingService
    {
        public Task StartNextMissionRunIfSystemIsAvailable(Robot robot);

        public Task<bool> OngoingMission(string robotId);

        public Task FreezeMissionRunQueueForRobot(string robotId);

        public Task StopCurrentMissionRun(string robotId);

        public Task AbortAllScheduledMissions(string robotId, string? abortReason = null);

        public Task ScheduleMissionToDriveToDockPosition(string robotId);

        public Task UnfreezeMissionRunQueueForRobot(string robotId);

        public bool MissionRunQueueIsEmpty(IList<MissionRun> missionRunQueue);

        public void TriggerRobotAvailable(RobotAvailableEventArgs e);

        public Task AbortActiveReturnToHomeMission(string robotId);

    }

    public class MissionSchedulingService(ILogger<MissionSchedulingService> logger, IMissionRunService missionRunService, IRobotService robotService,
            IIsarService isarService, ILocalizationService localizationService, IReturnToHomeService returnToHomeService, ISignalRService signalRService, IErrorHandlingService errorHandlingService) : IMissionSchedulingService
    {
        public async Task StartNextMissionRunIfSystemIsAvailable(Robot robot)
        {
            logger.LogInformation("Robot {robotName} has status {robotStatus} and current area {areaName}", robot.Name, robot.Status, robot.CurrentInspectionArea?.Name);

            MissionRun? missionRun;
            try { missionRun = await SelectNextMissionRun(robot); }
            catch (RobotNotFoundException)
            {
                logger.LogError("Robot with ID: {RobotId} was not found in the database", robot.Id);
                return;
            }

            if (robot.MissionQueueFrozen && missionRun != null && !(missionRun.IsEmergencyMission() || missionRun.IsReturnHomeMission()))
            {
                logger.LogInformation("Robot {robotName} was ready to start a mission but its mission queue was frozen", robot.Name);
                return;
            }

            if (missionRun == null)
            {
                logger.LogInformation("The robot was ready to start mission, but no mission is scheduled");

                if (robot.RobotCapabilities != null && robot.RobotCapabilities.Contains(RobotCapabilitiesEnum.return_to_home))
                {
                    try { missionRun = await returnToHomeService.ScheduleReturnToHomeMissionRunIfNotAlreadyScheduledOrRobotIsHome(robot.Id); }
                    catch (ReturnToHomeMissionFailedToScheduleException)
                    {
                        signalRService.ReportGeneralFailToSignalR(robot, $"Failed to schedule return to home for robot {robot.Name}", "");
                        logger.LogError("Failed to schedule a return to home mission for robot {RobotId}", robot.Id);
                    }
                }
            }

            if (missionRun == null) { return; }

            if (!TheSystemIsAvailableToRunAMission(robot, missionRun))
            {
                logger.LogInformation("Mission {MissionRunId} was put on the queue as the system may not start a mission now", missionRun.Id);
                return;
            }

            if (missionRun.InspectionArea == null)
            {
                logger.LogWarning("Mission {MissionRunId} does not have an inspection area and was therefore not started", missionRun.Id);
                return;
            }

            if (robot.CurrentInspectionArea == null && missionRun.InspectionArea != null)
            {
                await robotService.UpdateCurrentInspectionArea(robot.Id, missionRun.InspectionArea.Id);
            }
            else if (!await localizationService.RobotIsOnSameInspectionAreaAsMission(robot.Id, missionRun.InspectionArea!.Id))
            {
                logger.LogError("Robot {RobotId} is not on the same inspection area as the mission run {MissionRunId}. Aborting all mission runs", robot.Id, missionRun.Id);
                try { await AbortAllScheduledMissions(robot.Id, "Aborted: Robot was at different inspection area"); }
                catch (RobotNotFoundException) { logger.LogError("Failed to abort scheduled missions for robot {RobotId}", robot.Id); }

                try { await returnToHomeService.ScheduleReturnToHomeMissionRunIfNotAlreadyScheduledOrRobotIsHome(robot.Id); }
                catch (ReturnToHomeMissionFailedToScheduleException)
                {
                    logger.LogError("Failed to schedule a return to home mission for robot {RobotId}", robot.Id);
                }
                return;
            }

            if ((robot.IsRobotPressureTooLow() || robot.IsRobotBatteryTooLow()) && !(missionRun.IsReturnHomeMission() || missionRun.IsEmergencyMission()))
            {
                missionRun = await HandleBatteryAndPressureLevel(robot);
                if (missionRun == null) { return; }
            }

            try { await StartMissionRun(missionRun, robot); }
            catch (Exception ex) when (
                ex is MissionException
                    or RobotNotFoundException
                    or RobotNotAvailableException
                    or MissionRunNotFoundException
                    or IsarCommunicationException)
            {
                logger.LogError(
                    ex,
                    "Mission run {MissionRunId} was not started successfully. {ErrorMessage}",
                    missionRun.Id,
                    ex.Message
                );
                await missionRunService.SetMissionRunToFailed(missionRun.Id, $"Mission run '{missionRun.Id}' was not started successfully. '{ex.Message}'");
            }
            catch (RobotBusyException)
            {
            }
        }

        public async Task<MissionRun?> HandleBatteryAndPressureLevel(Robot robot)
        {
            if (robot.IsRobotPressureTooLow())
            {
                logger.LogError("Robot with ID: {RobotId} cannot start missions because pressure value is too low.", robot.Id);
                signalRService.ReportGeneralFailToSignalR(robot, $"Low pressure value for robot {robot.Name}", "Pressure value is too low to start a mission.");
            }
            if (robot.IsRobotBatteryTooLow())
            {
                logger.LogError("Robot with ID: {RobotId} cannot start missions because battery value is too low.", robot.Id);
                signalRService.ReportGeneralFailToSignalR(robot, $"Low battery value for robot {robot.Name}", "Battery value is too low to start a mission.");
            }

            try { await AbortAllScheduledMissions(robot.Id, "Aborted: Robot pressure or battery values are too low."); }
            catch (RobotNotFoundException) { logger.LogError("Failed to abort scheduled missions for robot {RobotId}", robot.Id); }

            MissionRun? missionRun;
            try { missionRun = await returnToHomeService.ScheduleReturnToHomeMissionRunIfNotAlreadyScheduledOrRobotIsHome(robot.Id); }
            catch (ReturnToHomeMissionFailedToScheduleException)
            {
                logger.LogError("Failed to schedule a return to home mission for robot {RobotId}", robot.Id);
                return null;
            }
            return missionRun;
        }

        public async Task<bool> OngoingMission(string robotId)
        {
            var ongoingMissions = await GetOngoingMissions(robotId, readOnly: true);
            return ongoingMissions is not null && ongoingMissions.Any();
        }


        public async Task FreezeMissionRunQueueForRobot(string robotId)
        {
            await robotService.UpdateMissionQueueFrozen(robotId, true);
            logger.LogInformation("Mission queue was frozen for robot with Id {RobotId}", robotId);
        }

        public async Task UnfreezeMissionRunQueueForRobot(string robotId)
        {
            await robotService.UpdateMissionQueueFrozen(robotId, false);
            logger.LogInformation("Mission queue for robot with ID {RobotId} was unfrozen", robotId);
        }

        public async Task StopCurrentMissionRun(string robotId)
        {
            var robot = await robotService.ReadById(robotId, readOnly: true);
            if (robot == null)
            {
                string errorMessage = $"Robot with ID: {robotId} was not found in the database";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            var ongoingMissionRuns = await GetOngoingMissions(robotId, readOnly: true);
            if (ongoingMissionRuns is null)
            {
                string errorMessage = $"There were no ongoing mission runs to stop for robot {robotId}";
                logger.LogWarning("{Message}", errorMessage);
                throw new MissionRunNotFoundException(errorMessage);
            }

            IList<string> ongoingMissionRunIds = ongoingMissionRuns.Select(missionRun => missionRun.Id).ToList();

            try { await isarService.StopMission(robot); }
            catch (HttpRequestException e)
            {
                const string Message = "Error connecting to ISAR while stopping mission";
                logger.LogError(e, "{Message}", Message);
                await errorHandlingService.HandleLosingConnectionToIsar(robot.Id);
                throw new MissionException(Message, (int)e.StatusCode!);
            }
            catch (MissionException e)
            {
                const string Message = "Error while stopping ISAR mission";
                logger.LogError(e, "{Message}", Message);
                throw;
            }
            catch (JsonException e)
            {
                const string Message = "Error while processing the response from ISAR";
                logger.LogError(e, "{Message}", Message);
                throw new MissionException(Message, 0);
            }
            catch (MissionNotFoundException) { logger.LogWarning("{Message}", $"No mission was running for robot {robot.Id}"); }

            await MoveInterruptedMissionsToQueue(ongoingMissionRunIds);

            try { await robotService.UpdateCurrentMissionId(robotId, null); }
            catch (RobotNotFoundException) { }
        }

        public async Task AbortAllScheduledMissions(string robotId, string? abortReason)
        {
            var robot = await robotService.ReadById(robotId, readOnly: true);
            if (robot == null)
            {
                string errorMessage = $"Robot with ID: {robotId} was not found in the database";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            var pendingMissionRuns = await missionRunService.ReadMissionRunQueue(robotId, readOnly: true);
            if (pendingMissionRuns is null)
            {
                string infoMessage = $"There were no mission runs in the queue to abort for robot {robotId}";
                logger.LogWarning("{Message}", infoMessage);
                return;
            }

            IList<string> pendingMissionRunIds = pendingMissionRuns.Select(missionRun => missionRun.Id).ToList();

            foreach (var pendingMissionRun in pendingMissionRuns)
            {
                await missionRunService.UpdateMissionRunProperty(pendingMissionRun.Id, "Status", MissionStatus.Aborted);
                await missionRunService.UpdateMissionRunProperty(pendingMissionRun.Id, "StatusReason", abortReason);
            }
        }

        public async Task ScheduleMissionToDriveToDockPosition(string robotId)
        {
            var robot = await robotService.ReadById(robotId, readOnly: true);
            if (robot == null)
            {
                logger.LogError("Robot with ID: {RobotId} was not found in the database", robotId);
                return;
            }

            Pose robotPose;

            if (robot.CurrentInspectionArea != null)
            {
                if (robot.CurrentInspectionArea?.DefaultLocalizationPose == null)
                {
                    robotPose = new Pose();
                }
                else
                {
                    robotPose = robot.CurrentInspectionArea.DefaultLocalizationPose.Pose;
                }
            }
            else
            {
                string errorMessage = $"Robot with ID {robotId} could return home as it did not have an inspection area";
                logger.LogError("{Message}", errorMessage);
                throw new InspectionAreaNotFoundException(errorMessage);
            }

            // Cloning to avoid tracking same object
            var clonedPose = ObjectCopier.Clone(robotPose);
            var customTaskQuery = new CustomTaskQuery
            {
                RobotPose = clonedPose,
                TaskOrder = 0
            };

            var missionRun = new MissionRun
            {
                Name = "Drive to Docking Station",
                Robot = robot,
                MissionRunType = MissionRunType.Emergency,
                InstallationCode = robot.CurrentInstallation.InstallationCode,
                InspectionArea = robot.CurrentInspectionArea!,
                Status = MissionStatus.Pending,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = new List<MissionTask>(
                [
                    new MissionTask(customTaskQuery)
                ])
            };

            try
            {
                await missionRunService.Create(missionRun);
            }
            catch (UnsupportedRobotCapabilityException)
            {
                logger.LogError($"Unsupported robot capability detected when driving to dock for robot {missionRun.Robot.Name}. This should not happen.");
            }
        }

        public bool MissionRunQueueIsEmpty(IList<MissionRun> missionRunQueue)
        {
            return !missionRunQueue.Any();
        }

        public void TriggerRobotAvailable(RobotAvailableEventArgs e)
        {
            OnRobotAvailable(e);
        }

        private async Task<MissionRun?> SelectNextMissionRun(Robot robot)
        {
            var missionRun = await missionRunService.ReadNextScheduledEmergencyMissionRun(robot.Id, readOnly: true);
            if (robot.MissionQueueFrozen == false && missionRun == null) { missionRun = await missionRunService.ReadNextScheduledMissionRun(robot.Id, readOnly: true); }
            return missionRun;
        }

        private async Task MoveInterruptedMissionsToQueue(IEnumerable<string> interruptedMissionRunIds)
        {
            foreach (string missionRunId in interruptedMissionRunIds)
            {
                var missionRun = await missionRunService.ReadById(missionRunId, readOnly: true);
                if (missionRun is null)
                {
                    logger.LogWarning("{Message}", $"Interrupted mission run with Id {missionRunId} could not be found");
                    continue;
                }

                if (missionRun.IsReturnHomeMission())
                {
                    logger.LogWarning("Return to home mission will not be added back to the queue.");
                    return;
                }

                var unfinishedTasks = missionRun.Tasks
                    .Where(t => !new List<Database.Models.TaskStatus>
                        {Database.Models.TaskStatus.Successful, Database.Models.TaskStatus.Failed}
                        .Contains(t.Status))
                    .Select(t => new MissionTask(t)).ToList();

                if (unfinishedTasks.Count == 0) continue;

                var newMissionRun = new MissionRun
                {
                    Name = missionRun.Name,
                    Robot = missionRun.Robot,
                    MissionRunType = missionRun.MissionRunType,
                    InstallationCode = missionRun.InspectionArea!.Installation.InstallationCode,
                    InspectionArea = missionRun.InspectionArea,
                    Status = MissionStatus.Pending,
                    DesiredStartTime = DateTime.UtcNow,
                    Tasks = unfinishedTasks
                };

                try
                {
                    await missionRunService.Create(newMissionRun, triggerCreatedMissionRunEvent: false);
                }
                catch (UnsupportedRobotCapabilityException)
                {
                    logger.LogError($"Unsupported robot capability detected when restarting interrupted missions for robot {missionRun.Robot.Name}. This should not happen.");
                }
            }
        }

        private async Task StartMissionRun(MissionRun queuedMissionRun, Robot robot)
        {
            string missionRunId = queuedMissionRun.Id;

            var missionRun = await missionRunService.ReadById(missionRunId, readOnly: true);
            if (missionRun == null)
            {
                string errorMessage = $"Could not find mission run with id {missionRunId}";
                logger.LogError("{Message}", errorMessage);
                throw new MissionRunNotFoundException(errorMessage);
            }

            IsarMission isarMission;
            try { isarMission = await isarService.StartMission(robot, missionRun); }
            catch (HttpRequestException e)
            {
                string errorMessage = $"Could not reach ISAR at {robot.IsarUri}";
                logger.LogError(e, "{Message}", errorMessage);
                await errorHandlingService.HandleLosingConnectionToIsar(robot.Id);
                throw new IsarCommunicationException(errorMessage);
            }
            catch (MissionException e)
            {
                const string ErrorMessage = "Error while starting ISAR mission";
                logger.LogError(e, "{Message}", ErrorMessage);
                throw new IsarCommunicationException(ErrorMessage);
            }
            catch (JsonException e)
            {
                const string ErrorMessage = "Error while processing of the response from ISAR";
                logger.LogError(e, "{Message}", ErrorMessage);
                throw new IsarCommunicationException(ErrorMessage);
            }

            await missionRunService.UpdateWithIsarInfo(missionRun.Id, isarMission);
            await missionRunService.UpdateMissionRunProperty(missionRun.Id, "Status", MissionStatus.Ongoing);

            robot.Status = RobotStatus.Busy;
            await robotService.UpdateRobotStatus(robot.Id, RobotStatus.Busy);
            await robotService.UpdateCurrentMissionId(robot.Id, missionRun.Id);

            logger.LogInformation("Started mission run '{Id}'", queuedMissionRun.Id);
        }

        private async Task<PagedList<MissionRun>?> GetOngoingMissions(string robotId, bool readOnly = true)
        {
            var ongoingMissions = await missionRunService.ReadAll(
                new MissionRunQueryStringParameters
                {
                    Statuses = [MissionStatus.Ongoing],
                    RobotId = robotId,
                    OrderBy = "DesiredStartTime",
                    PageSize = 100
                }, readOnly: readOnly);

            return ongoingMissions;
        }

        private bool TheSystemIsAvailableToRunAMission(Robot robot, MissionRun missionRun)
        {
            if (robot.MissionQueueFrozen && !(missionRun.IsEmergencyMission() || missionRun.IsReturnHomeMission()))
            {
                logger.LogInformation("Mission run {MissionRunId} was not started as the mission run queue for robot {RobotName} is frozen", missionRun.Id, robot.Name);
                return false;
            }

            if (robot.Status is not RobotStatus.Available)
            {
                logger.LogInformation("Mission run {MissionRunId} was not started as the robot is not available", missionRun.Id);
                return false;
            }
            if (!robot.IsarConnected)
            {
                logger.LogWarning("Mission run {MissionRunId} was not started as the robots {RobotId} isar instance is disconnected", missionRun.Id, robot.Id);
                return false;
            }
            if (robot.Deprecated)
            {
                logger.LogWarning("Mission run {MissionRunId} was not started as the robot {RobotId} is deprecated", missionRun.Id, robot.Id);
                return false;
            }
            return true;
        }

        public async Task AbortActiveReturnToHomeMission(string robotId)
        {
            var activeReturnToHomeMission = await returnToHomeService.GetActiveReturnToHomeMissionRun(robotId, readOnly: true);

            if (activeReturnToHomeMission == null)
            {
                logger.LogWarning("Attempted to abort active Return to Home mission for robot with Id {RobotId} but none was found", robotId);
                return;
            }

            try { await missionRunService.UpdateMissionRunProperty(activeReturnToHomeMission.Id, "Status", MissionStatus.Aborted); }
            catch (MissionRunNotFoundException) { return; }

            if (activeReturnToHomeMission.Status == MissionStatus.Pending) { return; }

            try { await StopCurrentMissionRun(activeReturnToHomeMission.Robot.Id); }
            catch (RobotNotFoundException) { return; }
            catch (MissionRunNotFoundException) { return; }
        }

        protected virtual void OnRobotAvailable(RobotAvailableEventArgs e) { RobotAvailable?.Invoke(this, e); }
        public static event EventHandler<RobotAvailableEventArgs>? RobotAvailable;
    }
}
