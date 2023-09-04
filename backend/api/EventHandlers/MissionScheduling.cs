using System.Text.Json;
using Api.Controllers;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Utilities;
using Microsoft.AspNetCore.Mvc;
namespace Api.EventHandlers
{
    public interface IMissionScheduling
    {
        public void StartMissionRunIfSystemIsAvailable(MissionRun missionRun);

        public Task<bool> TheSystemIsAvailableToRunAMission(string robotId, MissionRun missionRun);

        public Task<bool> OngoingMission(string robotId);

        public void StartMissionRun(MissionRun queuedMissionRun);

        public Task FreezeMissionRunQueueForRobot(string robotId);

        public Task StopCurrentMissionRun(string robotId);

        public Task ScheduleMissionToReturnToSafePosition(string robotId, string areaId);

        public Task UnfreezeMissionRunQueueForRobot(string robotId);

    }

    public class MissionScheduling : IMissionScheduling
    {
        private readonly IIsarService _isarService;
        private readonly ILogger<MissionScheduling> _logger;
        private readonly IMissionRunService _missionRunService;
        private readonly IAreaService _areaService;
        private readonly RobotController _robotController;
        private readonly IRobotService _robotService;

        public MissionScheduling(ILogger<MissionScheduling> logger, IMissionRunService missionRunService, IIsarService isarService, IRobotService robotService, RobotController robotController, IAreaService areaService)
        {
            _logger = logger;
            _missionRunService = missionRunService;
            _isarService = isarService;
            _robotService = robotService;
            _robotController = robotController;
            _areaService = areaService;
        }

        public void StartMissionRunIfSystemIsAvailable(MissionRun missionRun)
        {
            if (!TheSystemIsAvailableToRunAMission(missionRun.Robot, missionRun).Result)
            {
                _logger.LogInformation("Mission {MissionRunId} was put on the queue as the system may not start a mission now", missionRun.Id);
                return;
            }

            try
            {
                StartMissionRun(missionRun);
            }
            catch (MissionException ex)
            {
                const MissionStatus NewStatus = MissionStatus.Failed;
                _logger.LogWarning(
                    "Mission run {MissionRunId} was not started successfully. Status updated to '{Status}'.\nReason: {FailReason}",
                    missionRun.Id,
                    NewStatus,
                    ex.Message
                );
                missionRun.Status = NewStatus;
                missionRun.StatusReason = $"Failed to start: '{ex.Message}'";
                _missionRunService.Update(missionRun);
            }
        }

        public async Task<bool> TheSystemIsAvailableToRunAMission(string robotId, MissionRun missionRun)
        {
            var robot = await _robotService.ReadById(robotId);
            if (robot == null)
            {
                _logger.LogError("Robot with ID: {RobotId} was not found in the database", robotId);
                return false;
            }
            return await TheSystemIsAvailableToRunAMission(robot, missionRun);
        }

        public async Task<bool> TheSystemIsAvailableToRunAMission(Robot robot, MissionRun missionRun)
        {
            bool ongoingMission = await OngoingMission(robot.Id);

            if (robot.MissionQueueFrozen && missionRun.MissionRunPriority != MissionRunPriority.Emergency)
            {
                _logger.LogInformation("Mission run {MissionRunId} was not started as the mission run queue for robot {RobotName} is frozen", missionRun.Id, robot.Name);
                return false;
            }

            if (ongoingMission)
            {
                _logger.LogInformation("Mission run {MissionRunId} was not started as there is already an ongoing mission", missionRun.Id);
                return false;
            }
            if (robot.Status is not RobotStatus.Available)
            {
                _logger.LogInformation("Mission run {MissionRunId} was not started as the robot is not available", missionRun.Id);
                return false;
            }
            if (!robot.Enabled)
            {
                _logger.LogWarning("Mission run {MissionRunId} was not started as the robot {RobotId} is not enabled", missionRun.Id, robot.Id);
                return false;
            }
            if (missionRun.DesiredStartTime > DateTimeOffset.UtcNow)
            {
                _logger.LogInformation("Mission run {MissionRunId} was not started as the start time is in the future", missionRun.Id);
                return false;
            }
            return true;
        }

        public void StartMissionRun(MissionRun queuedMissionRun)
        {
            var result = _robotController.StartMission(
                queuedMissionRun.Robot.Id,
                queuedMissionRun.Id
            ).Result;
            if (result.Result is not OkObjectResult)
            {
                string errorMessage = "Unknown error from robot controller";
                if (result.Result is ObjectResult returnObject)
                {
                    errorMessage = returnObject.Value?.ToString() ?? errorMessage;
                }
                throw new MissionException(errorMessage);
            }
            _logger.LogInformation("Started mission run '{Id}'", queuedMissionRun.Id);
        }

        public async Task<bool> OngoingMission(string robotId)
        {
            var ongoingMissions = await _missionRunService.ReadAll(
                new MissionRunQueryStringParameters
                {
                    Statuses = new List<MissionStatus>
                    {
                        MissionStatus.Ongoing
                    },
                    RobotId = robotId,
                    OrderBy = "DesiredStartTime",
                    PageSize = 100
                });

            return ongoingMissions.Any();
        }

