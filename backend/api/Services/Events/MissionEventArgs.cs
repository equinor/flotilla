namespace Api.Services.Events
{
    public class MissionRunCreatedEventArgs(string missionRunId) : EventArgs
    {
        public string MissionRunId { get; } = missionRunId;
    }

    public class RobotAvailableEventArgs(string robotId) : EventArgs
    {
        public string RobotId { get; } = robotId;
    }
    public class MissionCompletedEventArgs(string robotId) : EventArgs
    {
        public string RobotId { get; } = robotId;
    }

    public class EmergencyButtonPressedForRobotEventArgs(string robotId) : EventArgs
    {
        public string RobotId { get; } = robotId;
    }

    public class TeamsMessageEventArgs(string teamsMessage) : EventArgs
    {
        public string TeamsMessage { get; } = teamsMessage;
    }
}
