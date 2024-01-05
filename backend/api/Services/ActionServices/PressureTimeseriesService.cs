using Npgsql;

namespace Api.Services.ActionServices
{
    public interface IPressureTimeseriesService
    {
        public Task AddPressureEntry(float pressureLevel, string isarId);
    }

    public class PressureTimeseriesService(ILogger<PressureTimeseriesService> logger, IRobotService robotService, ITimeseriesService timeseriesService) : IPressureTimeseriesService
    {
        private const double Tolerance = 1E-05D;

        public async Task AddPressureEntry(float pressureLevel, string isarId)
        {
            var robot = await robotService.ReadByIsarId(isarId);
            if (robot == null)
            {
                logger.LogWarning("Could not find corresponding robot for pressure update on robot with ISAR id'{IsarId}'", isarId);
                return;
            }

            if (robot.PressureLevel is null) return;

            if (Math.Abs(pressureLevel - (float)robot.PressureLevel) > Tolerance) await robotService.UpdateRobotPressureLevel(robot.Id, pressureLevel);

            try { await timeseriesService.AddPressureEntry(robot.CurrentMissionId!, pressureLevel, robot.Id); }
            catch (NpgsqlException e)
            {
                logger.LogError(e, "An error occurred while connecting to the timeseries database");
                return;
            }
            logger.LogDebug("Updated pressure on robot '{RobotName}' with ISAR id '{IsarId}'", robot.Name, robot.IsarId);
        }
    }
}
