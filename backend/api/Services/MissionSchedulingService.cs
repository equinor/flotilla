﻿using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services.Events;
using Api.Services.Helpers;
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

        public Task AbortAllScheduledNormalMissions(string robotId, string? abortReason = null);

        public Task ScheduleMissionToDriveToDockPosition(string robotId);

        public Task UnfreezeMissionRunQueueForRobot(string robotId);

        public bool MissionRunQueueIsEmpty(IList<MissionRun> missionRunQueue);

        public void TriggerRobotAvailable(RobotAvailableEventArgs e);

        public Task AbortActiveReturnToHomeMission(string robotId);

        public Task ScheduleMissionRunFromMissionDefinitionLastSuccessfullRun(
            string missionDefinitionId,
            string robotId
        );
    }

    public class MissionSchedulingService(
        ILogger<MissionSchedulingService> logger,
        IMissionRunService missionRunService,
        IMissionDefinitionService missionDefinitionService,
        IMapService mapService,
        IRobotService robotService,
        IIsarService isarService,
        IReturnToHomeService returnToHomeService,
        ISignalRService signalRService,
        IErrorHandlingService errorHandlingService,
        IInspectionAreaService inspectionAreaService,
        IInstallationService installationService
    ) : IMissionSchedulingService
    {
        public async Task StartNextMissionRunIfSystemIsAvailable(Robot robot)
        {
            logger.LogInformation(
                "Robot {robotName} has status {robotStatus} and current area {areaName}",
                robot.Name,
                robot.Status,
                robot.CurrentInspectionArea?.Name
            );

            MissionRun? missionRun;
            try
            {
                missionRun = await SelectNextMissionRun(robot);
            }
            catch (RobotNotFoundException)
            {
                logger.LogError("Robot with ID: {RobotId} was not found in the database", robot.Id);
                return;
            }

            if (missionRun == null)
                return;

            if (robot.MissionQueueFrozen && !missionRun.IsReturnHomeOrEmergencyMission())
            {
                logger.LogInformation(
                    "Robot {robotName} is ready to start a mission but its mission queue is frozen",
                    robot.Name
                );
                return;
            }

            if (
                !MissionSchedulingHelpers.TheSystemIsAvailableToRunAMission(
                    robot,
                    missionRun,
                    logger
                )
            )
            {
                logger.LogInformation(
                    "Mission {MissionRunId} was put on the queue as the system may not start a mission now",
                    missionRun.Id
                );
                return;
            }

            if (missionRun.InspectionArea == null)
            {
                logger.LogWarning(
                    "Mission {MissionRunId} cannot be started as it does not have an inspection area",
                    missionRun.Id
                );
                return;
            }

            if (robot.CurrentInspectionArea == null)
            {
                await robotService.UpdateCurrentInspectionArea(
                    robot.Id,
                    missionRun.InspectionArea!.Id
                );
                robot.CurrentInspectionArea = missionRun.InspectionArea!;
            }
            else if (
                !missionRun.IsReturnHomeOrEmergencyMission()
                && !inspectionAreaService.MissionTasksAreInsideInspectionAreaPolygon(
                    (List<MissionTask>)missionRun.Tasks,
                    missionRun.InspectionArea
                )
            )
            {
                logger.LogError(
                    "Robot {RobotNAme} with Id {RobotId} is not on the same inspection area as the mission run with Id {MissionRunId}. Aborting all mission runs",
                    robot.Name,
                    robot.Id,
                    missionRun.Id
                );
                try
                {
                    await AbortAllScheduledNormalMissions(
                        robot.Id,
                        $"All missions aborted: Robot {robot.Name} is on inspection area {robot.CurrentInspectionArea?.Name} "
                            + $"and mission run was on inspection area {missionRun.InspectionArea?.Name}"
                    );
                }
                catch (RobotNotFoundException)
                {
                    logger.LogError(
                        "Failed to abort all scheduled missions for robot {RobotName} with Id {RobotId}",
                        robot.Name,
                        robot.Id
                    );
                }

                return;
            }

            if (
                (robot.IsRobotPressureTooLow() || robot.IsRobotBatteryTooLow())
                && !missionRun.IsReturnHomeOrEmergencyMission()
            )
            {
                await HandleBatteryAndPressureLevel(robot);
                return;
            }

            try
            {
                await StartMissionRun(missionRun, robot);
            }
            catch (Exception ex)
                when (ex
                        is MissionException
                            or RobotNotFoundException
                            or RobotNotAvailableException
                            or MissionRunNotFoundException
                            or IsarCommunicationException
                )
            {
                logger.LogError(
                    ex,
                    "Mission run {MissionRunId} was not started successfully. {ErrorMessage}",
                    missionRun.Id,
                    ex.Message
                );
                try
                {
                    await missionRunService.SetMissionRunToFailed(
                        missionRun.Id,
                        $"Mission run '{missionRun.Id}' was not started successfully. '{ex.Message}'"
                    );
                }
                catch (MissionRunNotFoundException)
                {
                    logger.LogError(
                        "Mission '{MissionId}' could not be set to failed as it no longer exists",
                        robot.CurrentMissionId
                    );
                }
            }
            catch (RobotBusyException)
            {
                return;
            }

            try
            {
                robot.CurrentInspectionArea ??= missionRun.InspectionArea;
                await returnToHomeService.ScheduleReturnToHomeMissionRunIfNotAlreadyScheduled(
                    robot
                );
            }
            catch (ReturnToHomeMissionFailedToScheduleException)
            {
                signalRService.ReportGeneralFailToSignalR(
                    robot,
                    $"Failed to schedule return home for robot {robot.Name}",
                    ""
                );
                logger.LogError(
                    "Failed to schedule a return home mission for robot {RobotId}",
                    robot.Id
                );
            }
        }

        public async Task HandleBatteryAndPressureLevel(Robot robot)
        {
            if (robot.IsRobotPressureTooLow())
            {
                logger.LogError(
                    "Robot with ID: {RobotId} cannot start missions because pressure value is too low.",
                    robot.Id
                );
                signalRService.ReportGeneralFailToSignalR(
                    robot,
                    $"Low pressure value for robot {robot.Name}",
                    "Pressure value is too low to start a mission."
                );
            }
            if (robot.IsRobotBatteryTooLow())
            {
                logger.LogError(
                    "Robot with ID: {RobotId} cannot start missions because battery value is too low.",
                    robot.Id
                );
                signalRService.ReportGeneralFailToSignalR(
                    robot,
                    $"Low battery value for robot {robot.Name}",
                    "Battery value is too low to start a mission."
                );
            }

            try
            {
                await AbortAllScheduledNormalMissions(
                    robot.Id,
                    $"Mission aborted for robot {robot.Name}: pressure or battery values too low."
                );
            }
            catch (RobotNotFoundException)
            {
                logger.LogError(
                    "Failed to abort scheduled missions for robot {RobotName} with Id {RobotId} since the robot was not found",
                    robot.Name,
                    robot.Id
                );
            }
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
            logger.LogInformation(
                "Mission queue for robot with ID {RobotId} was unfrozen",
                robotId
            );
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
                string errorMessage =
                    $"There were no ongoing mission runs to stop for robot {robotId}";
                logger.LogWarning("{Message}", errorMessage);
                throw new MissionRunNotFoundException(errorMessage);
            }

            IList<string> ongoingMissionRunIds = ongoingMissionRuns
                .Select(missionRun => missionRun.Id)
                .ToList();

            try
            {
                await isarService.StopMission(robot);
            }
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
            catch (MissionNotFoundException)
            {
                logger.LogWarning("{Message}", $"No mission was running for robot {robot.Id}");
            }

            await MoveInterruptedMissionsToQueue(ongoingMissionRunIds);

            try
            {
                await robotService.UpdateCurrentMissionId(robotId, null);
            }
            catch (RobotNotFoundException) { }
        }

        public async Task AbortAllScheduledNormalMissions(string robotId, string? abortReason)
        {
            var robot = await robotService.ReadById(robotId, readOnly: true);
            if (robot == null)
            {
                string errorMessage = $"Robot with ID: {robotId} was not found in the database";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            var pendingMissionRuns = await missionRunService.ReadMissionRunQueue(
                robotId,
                type: MissionRunType.Normal,
                readOnly: true
            );
            if (pendingMissionRuns is null)
            {
                string infoMessage =
                    $"There were no mission runs in the queue to abort for robot {robotId}";
                logger.LogWarning("{Message}", infoMessage);
                return;
            }

            IList<string> pendingMissionRunIds = pendingMissionRuns
                .Select(missionRun => missionRun.Id)
                .ToList();

            foreach (var pendingMissionRun in pendingMissionRuns)
            {
                await missionRunService.UpdateMissionRunProperty(
                    pendingMissionRun.Id,
                    "Status",
                    MissionStatus.Aborted
                );
                await missionRunService.UpdateMissionRunProperty(
                    pendingMissionRun.Id,
                    "StatusReason",
                    abortReason
                );
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
                robotPose = new Pose();
            }
            else
            {
                string errorMessage =
                    $"Robot with ID {robotId} could not return home as it did not have an inspection area";
                logger.LogError("{Message}", errorMessage);
                throw new InspectionAreaNotFoundException(errorMessage);
            }

            // Cloning to avoid tracking same object
            var clonedPose = ObjectCopier.Clone(robotPose);
            var customTaskQuery = new CustomTaskQuery { RobotPose = clonedPose, TaskOrder = 0 };

            var missionRun = new MissionRun
            {
                Name = "Return home",
                Robot = robot,
                MissionRunType = MissionRunType.Emergency,
                InstallationCode = robot.CurrentInstallation.InstallationCode,
                InspectionArea = robot.CurrentInspectionArea!,
                Status = MissionStatus.Pending,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = new List<MissionTask>([new MissionTask(customTaskQuery)]),
            };

            try
            {
                await missionRunService.Create(
                    missionRun: missionRun,
                    triggerCreatedMissionRunEvent: false
                );
            }
            catch (UnsupportedRobotCapabilityException)
            {
                logger.LogError(
                    $"Unsupported robot capability detected when driving to dock for robot {missionRun.Robot.Name}. This should not happen."
                );
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
            var missionRun = await missionRunService.ReadNextScheduledMissionRun(
                robot.Id,
                type: MissionRunType.Emergency,
                readOnly: true
            );
            if (robot.MissionQueueFrozen == false && missionRun == null)
            {
                missionRun = await missionRunService.ReadNextScheduledMissionRun(
                    robot.Id,
                    type: MissionRunType.Normal,
                    readOnly: true
                );
            }
            missionRun ??= await missionRunService.ReadNextScheduledMissionRun(
                robot.Id,
                type: MissionRunType.ReturnHome,
                readOnly: true
            );
            return missionRun;
        }

        private async Task MoveInterruptedMissionsToQueue(
            IEnumerable<string> interruptedMissionRunIds
        )
        {
            foreach (string missionRunId in interruptedMissionRunIds)
            {
                var missionRun = await missionRunService.ReadById(missionRunId, readOnly: true);
                if (missionRun is null)
                {
                    logger.LogWarning(
                        "{Message}",
                        $"Interrupted mission run with Id {missionRunId} could not be found"
                    );
                    continue;
                }

                if (missionRun.IsReturnHomeMission())
                {
                    logger.LogWarning("Return home mission will not be added back to the queue.");
                    return;
                }

                var unfinishedTasks = missionRun
                    .Tasks.Where(t =>
                        !new List<Database.Models.TaskStatus>
                        {
                            Database.Models.TaskStatus.Successful,
                            Database.Models.TaskStatus.Failed,
                        }.Contains(t.Status)
                    )
                    .Select(t => new MissionTask(t))
                    .ToList();

                if (unfinishedTasks.Count == 0)
                    continue;

                var newMissionRun = new MissionRun
                {
                    Name = missionRun.Name,
                    Robot = missionRun.Robot,
                    MissionRunType = missionRun.MissionRunType,
                    InstallationCode = missionRun.InspectionArea!.Installation.InstallationCode,
                    InspectionArea = missionRun.InspectionArea,
                    Status = MissionStatus.Pending,
                    DesiredStartTime = DateTime.UtcNow,
                    Tasks = unfinishedTasks,
                };

                try
                {
                    await missionRunService.Create(
                        newMissionRun,
                        triggerCreatedMissionRunEvent: false
                    );
                }
                catch (UnsupportedRobotCapabilityException)
                {
                    logger.LogError(
                        "Unsupported robot capability detected when restarting interrupted missions for robot {robotName}. This should not happen.",
                        missionRun.Robot.Name
                    );
                }
            }
        }

        private async Task StartMissionRun(MissionRun queuedMissionRun, Robot robot)
        {
            IsarMission isarMission;
            try
            {
                isarMission = await isarService.StartMission(robot, queuedMissionRun);
            }
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

            await missionRunService.UpdateWithIsarInfo(queuedMissionRun.Id, isarMission);
            await missionRunService.UpdateMissionRunProperty(
                queuedMissionRun.Id,
                "Status",
                MissionStatus.Ongoing
            );

            robot.Status = RobotStatus.Busy;
            await robotService.UpdateRobotStatus(robot.Id, RobotStatus.Busy);
            await robotService.UpdateCurrentMissionId(robot.Id, queuedMissionRun.Id);

            logger.LogInformation("Started mission run '{Id}'", queuedMissionRun.Id);
        }

        private async Task<PagedList<MissionRun>?> GetOngoingMissions(
            string robotId,
            bool readOnly = true
        )
        {
            var ongoingMissions = await missionRunService.ReadAll(
                new MissionRunQueryStringParameters
                {
                    Statuses = [MissionStatus.Ongoing],
                    RobotId = robotId,
                    OrderBy = "DesiredStartTime",
                    PageSize = 100,
                },
                readOnly: readOnly
            );

            return ongoingMissions;
        }

        public async Task AbortActiveReturnToHomeMission(string robotId)
        {
            var activeReturnToHomeMission =
                await returnToHomeService.GetActiveReturnToHomeMissionRun(robotId, readOnly: true);

            if (activeReturnToHomeMission == null)
            {
                logger.LogWarning(
                    "Attempted to abort active Return home mission for robot with Id {RobotId} but none was found",
                    robotId
                );
                return;
            }

            try
            {
                await missionRunService.UpdateMissionRunProperty(
                    activeReturnToHomeMission.Id,
                    "Status",
                    MissionStatus.Aborted
                );
            }
            catch (MissionRunNotFoundException)
            {
                return;
            }

            try
            {
                await StopCurrentMissionRun(activeReturnToHomeMission.Robot.Id);
            }
            catch (RobotNotFoundException)
            {
                return;
            }
            catch (MissionRunNotFoundException)
            {
                return;
            }
        }

        protected virtual void OnRobotAvailable(RobotAvailableEventArgs e)
        {
            RobotAvailable?.Invoke(this, e);
        }

        public static event EventHandler<RobotAvailableEventArgs>? RobotAvailable;

        public async Task ScheduleMissionRunFromMissionDefinitionLastSuccessfullRun(
            string missionDefinitionId,
            string robotId
        )
        {
            logger.LogInformation(
                "Scheduling mission run for robot with ID {RobotId} from mission definition with ID {MissionDefinitionId}",
                robotId,
                missionDefinitionId
            );

            Robot robot;
            try
            {
                robot = await robotService.GetRobotWithSchedulingPreCheck(robotId);
            }
            catch (Exception e) when (e is RobotNotFoundException)
            {
                logger.LogError(
                    "Robot with ID {RobotId} was not found when scheduling mission run",
                    robotId
                );
                return;
            }
            catch (Exception e) when (e is RobotPreCheckFailedException)
            {
                logger.LogError(
                    "Robot with ID {RobotId} failed pre-check when scheduling mission run",
                    robotId
                );
                return;
            }

            var missionDefinition = await missionDefinitionService.ReadById(
                missionDefinitionId,
                readOnly: true
            );
            if (missionDefinition == null)
            {
                logger.LogWarning(
                    "Mission definition with ID {MissionDefinitionId} was not found",
                    missionDefinitionId
                );
                return;
            }
            else if (missionDefinition.InspectionArea == null)
            {
                logger.LogWarning(
                    "Mission definition with ID {id} does not have an inspection area when scheduling",
                    missionDefinition.Id
                );
                return;
            }
            else if (missionDefinition.LastSuccessfulRun == null)
            {
                logger.LogWarning(
                    "Mission definition with ID {id} does not have a last successful run when scheduling",
                    missionDefinition.Id
                );
                return;
            }

            try
            {
                await installationService.AssertRobotIsOnSameInstallationAsMission(
                    robot,
                    missionDefinition
                );
            }
            catch (InstallationNotFoundException)
            {
                logger.LogError(
                    "Installation for mission definition with ID {MissionDefinitionId} was not found",
                    missionDefinitionId
                );
                return;
            }
            catch (RobotNotInSameInstallationAsMissionException)
            {
                logger.LogError(
                    "Robot with ID {RobotId} is not in the same installation as the mission definition with ID {MissionDefinitionId}",
                    robotId,
                    missionDefinitionId
                );
                return;
            }

            var missionTasks = new List<MissionTask>();

            foreach (var task in missionDefinition.LastSuccessfulRun.Tasks)
            {
                missionTasks.Add(new MissionTask(task));
            }

            if (missionTasks.Count == 0)
            {
                logger.LogWarning(
                    "Mission definition with ID {id} does not have tasks when scheduling",
                    missionDefinition.Id
                );
                return;
            }

            var missionRun = new MissionRun
            {
                Name = missionDefinition.Name,
                Robot = robot,
                MissionId = missionDefinition.Id,
                Status = MissionStatus.Pending,
                MissionRunType = MissionRunType.Normal,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = missionTasks,
                InstallationCode = missionDefinition.InstallationCode,
                InspectionArea = missionDefinition.InspectionArea,
            };

            if (missionDefinition.Map == null)
            {
                var newMap = await mapService.ChooseMapFromMissionRunTasks(missionRun);
                if (newMap != null)
                {
                    logger.LogInformation(
                        $"Assigned map {newMap.MapName} to mission definition with id {missionDefinition.Id}"
                    );
                    missionDefinition.Map = newMap;
                    await missionDefinitionService.Update(missionDefinition);
                }
            }

            if (missionRun.Tasks.Any())
            {
                missionRun.SetEstimatedTaskDuration();
            }

            MissionRun newMissionRun;
            try
            {
                newMissionRun = await missionRunService.Create(missionRun);
            }
            catch (UnsupportedRobotCapabilityException)
            {
                logger.LogError(
                    "Unsupported robot capability detected when scheduling mission for robot {robotName}.",
                    robot.Name
                );
                return;
            }

            return;
        }
    }
}
