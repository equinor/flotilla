namespace Api.Controllers.Models
{
    public struct ScheduledMissionQuery
    {
        public string RobotId { get; set; }
        public string IsarMissionId { get; set; }
        public DateTimeOffset? StartTime { get; set; }
    }
}
