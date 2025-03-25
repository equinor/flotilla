using Api.Database.Models;

namespace Api.Services.Events
{
    public class MissionRunCreatedEventArgs(MissionRun missionRun) : EventArgs
    {
        public MissionRun MissionRun { get; } = missionRun;
    }

    public class RobotStatusThatCanReceiveMissionEventArgs(Robot robot) : EventArgs
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
