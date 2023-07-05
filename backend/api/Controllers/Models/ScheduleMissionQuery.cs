namespace Api.Controllers.Models
{
    public class ScheduleMissionQuery
    {
        public string MissionDefinitionId { get; set; }
        public string RobotId { get; set; }
        public DateTimeOffset DesiredStartTime { get; set; }
    }
}
