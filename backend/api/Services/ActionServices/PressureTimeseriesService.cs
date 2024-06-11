namespace Api.Services.ActionServices
{
    public interface IPressureTimeseriesService
    {
        public Task AddPressureEntry(float pressureLevel, string isarId);
    }

    public class PressureTimeseriesService(ILogger<PressureTimeseriesService> logger, IRobotService robotService) : IPressureTimeseriesService
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

            try
            {
                if (robot.PressureLevel is null || Math.Abs(pressureLevel - (float)robot.PressureLevel) > Tolerance) await robotService.UpdateRobotPressureLevel(robot.Id, pressureLevel);
            }
            catch (Exception e)
            {
                logger.LogWarning("Failed to update robot pressure value for robot with ID '{isarId}'. Exception: {message}", isarId, e.Message);
                return;
            }

            logger.LogDebug("Updated pressure on robot '{RobotName}' with ISAR id '{IsarId}'", robot.Name, robot.IsarId);
        }
    }
}
