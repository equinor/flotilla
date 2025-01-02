using Api.Database.Models;

namespace Api.Services.Events
{
    public class MissionRunCreatedEventArgs(string missionRunId) : EventArgs
    {
        public string MissionRunId { get; } = missionRunId;
    }

    public class RobotAvailableEventArgs(Robot robot) : EventArgs
    {
        public Robot Robot { get; } = robot;
    }

    public class RobotEmergencyEventArgs(Robot robot, RobotFlotillaStatus robotFlotillaStatus)
        : EventArgs
    {
        public Robot Robot { get; } = robot;
        public RobotFlotillaStatus RobotFlotillaStatus { get; } = robotFlotillaStatus;
    }

    public class TeamsMessageEventArgs(string teamsMessage) : EventArgs
    {
        public string TeamsMessage { get; } = teamsMessage;
    }
}
