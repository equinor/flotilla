using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.Events;
using Api.Utilities;
namespace Api.EventHandlers
{
    public interface IMissionScheduling
    {
        public void StartMissionRunIfSystemIsAvailable(MissionRun missionRun);

        public Task<bool> OngoingMission(string robotId);

        public Task FreezeMissionRunQueueForRobot(string robotId);

        public Task StopCurrentMissionRun(string robotId);

        public Task ScheduleMissionToReturnToSafePosition(string robotId, string areaId);

        public Task UnfreezeMissionRunQueueForRobot(string robotId);

        public void TriggerRobotAvailable(RobotAvailableEventArgs e);
    }

    public class MissionScheduling : IMissionScheduling
    {
        private readonly IAreaService _areaService;
        private readonly IIsarService _isarService;
        private readonly ILogger<MissionScheduling> _logger;
        private readonly IMissionRunService _missionRunService;
        private readonly IMissionSchedulingService _missionSchedulingService;
        private readonly IRobotService _robotService;

        public MissionScheduling(ILogger<MissionScheduling> logger, IMissionRunService missionRunService, IIsarService isarService, IRobotService robotService, IAreaService areaService,
            IMissionSchedulingService missionSchedulingService)
        {
            _logger = logger;
            _missionRunService = missionRunService;
            _isarService = isarService;
            _robotService = robotService;
            _areaService = areaService;
            _missionSchedulingService = missionSchedulingService;
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
                _missionSchedulingService.StartMissionRun(missionRun);
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
            var robot = await _robotService.ReadById(robotId);
            if (robot == null)
            {
                _logger.LogError("Robot with ID: {RobotId} was not found in the database", robotId);
                return;
            }
            robot.MissionQueueFrozen = true;
            await _robotService.Update(robot);
            _logger.LogInformation("Mission queue for robot {RobotName} with ID {RobotId} was frozen", robot.Name, robot.Id);
        }

        public async Task UnfreezeMissionRunQueueForRobot(string robotId)
        {
            var robot = await _robotService.ReadById(robotId);
            if (robot == null)
            {
                _logger.LogError("Robot with ID: {RobotId} was not found in the database", robotId);
                return;
            }
            robot.MissionQueueFrozen = false;
            await _robotService.Update(robot);
            _logger.LogInformation("Mission queue for robot {RobotName} with ID {RobotId} was unfrozen", robot.Name, robot.Id);
        }

        public async Task StopCurrentMissionRun(string robotId)
        {
            var robot = await _robotService.ReadById(robotId);
            if (robot == null)
            {
                _logger.LogError("Robot with ID: {RobotId} was not found in the database", robotId);
                return;
            }

            var ongoingMissions = await GetOngoingMissions(robot.Id);

            if (ongoingMissions == null)
            {
                _logger.LogWarning("Flotilla has no mission running for robot {RobotName} but an attempt to stop will be made regardless", robot.Name);
            }
            else
            {
                foreach (var mission in ongoingMissions)
                {
                    if (mission.MissionRunPriority == MissionRunPriority.Emergency) { continue; }

                    var newMission = new MissionRun
                    {
                        Name = mission.Name,
                        Robot = robot,
                        MissionRunPriority = MissionRunPriority.Normal,
                        InstallationCode = mission.InstallationCode,
                        Area = mission.Area,
                        Status = MissionStatus.Pending,
                        DesiredStartTime = DateTime.UtcNow,
                        Tasks = mission.Tasks,
                        Map = new MapMetadata()
                    };

                    await _missionRunService.Create(newMission);
                }
            }

            try
            {
                await _isarService.StopMission(robot);
            }
            catch (HttpRequestException e)
            {
                const string Message = "Error connecting to ISAR while stopping mission";
                _logger.LogError(e, "{Message}", Message);
                _missionSchedulingService.OnIsarUnavailable(robot.Id);
                throw new MissionException(Message, (int)e.StatusCode!);
            }
            catch (MissionException e)
            {
                const string Message = "Error while stopping ISAR mission";
                _logger.LogError(e, "{Message}", Message);
                throw;
            }
            catch (JsonException e)
            {
                const string Message = "Error while processing the response from ISAR";
                _logger.LogError(e, "{Message}", Message);
                throw new MissionException(Message, 0);
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
            var closestSafePosition = _missionSchedulingService.ClosestSafePosition(robot.Pose, area.SafePositions);
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

            await _missionRunService.Create(missionRun);
        }

        public void TriggerRobotAvailable(RobotAvailableEventArgs e)
        {
            OnRobotAvailable(e);
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

        private async Task<PagedList<MissionRun>?> GetOngoingMissions(string robotId)
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

            return ongoingMissions;
        }

        private async Task<bool> TheSystemIsAvailableToRunAMission(Robot robot, MissionRun missionRun)
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
            return true;
        }

        public static bool MissionRunQueueIsEmpty(IList<MissionRun> missionRunQueue)
        {
            return !missionRunQueue.Any();
        }

        protected virtual void OnRobotAvailable(RobotAvailableEventArgs e) { RobotAvailable?.Invoke(this, e); }
        public static event EventHandler<RobotAvailableEventArgs>? RobotAvailable;
    }
}
