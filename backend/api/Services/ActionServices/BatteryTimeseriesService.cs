using Api.Database.Models;

namespace Api.Services.ActionServices
{
    public interface IBatteryTimeseriesService
    {
        public Task<Robot?> AddBatteryEntry(float batteryLevel, string isarId);
    }

    public class BatteryTimeseriesService(ILogger<BatteryTimeseriesService> logger, IRobotService robotService) : IBatteryTimeseriesService
    {
        private const double Tolerance = 1E-05D;

        public async Task<Robot?> AddBatteryEntry(float batteryLevel, string isarId)
        {
            var robot = await robotService.ReadByIsarId(isarId);
            if (robot == null)
            {
                logger.LogWarning("Could not find corresponding robot for battery update on robot with ISAR id'{IsarId}'", isarId);
                return null;
            }

            try
            {
                if (Math.Abs(batteryLevel - robot.BatteryLevel) > Tolerance)
                {
                    robot = await robotService.UpdateRobotBatteryLevel(robot.Id, batteryLevel);
                }
            }
            catch (Exception e)
            {
                logger.LogWarning("Failed to update robot battery value for robot with ID '{isarId}'. Exception: {message}", isarId, e.Message);
                return null;
            }

            logger.LogDebug("Updated battery on robot '{RobotName}' with ISAR id '{IsarId}'", robot.Name, robot.IsarId);
            return robot;
        }
    }
}