        public async Task FreezeMissionRunQueueForRobot(string robotId)
        {
            var robot = (await _robotService.ReadById(robotId))!;
            robot.MissionQueueFrozen = true;
            await _robotService.Update(robot);
            _logger.LogInformation("Mission queue for robot {RobotName} with ID {RobotId} was frozen", robot.Name, robot.Id);
        }

        public async Task UnfreezeMissionRunQueueForRobot(Robot robot)
        {
            robot.MissionQueueFrozen = false;
            await _robotService.Update(robot);
            _logger.LogInformation("Mission queue for robot {RobotName} with ID {RobotId} was unfrozen", robot.Name, robot.Id);
        }

        public async Task UnfreezeMissionRunQueueForRobot(string robotId)
        {
            var robot = await _robotService.ReadById(robotId);
            if (robot == null)
            {
                _logger.LogError("Robot with ID: {RobotId} was not found in the database", robotId);
                return;
            }
            await UnfreezeMissionRunQueueForRobot(robot);
        }

        public async Task StopCurrentMissionRun(string robotId)
        {
            var robot = await _robotService.ReadById(robotId);
            if (robot == null)
            {
                _logger.LogError("Robot with ID: {RobotId} was not found in the database", robotId);
                return;
            }
            if (!await OngoingMission(robot.Id))
            {
                _logger.LogWarning("Flotilla has no mission running for robot {RobotName} but an attempt to stop will be made regardless", robot.Name);
            }

            try
            {
                await _isarService.StopMission(robot);
            }
            catch (HttpRequestException e)
            {
                string message = "Error connecting to ISAR while stopping mission";
                _logger.LogError(e, "{Message}", message);
                OnIsarUnavailable(robot);
                throw new MissionException(message, (int)e.StatusCode!);
            }
            catch (MissionException e)
            {
                string message = "Error while stopping ISAR mission";
                _logger.LogError(e, "{Message}", message);
                throw;
            }
            catch (JsonException e)
            {
                string message = "Error while processing the response from ISAR";
                _logger.LogError(e, "{Message}", message);
                throw new MissionException(message, 0);
            }

            robot.CurrentMissionId = null;
            await _robotService.Update(robot);
        }

        public async Task ScheduleMissionToReturnToSafePosition(string robotId, string areaId)
        {
            var area = await _areaService.ReadById(areaId);
            if (area == null)
            {
                _logger.LogError("Could not find area with ID {AreaId}", areaId);
                return;
            }
            var robot = await _robotService.ReadById(robotId);
            if (robot == null)
            {
                _logger.LogError("Robot with ID: {RobotId} was not found in the database", robotId);
                return;
            }
            var closestSafePosition = ClosestSafePosition(robot.Pose, area.SafePositions);
            // Cloning to avoid tracking same object
            var clonedPose = ObjectCopier.Clone(closestSafePosition);
            var customTaskQuery = new CustomTaskQuery
            {
                RobotPose = clonedPose,
                Inspections = new List<CustomInspectionQuery>(),
                InspectionTarget = new Position(),
                TaskOrder = 0
            };

            var missionRun = new MissionRun
            {
                Name = "Drive to Safe Position",
                Robot = robot,
                MissionRunPriority = MissionRunPriority.Emergency,
                InstallationCode = area.Installation!.InstallationCode,
                Area = area,
                Status = MissionStatus.Pending,
                DesiredStartTime = DateTimeOffset.UtcNow,
                Tasks = new List<MissionTask>(new[]
                {
                    new MissionTask(customTaskQuery)
                }),
                Map = new MapMetadata()
            };

            await _missionRunService.Create(missionRun);
        }

        public static bool MissionRunQueueIsEmpty(IList<MissionRun> missionRunQueue)
        {
            return !missionRunQueue.Any();
        }
        private async void OnIsarUnavailable(Robot robot)
        {
            robot.Enabled = false;
            robot.Status = RobotStatus.Offline;
            if (robot.CurrentMissionId != null)
            {
                var missionRun = await _missionRunService.ReadById(robot.CurrentMissionId);
                if (missionRun != null)
                {
                    missionRun.SetToFailed();
                    await _missionRunService.Update(missionRun);
                    _logger.LogWarning(
                        "Mission '{Id}' failed because ISAR could not be reached",
                        missionRun.Id
                    );
                }
            }
            robot.CurrentMissionId = null;
            await _robotService.Update(robot);
        }

        public static Pose ClosestSafePosition(Pose robotPose, IList<SafePosition> safePositions)
        {
            if (safePositions == null || !safePositions.Any())
            {
                throw new ArgumentException("List of safe positions cannot be null or empty.");
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

        private static float CalculateDistance(Pose pose1, Pose pose2)
        {
            var pos1 = pose1.Position;
            var pos2 = pose2.Position;
            return (float)Math.Sqrt(Math.Pow(pos1.X - pos2.X, 2) + Math.Pow(pos1.Y - pos2.Y, 2) + Math.Pow(pos1.Z - pos2.Z, 2));
        }
    }
}
