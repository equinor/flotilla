using Api.Controllers;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.AspNetCore.Mvc;
namespace Api.Services
{
    public interface IMissionSchedulingService
    {
        public void StartMissionRun(MissionRun queuedMissionRun);
        public Pose ClosestSafePosition(Pose robotPose, IList<SafePosition> safePositions);
        public void OnIsarUnavailable(string robotId);
    }

    public class MissionSchedulingService : IMissionSchedulingService
    {
        private readonly ILogger<MissionSchedulingService> _logger;
        private readonly IMissionRunService _missionRunService;
        private readonly RobotController _robotController;
        private readonly IRobotService _robotService;

        public MissionSchedulingService(ILogger<MissionSchedulingService> logger, IMissionRunService missionRunService, IRobotService robotService, RobotController robotController)
        {
            _logger = logger;
            _missionRunService = missionRunService;
            _robotService = robotService;
            _robotController = robotController;
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

        public async void OnIsarUnavailable(string robotId)
        {
            var robot = await _robotService.ReadById(robotId);
            if (robot == null)
            {
                _logger.LogError("Robot with ID: {RobotId} was not found in the database", robotId);
                return;
            }

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

        public Pose ClosestSafePosition(Pose robotPose, IList<SafePosition> safePositions)
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
