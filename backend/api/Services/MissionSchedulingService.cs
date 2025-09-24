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

        public Task MoveCurrentMissionRunBackToQueue(string robotId, string? stopReason = null);

        public Task<MissionRun> MoveMissionRunBackToQueue(
            string robotId,
            string isarMissionRunId,
            string? stopReason = null
        );

        public Task DeleteAllScheduledMissions(string robotId, string? abortReason = null);

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
        IAreaPolygonService areaPolygonService,
        IExclusionAreaService exclusionAreaService
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
                missionRun = await missionRunService.ReadNextScheduledMissionRun(
                    robot.Id,
                    readOnly: true
                );
            }
            catch (RobotNotFoundException)
            {
                logger.LogError("Robot with ID: {RobotId} was not found in the database", robot.Id);
                return;
            }

            if (missionRun == null)
                return;

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

            missionRun.Tasks = await exclusionAreaService.FilterOutExcludedMissionTasks(
                missionRun.Tasks,
                missionRun.InstallationCode
            );

            if (missionRun.Tasks.Count == 0)
            {
                logger.LogWarning(
                    "MissionRun {RobotName} was not started on robot {RobotId} as all its tasks are in exclusion areas",
                    missionRun.Id,
                    robot.Id
                );
                try
                {
                    await AbortMissionRun(
                        missionRun,
                        $"Mission run {missionRun.Id} aborted: All tasks are in exclusion areas"
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
                return;
            }

            await missionRunService.UpdateMissionRunProperty(
                missionRun.Id,
                "Tasks",
                missionRun.Tasks
            );

            if (
                !areaPolygonService.MissionTasksAreInsideAreaPolygon(
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

            var ongoingMissionRunIds = ongoingMissionRuns
                .Where(missionRun => missionRun.Id == robot.CurrentMissionId)
                .Select(missionRun => missionRun.Id)
                .ToList();

            try
            {
                foreach (var ongoingMissionRunId in ongoingMissionRunIds)
                {
                    logger.LogInformation(
                        $"Sending request to stop mission with ID {ongoingMissionRunId}"
                    );
                    await isarService.StopMission(robot, ongoingMissionRunId);

                    if (stopReason is not null)
                    {
                        await missionRunService.UpdateMissionRunProperty(
                            ongoingMissionRunId,
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

            await MoveInterruptedMissionsToQueue(ongoingMissionRunIds);

            try
            {
                await robotService.UpdateCurrentMissionId(robotId, null);
            }
            catch (RobotNotFoundException) { }
        }

        public async Task<MissionRun> MoveMissionRunBackToQueue(
            string robotId,
            string isarMissionRunId,
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

            var missionRun = await missionRunService.ReadById(isarMissionRunId, readOnly: true);
            if (missionRun is null)
            {
                string errorMessage =
                    $"Mission {isarMissionRunId} on robot {robotId} was not found and could not be put back on the queue";
                logger.LogWarning("{Message}", errorMessage);
                throw new MissionRunNotFoundException(errorMessage);
            }

            await missionRunService.UpdateMissionRunProperty(
                missionRun.Id,
                "StatusReason",
                stopReason
            );

            await missionRunService.UpdateMissionRunProperty(
                missionRun.Id,
                "Status",
                MissionStatus.Pending
            );
            _ = signalRService.SendMessageAsync(
                "Mission run created",
                missionRun.InspectionArea.Installation,
                new MissionRunResponse(missionRun)
            );

            try
            {
                if (robot.CurrentMissionId == missionRun.Id)
                {
                    await robotService.UpdateCurrentMissionId(robot.Id, null);
                }
            }
            catch (RobotNotFoundException) { }
            catch (MissionNotFoundException)
            {
                logger.LogError(
                    "Mission not found when setting last mission run for mission definition {missionId}",
                    missionRun.MissionId
                );
            }

            return missionRun;
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

        public async Task DeleteAllScheduledMissions(string robotId, string? abortReason)
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
                await missionRunService.Delete(pendingMissionRun.Id);
            }
        }

        public void TriggerRobotReadyForMissions(RobotReadyForMissionsEventArgs e)
        {
            RobotReadyForMissions?.Invoke(this, e);
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
                    missionRun.InspectionArea.Installation,
                    new MissionRunResponse(missionRun)
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
                    OrderBy = "CreationTime",
                    PageSize = 100,
                },
                readOnly: readOnly
            );

            return ongoingMissions;
        }

        public static event EventHandler<RobotReadyForMissionsEventArgs>? RobotReadyForMissions;
    }
}
