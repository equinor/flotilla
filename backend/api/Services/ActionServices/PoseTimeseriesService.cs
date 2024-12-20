using Api.Database.Models;

namespace Api.Services.ActionServices
{
    public interface IPoseTimeseriesService
    {
        public Task AddPoseEntry(Pose pose, string isarId);
    }

    public class PoseTimeseriesService(
        ILogger<PoseTimeseriesService> logger,
        IRobotService robotService
    ) : IPoseTimeseriesService
    {
        public async Task AddPoseEntry(Pose pose, string isarId)
        {
            var robot = await robotService.ReadByIsarId(isarId, readOnly: true);
            if (robot == null)
            {
                logger.LogWarning(
                    "Could not find corresponding robot for pose update on robot with ISAR id '{IsarId}'",
                    isarId
                );
                return;
            }

            try
            {
                await robotService.UpdateRobotPose(robot.Id, pose);
            }
            catch (Exception e)
            {
                logger.LogWarning(
                    "Failed to update robot pose value for robot with ID '{isarId}'. Exception: {message}",
                    isarId,
                    e.Message
                );
                return;
            }

            logger.LogDebug(
                "Updated pose on robot '{RobotName}' with ISAR id '{IsarId}'",
                robot.Name,
                robot.IsarId
            );
        }
    }
}
