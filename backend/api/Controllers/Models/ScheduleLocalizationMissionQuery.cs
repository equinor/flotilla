using Api.Database.Models;

namespace Api.Controllers.Models
{
    public struct ScheduleLocalizationMissionQuery
    {
        public string RobotId { get; set; }
        public Pose LocalizationPose { get; set; }
        public string DeckId { get; set; }
    }
}
