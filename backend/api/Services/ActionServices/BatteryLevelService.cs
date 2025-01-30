﻿using Api.Database.Models;

namespace Api.Services.ActionServices
{
    public interface IBatteryLevelService
    {
        public Task<Robot?> UpdateBatteryLevel(float batteryLevel, string isarId);
    }

    public class BatteryLevelService(
        ILogger<BatteryLevelService> logger,
        IRobotService robotService
    ) : IBatteryLevelService
    {
        private const double Tolerance = 1E-05D;

        public async Task<Robot?> UpdateBatteryLevel(float batteryLevel, string isarId)
        {
            var robot = await robotService.ReadByIsarId(isarId, readOnly: true);
            if (robot == null)
            {
                logger.LogWarning(
                    "Could not find corresponding robot for battery update on robot with ISAR id'{IsarId}'",
                    isarId
                );
                return null;
            }

            try
            {
                if (Math.Abs(batteryLevel - robot.BatteryLevel) > Tolerance)
                {
                    await robotService.UpdateRobotBatteryLevel(robot.Id, batteryLevel);
                    robot.BatteryLevel = batteryLevel;
                }
            }
            catch (Exception e)
            {
                logger.LogWarning(
                    "Failed to update robot battery value for robot with ID '{isarId}'. Exception: {message}",
                    isarId,
                    e.Message
                );
                return null;
            }

            logger.LogDebug(
                "Updated battery on robot '{RobotName}' with ISAR id '{IsarId}'",
                robot.Name,
                robot.IsarId
            );
            return robot;
        }
    }
}
