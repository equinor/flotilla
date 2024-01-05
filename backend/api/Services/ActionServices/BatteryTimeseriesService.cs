using Npgsql;

namespace Api.Services.ActionServices
{
    public interface IBatteryTimeseriesService
    {
        public Task AddBatteryEntry(float batteryLevel, string isarId);
    }

    public class BatteryTimeseriesService(ILogger<BatteryTimeseriesService> logger, IRobotService robotService, ITimeseriesService timeseriesService) : IBatteryTimeseriesService
    {
        private const double Tolerance = 1E-05D;

        public async Task AddBatteryEntry(float batteryLevel, string isarId)
        {
            var robot = await robotService.ReadByIsarId(isarId);
            if (robot == null)
            {
                logger.LogWarning("Could not find corresponding robot for battery update on robot with ISAR id'{IsarId}'", isarId);
                return;
            }

            if (Math.Abs(batteryLevel - robot.BatteryLevel) > Tolerance) await robotService.UpdateRobotBatteryLevel(robot.Id, batteryLevel);

            try { await timeseriesService.AddBatteryEntry(robot.CurrentMissionId!, batteryLevel, robot.Id); }
            catch (NpgsqlException e)
            {
                logger.LogError(e, "An error occurred while connecting to the timeseries database");
                return;
            }
            logger.LogDebug("Updated battery on robot '{RobotName}' with ISAR id '{IsarId}'", robot.Name, robot.IsarId);
        }
    }
}
