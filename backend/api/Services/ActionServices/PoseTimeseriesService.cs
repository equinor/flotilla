using Api.Database.Models;
namespace Api.Services.ActionServices
{
    public interface IPoseTimeseriesService
    {
        public Task AddPoseEntry(Pose pose, string isarId);
    }

    public class PoseTimeseriesService : IPoseTimeseriesService
    {
        private readonly ILogger<PoseTimeseriesService> _logger;
        private readonly IRobotService _robotService;
        private readonly ITimeseriesService _timeseriesService;

        public PoseTimeseriesService(ILogger<PoseTimeseriesService> logger, IRobotService robotService, ITimeseriesService timeseriesService)
        {
            _logger = logger;
            _robotService = robotService;
            _timeseriesService = timeseriesService;
        }

        public async Task AddPoseEntry(Pose pose, string isarId)
        {
            var robot = await _robotService.ReadByIsarId(isarId);
            if (robot == null)
            {
                _logger.LogWarning(
                    "Could not find corresponding robot for pose update on robot with ISAR id '{IsarId}'", isarId);
                return;
            }

            await _robotService.UpdateRobotPose(robot.Id, pose);
            await _timeseriesService.Create(
                new RobotPoseTimeseries(robot.Pose)
                {
                    MissionId = robot.CurrentMissionId,
                    RobotId = robot.Id,
                    Time = DateTime.UtcNow
                }
            );
            _logger.LogDebug("Updated pose on robot '{RobotName}' with ISAR id '{IsarId}'", robot.Name, robot.IsarId);

        }
    }
}
