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
}
