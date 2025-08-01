using System.Text.Json;
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

        public Task FreezeMissionRunQueueForRobot(string robotId);

        public Task MoveCurrentMissionRunBackToQueue(string robotId, string? stopReason = null);

        public Task AbortAllScheduledNormalMissions(string robotId, string? abortReason = null);

        public Task UnfreezeMissionRunQueueForRobot(string robotId);

        public bool MissionRunQueueIsEmpty(IList<MissionRun> missionRunQueue);

        public void TriggerRobotReadyForMissions(RobotReadyForMissionsEventArgs e);
    }

    public class MissionSchedulingService(
        ILogger<MissionSchedulingService> logger,
        IMissionRunService missionRunService,
        IRobotService robotService,
        IIsarService isarService,
        ISignalRService signalRService,
        IErrorHandlingService errorHandlingService,
        IInspectionAreaService inspectionAreaService,
        IAreaPolygonService areaPolygonService
    ) : IMissionSchedulingService
    {
        public async Task StartNextMissionRunIfSystemIsAvailable(Robot robot)
        {
            logger.LogInformation(
                "Robot {robotName} has status {robotStatus} and current inspection area id {areaId}",
                robot.Name,
                robot.Status,
                robot.CurrentInspectionAreaId
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

            if (robot.MissionQueueFrozen && !missionRun.IsEmergencyMission())
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

            if (robot.CurrentInspectionAreaId == null)
            {
                logger.LogError(
                    "Robot {RobotName} with Id {RobotId} is not on an inspection area.",
                    robot.Name,
                    robot.Id
                );
                signalRService.ReportGeneralFailToSignalR(
                    robot,
                    $"Robot {robot.Name} is not in an Inspection Area",
                    "Robot is not in an Inspection Area and therefore cannot start a mission. The Mission will be added to the queue."
                );
                return;
            }

            var currentInspectionArea = await inspectionAreaService.ReadById(
                robot.CurrentInspectionAreaId,
                readOnly: true
            );

            if (currentInspectionArea == null)
            {
                logger.LogError(
                    "Robot {RobotName} with Id {RobotId} is not on a valid inspection area.",
                    robot.Name,
                    robot.Id
                );
                return;
            }

            if (
                !missionRun.IsEmergencyMission()
                && !areaPolygonService.MissionTasksAreInsideAreaPolygon(
                    (List<MissionTask>)missionRun.Tasks,
                    currentInspectionArea.AreaPolygon
                )
            )
            {
                logger.LogError(
                    "Robot {RobotNAme} with Id {RobotId} is not on the same inspection area as the mission run with Id {MissionRunId}. Aborting this mission run",
                    robot.Name,
                    robot.Id,
                    missionRun.Id
                );
                try
                {
                    await AbortMissionRun(
                        missionRun,
                        $"Mission run {missionRun.Id} aborted: Robot {robot.Name} is on inspection area {currentInspectionArea.Name} "
                            + $"and mission run is on inspection area {missionRun.InspectionArea.Name}"
                    );
                }
                catch (RobotNotFoundException)
                {
                    logger.LogError(
                        "Failed to abort scheduled mission {missionRun.Id} for robot {RobotName} with Id {RobotId}",
                        missionRun.Id,
                        robot.Name,
                        robot.Id
                    );
                }

                await StartNextMissionRunIfSystemIsAvailable(robot);
                return;
            }

            if (
                (robot.IsRobotPressureTooLow() || robot.IsRobotBatteryTooLow())
                && !missionRun.IsEmergencyMission()
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
            catch (RobotBusyException) { }
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

        public async Task MoveCurrentMissionRunBackToQueue(
            string robotId,
            string? stopReason = null
        )
        {
            var robot = await robotService.ReadById(robotId, readOnly: true);
            if (robot == null)
            {
                string errorMessage = $"Robot with ID: {robotId} was not found in the database";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            var ongoingMissionRuns = await GetOngoingMissions(robotId, readOnly: true);
            if (ongoingMissionRuns is null || ongoingMissionRuns.Count == 0)
            {
                string errorMessage =
                    $"There were no ongoing mission runs to stop for robot {robotId}";
                logger.LogWarning("{Message}", errorMessage);
                throw new MissionRunNotFoundException(errorMessage);
            }

            var ongoingMissionRunInfos = ongoingMissionRuns
                .Where(missionRun => missionRun.Id == robot.CurrentMissionId)
                .Select(missionRun => (Id: missionRun.Id, IsarMissionId: missionRun.IsarMissionId))
                .ToList();

            try
            {
                foreach (var ongoingMissionRunInfo in ongoingMissionRunInfos)
                {
                    if (ongoingMissionRunInfo.IsarMissionId != null)
                    {
                        logger.LogInformation(
                            "The Isar mission ID we try to stop is"
                                + ongoingMissionRunInfo.IsarMissionId
                        );
                        await isarService.StopMission(robot, ongoingMissionRunInfo.IsarMissionId);
                    }

                    if (stopReason is not null && ongoingMissionRunInfo.Id != null)
                    {
                        await missionRunService.UpdateMissionRunProperty(
                            ongoingMissionRunInfo.Id,
                            "StatusReason",
                            stopReason
                        );
                    }
                }
            }
            catch (HttpRequestException e)
            {
                const string Message = "Error connecting to ISAR while stopping mission";
                logger.LogError(e, "{Message}", Message);
                await errorHandlingService.HandleLosingConnectionToIsar(robot.Id);
                throw new MissionException(Message, 0);
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

            var ongoingMissionRunIds = ongoingMissionRunInfos.Select(info => info.Id).ToList();
            await MoveInterruptedMissionsToQueue(ongoingMissionRunIds);

            try
            {
                await robotService.UpdateCurrentMissionId(robotId, null);
            }
            catch (RobotNotFoundException) { }
        }

        public async Task AbortMissionRun(MissionRun missionRun, string abortReason)
        {
            await missionRunService.UpdateMissionRunProperty(
                missionRun.Id,
                "Status",
                MissionStatus.Aborted
            );
            await missionRunService.UpdateMissionRunProperty(
                missionRun.Id,
                "StatusReason",
                abortReason
            );
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

        public bool MissionRunQueueIsEmpty(IList<MissionRun> missionRunQueue)
        {
            return !missionRunQueue.Any();
        }

        public void TriggerRobotReadyForMissions(RobotReadyForMissionsEventArgs e)
        {
            OnRobotReadyForMissions(e);
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

                logger.LogInformation(
                    "Moving interrupted mission run {MissionRunId} back to the queue",
                    missionRun.Id
                );
                await missionRunService.UpdateMissionRunProperty(
                    missionRun.Id,
                    "Status",
                    MissionStatus.Pending
                );
                _ = signalRService.SendMessageAsync(
                    "Mission run created",
                    missionRun?.InspectionArea?.Installation,
                    missionRun != null ? new MissionRunResponse(missionRun) : null
                );
            }
        }

        private async Task StartMissionRun(MissionRun queuedMissionRun, Robot robot)
        {
            // Reset status reason in case this is a restarted mission run
            if (queuedMissionRun.StatusReason != null)
            {
                await missionRunService.UpdateMissionRunProperty(
                    queuedMissionRun.Id,
                    "StatusReason",
                    null
                );
            }

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

        protected virtual void OnRobotReadyForMissions(RobotReadyForMissionsEventArgs e)
        {
            RobotReadyForMissions?.Invoke(this, e);
        }

        public static event EventHandler<RobotReadyForMissionsEventArgs>? RobotReadyForMissions;
    }
}
