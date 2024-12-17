using Api.Database.Models;

namespace Api.Services.ActionServices
{
    public interface IPressureTimeseriesService
    {
        public Task<Robot?> AddPressureEntry(float pressureLevel, string isarId);
    }

    public class PressureTimeseriesService(ILogger<PressureTimeseriesService> logger, IRobotService robotService) : IPressureTimeseriesService
    {
        private const double Tolerance = 1E-05D;

        public async Task<Robot?> AddPressureEntry(float pressureLevel, string isarId)
        {
            var robot = await robotService.ReadByIsarId(isarId, readOnly: true);
            if (robot == null)
            {
                logger.LogWarning("Could not find corresponding robot for pressure update on robot with ISAR id'{IsarId}'", isarId);
                return null;
            }

            try
            {
                if (robot.PressureLevel is null || Math.Abs(pressureLevel - (float)robot.PressureLevel) > Tolerance)
                {
                    await robotService.UpdateRobotPressureLevel(robot.Id, pressureLevel);
                    robot.PressureLevel = pressureLevel;
                }
            }
            catch (Exception e)
            {
                logger.LogWarning("Failed to update robot pressure value for robot with ID '{isarId}'. Exception: {message}", isarId, e.Message);
                return null;
            }

            logger.LogDebug("Updated pressure on robot '{RobotName}' with ISAR id '{IsarId}'", robot.Name, robot.IsarId);
            return robot;
        }
    }
}
