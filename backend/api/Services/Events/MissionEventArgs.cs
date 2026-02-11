using Api.Database.Models;

namespace Api.Services.Events
{
    public class RobotReadyForMissionsEventArgs(Robot robot) : EventArgs
    {
        public Robot Robot { get; } = robot;
    }

    public class RobotEmergencyEventArgs(Robot robot, string? message = null) : EventArgs
    {
        public Robot Robot { get; } = robot;
        public string? Message { get; } = message;
    }

    public class TeamsMessageEventArgs(string teamsMessage) : EventArgs
    {
        public string TeamsMessage { get; } = teamsMessage;
    }
}
