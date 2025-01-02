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

    public class RobotEmergencyEventArgs(string robotId, RobotFlotillaStatus robotFlotillaStatus)
        : EventArgs
    {
        public string RobotId { get; } = robotId;
        public RobotFlotillaStatus RobotFlotillaStatus { get; } = robotFlotillaStatus;
    }

    public class TeamsMessageEventArgs(string teamsMessage) : EventArgs
    {
        public string TeamsMessage { get; } = teamsMessage;
    }
}
