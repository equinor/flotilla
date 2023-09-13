namespace Api.Controllers.Models
{
    public class ScheduleMissionQuery
    {
        public string MissionDefinitionId { get; set; } = string.Empty;
        public string RobotId { get; set; } = string.Empty;

        public string DeckId { get; set; } = string.Empty;
        public DateTimeOffset DesiredStartTime { get; set; }
    }
}
