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
        public Task StartNextMissionRunIfSystemIsAvailable(string robotId);

        public Task<bool> OngoingMission(string robotId);

        public Task FreezeMissionRunQueueForRobot(string robotId);

        public Task StopCurrentMissionRun(string robotId);

        public Task CancelAllScheduledMissions(string robotId);

        public Task ScheduleMissionToReturnToSafePosition(string robotId, string areaId);

        public Task UnfreezeMissionRunQueueForRobot(string robotId);

        public bool MissionRunQueueIsEmpty(IList<MissionRun> missionRunQueue);

        public void TriggerRobotAvailable(RobotAvailableEventArgs e);

        public void TriggerMissionCompleted(MissionCompletedEventArgs e);
    }

    public class MissionSchedulingService(ILogger<MissionSchedulingService> logger, IMissionRunService missionRunService, IRobotService robotService,
            IAreaService areaService, IIsarService isarService, ILocalizationService localizationService, IReturnToHomeService returnToHomeService) : IMissionSchedulingService
    {
        public async Task StartNextMissionRunIfSystemIsAvailable(string robotId)
        {
            logger.LogInformation("Starting next mission run if system is available for robot ID: {RobotId}", robotId);
            var robot = await robotService.ReadById(robotId);
            if (robot == null)
            {
                logger.LogError("Robot with ID: {RobotId} was not found in the database", robotId);
                return;
            }

            MissionRun? missionRun;
            try { missionRun = await SelectNextMissionRun(robot.Id); }
            catch (RobotNotFoundException)
            {
                logger.LogError("Robot with ID: {RobotId} was not found in the database", robotId);
                return;
            }

            if (missionRun == null)
            {
                logger.LogInformation("The robot was ready to start mission, but no mission is scheduled");
                if (!robot.MissionQueueFrozen)
                {
                    try { await returnToHomeService.ScheduleReturnToHomeMissionRunIfRobotIsNotHome(robot.Id); }
                    catch (ReturnToHomeMissionFailedToScheduleException)
                    {
                        logger.LogError("Failed to schedule a return to home mission for robot {RobotId}", robot.Id);
                        await robotService.UpdateCurrentArea(robot.Id, null);
                    }
                }
                return;
            }

            if (!await TheSystemIsAvailableToRunAMission(missionRun.Robot.Id, missionRun.Id))
            {
                logger.LogInformation("Mission {MissionRunId} was put on the queue as the system may not start a mission now", missionRun.Id);
                return;
            }


            // Verify that localization is fine
            if (!await localizationService.RobotIsLocalized(missionRun.Robot.Id) && !missionRun.IsLocalizationMission())
            {
                logger.LogError("Tried to schedule mission {MissionRunId} on robot {RobotId} before the robot was localized, scheduled missions will be canceled", missionRun.Id, missionRun.Robot.Id);
                try { await CancelAllScheduledMissions(robot.Id); }
                catch (RobotNotFoundException) { logger.LogError("Failed to cancel scheduled missions for robot {RobotId}", robot.Id); }
                return;
            }

            if (!missionRun.IsLocalizationMission() && !await localizationService.RobotIsOnSameDeckAsMission(missionRun.Robot.Id, missionRun.Area.Id))
            {
                logger.LogError("Robot {RobotId} is not on the same deck as the mission run {MissionRunId}. Cancelling all mission run", missionRun.Robot.Id, missionRun.Id);
                try { await CancelAllScheduledMissions(missionRun.Robot.Id); }
                catch (RobotNotFoundException) { logger.LogError("Failed to cancel scheduled missions for robot {RobotId}", missionRun.Robot.Id); }

                try { await returnToHomeService.ScheduleReturnToHomeMissionRunIfRobotIsNotHome(missionRun.Robot.Id); }
                catch (ReturnToHomeMissionFailedToScheduleException)
                {
                    logger.LogError("Failed to schedule a return to home mission for robot {RobotId}", missionRun.Robot.Id);
                    await robotService.UpdateCurrentArea(missionRun.Robot.Id, null);
                }
                return;
            }

            try { await StartMissionRun(missionRun); }
            catch (Exception ex) when (
                ex is MissionException
                    or RobotNotFoundException
                    or RobotNotAvailableException
                    or MissionRunNotFoundException
                    or IsarCommunicationException)
            {
                const MissionStatus NewStatus = MissionStatus.Failed;
                logger.LogError(
                    ex,
                    "Mission run {MissionRunId} was not started successfully due to {ErrorMessage}",
                    missionRun.Id,
                    ex.Message
                );
                missionRun.Status = NewStatus;
                missionRun.StatusReason = $"Failed to start: '{ex.Message}'";
                await missionRunService.Update(missionRun);
            }
        }

        public async Task<bool> OngoingMission(string robotId)
        {
            var ongoingMissions = await GetOngoingMissions(robotId);
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
            var robot = await robotService.ReadById(robotId);
            if (robot == null)
            {
                string errorMessage = $"Robot with ID: {robotId} was not found in the database";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            var ongoingMissionRuns = await GetOngoingMissions(robotId);
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
                await robotService.SetRobotOffline(robot.Id);
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

        public async Task CancelAllScheduledMissions(string robotId)
        {
            var robot = await robotService.ReadById(robotId);
            if (robot == null)
            {
                string errorMessage = $"Robot with ID: {robotId} was not found in the database";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            var pendingMissionRuns = await missionRunService.ReadMissionRunQueue(robotId);
            if (pendingMissionRuns is null)
            {
                string infoMessage = $"There were no mission runs in the queue to stop for robot {robotId}";
                logger.LogWarning("{Message}", infoMessage);
                return;
            }

            IList<string> pendingMissionRunIds = pendingMissionRuns.Select(missionRun => missionRun.Id).ToList();

            foreach (var pendingMissionRun in pendingMissionRuns)
            {
                pendingMissionRun.Status = MissionStatus.Cancelled;
                await missionRunService.Update(pendingMissionRun);
            }
        }

        public async Task ScheduleMissionToReturnToSafePosition(string robotId, string areaId)
        {
            var area = await areaService.ReadById(areaId);
            if (area == null)
            {
                logger.LogError("Could not find area with ID {AreaId}", areaId);
                return;
            }
            var robot = await robotService.ReadById(robotId);
            if (robot == null)
            {
                logger.LogError("Robot with ID: {RobotId} was not found in the database", robotId);
                return;
            }
            var closestSafePosition = ClosestSafePosition(robot.Pose, area.SafePositions);
            // Cloning to avoid tracking same object
            var clonedPose = ObjectCopier.Clone(closestSafePosition);
            var customTaskQuery = new CustomTaskQuery
            {
                RobotPose = clonedPose,
                Inspections = [],
                TaskOrder = 0
            };

            var missionRun = new MissionRun
            {
                Name = "Drive to Safe Position",
                Robot = robot,
                MissionRunPriority = MissionRunPriority.Emergency,
                InstallationCode = area.Installation.InstallationCode,
                Area = area,
                Status = MissionStatus.Pending,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = new List<MissionTask>(new[]
                {
                    new MissionTask(customTaskQuery)
                }),
                Map = new MapMetadata()
            };

            await missionRunService.Create(missionRun);
        }

        public bool MissionRunQueueIsEmpty(IList<MissionRun> missionRunQueue)
        {
            return !missionRunQueue.Any();
        }

        public void TriggerRobotAvailable(RobotAvailableEventArgs e)
        {
            OnRobotAvailable(e);
        }

        public void TriggerMissionCompleted(MissionCompletedEventArgs e)
        {
            OnMissionCompleted(e);
        }

        private async Task<MissionRun?> SelectNextMissionRun(string robotId)
        {
            var robot = await robotService.ReadById(robotId);
            if (robot == null)
            {
                string errorMessage = $"Could not find robot with id {robotId}";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            MissionRun? missionRun = await missionRunService.ReadNextScheduledLocalizationMissionRun(robot.Id);
            if (missionRun == null) { missionRun = await missionRunService.ReadNextScheduledEmergencyMissionRun(robot.Id); }
            if (robot.MissionQueueFrozen == false && missionRun == null) { missionRun = await missionRunService.ReadNextScheduledMissionRun(robot.Id); }
            return missionRun;
        }

        private async Task MoveInterruptedMissionsToQueue(IEnumerable<string> interruptedMissionRunIds)
        {
            foreach (string missionRunId in interruptedMissionRunIds)
            {
                var missionRun = await missionRunService.ReadById(missionRunId);
                if (missionRun is null)
                {
                    logger.LogWarning("{Message}", $"Interrupted mission run with Id {missionRunId} could not be found");
                    continue;
                }

                var newMissionRun = new MissionRun
                {
                    Name = missionRun.Name,
                    Robot = missionRun.Robot,
                    MissionRunPriority = missionRun.MissionRunPriority,
                    InstallationCode = missionRun.Area!.Installation.InstallationCode,
                    Area = missionRun.Area,
                    Status = MissionStatus.Pending,
                    DesiredStartTime = DateTime.UtcNow,
                    Tasks = missionRun.Tasks,
                    Map = new MapMetadata()
                };

                await missionRunService.Create(newMissionRun);
            }
        }

        private async Task StartMissionRun(MissionRun queuedMissionRun)
        {
            string robotId = queuedMissionRun.Robot.Id;
            string missionRunId = queuedMissionRun.Id;

            var robot = await robotService.ReadById(robotId);
            if (robot == null)
            {
                string errorMessage = $"Could not find robot with id {robotId}";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            if (robot.Status is not RobotStatus.Available)
            {
                string errorMessage = $"Robot {robotId} has status {robot.Status} and is not available";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotAvailableException(errorMessage);
            }

            var missionRun = await missionRunService.ReadById(missionRunId);
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
                await robotService.SetRobotOffline(robot.Id);
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

            missionRun.UpdateWithIsarInfo(isarMission);
            missionRun.Status = MissionStatus.Ongoing;
            await missionRunService.Update(missionRun);

            robot.Status = RobotStatus.Busy;
            await robotService.UpdateRobotStatus(robot.Id, RobotStatus.Busy);
            await robotService.UpdateCurrentMissionId(robot.Id, missionRun.Id);

            logger.LogInformation("Started mission run '{Id}'", queuedMissionRun.Id);
        }

        private static Pose ClosestSafePosition(Pose robotPose, IList<SafePosition> safePositions)
        {
            if (safePositions == null || !safePositions.Any())
            {
                string message = "No safe position for area the robot is localized in";
                throw new SafeZoneException(message);
            }

            var closestPose = safePositions[0].Pose;
            float minDistance = CalculateDistance(robotPose, closestPose);

            for (int i = 1; i < safePositions.Count; i++)
            {
                float currentDistance = CalculateDistance(robotPose, safePositions[i].Pose);
                if (currentDistance < minDistance)
                {
                    minDistance = currentDistance;
                    closestPose = safePositions[i].Pose;
                }
            }
            return closestPose;
        }

        private async Task<PagedList<MissionRun>?> GetOngoingMissions(string robotId)
        {
            var ongoingMissions = await missionRunService.ReadAll(
                new MissionRunQueryStringParameters
                {
                    Statuses = [MissionStatus.Ongoing],
                    RobotId = robotId,
                    OrderBy = "DesiredStartTime",
                    PageSize = 100
                });

            return ongoingMissions;
        }

        private async Task<bool> TheSystemIsAvailableToRunAMission(string robotId, string missionRunId)
        {
            bool ongoingMission = await OngoingMission(robotId);

            var robot = await robotService.ReadById(robotId);
            if (robot is null)
            {
                string errorMessage = $"Robot with ID: {robotId} was not found in the database";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            var missionRun = await missionRunService.ReadById(missionRunId);
            if (missionRun is null)
            {
                string errorMessage = $"Mission run with Id {missionRunId} was not found in the database";
                logger.LogError("{Message}", errorMessage);
                throw new MissionRunNotFoundException(errorMessage);
            }

            if (robot.MissionQueueFrozen && missionRun.MissionRunPriority != MissionRunPriority.Emergency)
            {
                logger.LogInformation("Mission run {MissionRunId} was not started as the mission run queue for robot {RobotName} is frozen", missionRun.Id, robot.Name);
                return false;
            }

            if (ongoingMission)
            {
                logger.LogInformation("Mission run {MissionRunId} was not started as there is already an ongoing mission", missionRun.Id);
                return false;
            }
            if (robot.Status is not RobotStatus.Available)
            {
                logger.LogInformation("Mission run {MissionRunId} was not started as the robot is not available", missionRun.Id);
                return false;
            }
            if (!robot.Enabled)
            {
                logger.LogWarning("Mission run {MissionRunId} was not started as the robot {RobotId} is not enabled", missionRun.Id, robot.Id);
                return false;
            }
            if (await missionRunService.OngoingLocalizationMissionRunExists(robot.Id))
            {
                logger.LogInformation("Mission run {MissionRunId} was not started as there is an ongoing localization mission", missionRun.Id);
                return false;
            }
            return true;
        }

        private static float CalculateDistance(Pose pose1, Pose pose2)
        {
            var pos1 = pose1.Position;
            var pos2 = pose2.Position;
            return (float)Math.Sqrt(Math.Pow(pos1.X - pos2.X, 2) + Math.Pow(pos1.Y - pos2.Y, 2) + Math.Pow(pos1.Z - pos2.Z, 2));
        }

        protected virtual void OnRobotAvailable(RobotAvailableEventArgs e) { RobotAvailable?.Invoke(this, e); }
        public static event EventHandler<RobotAvailableEventArgs>? RobotAvailable;
        protected virtual void OnMissionCompleted(MissionCompletedEventArgs e) { MissionCompleted?.Invoke(this, e); }
        public static event EventHandler<MissionCompletedEventArgs>? MissionCompleted;
    }
}
