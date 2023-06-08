using Api.Database.Models;

namespace Api.Controllers.Models
{
    public class RescheduleMissionQuery
    {
        public MissionSourceType MissionType { get; set; }
        public string RobotId { get; set; }
        public DateTimeOffset DesiredStartTime { get; set; }
        public TimeSpan? InspectionFrequency { get; set; }
    }
}
