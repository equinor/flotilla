namespace Api.Services.Events
{
    public class MissionRunCreatedEventArgs : EventArgs
    {

        public MissionRunCreatedEventArgs(string missionRunId)
        {
            MissionRunId = missionRunId;
        }
        public string MissionRunId { get; set; }
    }

    public class RobotAvailableEventArgs : EventArgs
    {

        public RobotAvailableEventArgs(string robotId)
        {
            RobotId = robotId;
        }
        public string RobotId { get; set; }
    }
}
