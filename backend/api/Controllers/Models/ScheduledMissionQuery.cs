namespace Api.Controllers.Models
{
    public struct ScheduledMissionQuery
    {
        public string RobotId { get; set; }
        public string EchoMissionId { get; set; }
        public DateTimeOffset? StartTime { get; set; }
    }
}
