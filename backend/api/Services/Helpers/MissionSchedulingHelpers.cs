using Api.Database.Models;

namespace Api.Services.Helpers;

public static class MissionSchedulingHelpers
{
    public static bool TheSystemIsAvailableToRunAMission(
        Robot robot,
        MissionRun missionRun,
        ILogger logger
    )
    {
        if (robot.MissionQueueFrozen && !missionRun.IsEmergencyMission())
        {
            logger.LogInformation(
                "Mission run {MissionRunId} was not started as the mission run queue for robot {RobotName} is frozen",
                missionRun.Id,
                robot.Name
            );
            return false;
        }

        switch (robot.Status)
        {
            case RobotStatus.Available:
            case RobotStatus.Home:
            case RobotStatus.ReturningHome:
                break;
            default:
                logger.LogInformation(
                    "Mission run {MissionRunId} was not started as the robot is not available, home or returning home",
                    missionRun.Id
                );
                return false;
        }

        if (!robot.IsarConnected)
        {
            logger.LogWarning(
                "Mission run {MissionRunId} was not started as the robots {RobotId} isar instance is disconnected",
                missionRun.Id,
                robot.Id
            );
            return false;
        }
        if (robot.Deprecated)
        {
            logger.LogWarning(
                "Mission run {MissionRunId} was not started as the robot {RobotId} is deprecated",
                missionRun.Id,
                robot.Id
            );
            return false;
        }
        return true;
    }
}
