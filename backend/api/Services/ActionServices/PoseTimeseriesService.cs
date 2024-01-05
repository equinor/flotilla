using Api.Database.Models;
namespace Api.Services.ActionServices
{
    public interface IPoseTimeseriesService
    {
        public Task AddPoseEntry(Pose pose, string isarId);
    }

    public class PoseTimeseriesService(ILogger<PoseTimeseriesService> logger, IRobotService robotService, ITimeseriesService timeseriesService) : IPoseTimeseriesService
    {
        public async Task AddPoseEntry(Pose pose, string isarId)
        {
            var robot = await robotService.ReadByIsarId(isarId);
            if (robot == null)
            {
                logger.LogWarning("Could not find corresponding robot for pose update on robot with ISAR id '{IsarId}'", isarId);
                return;
            }

            await robotService.UpdateRobotPose(robot.Id, pose);
            await timeseriesService.AddPoseEntry(robot.CurrentMissionId!, pose, robot.Id);
            logger.LogDebug("Updated pose on robot '{RobotName}' with ISAR id '{IsarId}'", robot.Name, robot.IsarId);
        }
    }
}
