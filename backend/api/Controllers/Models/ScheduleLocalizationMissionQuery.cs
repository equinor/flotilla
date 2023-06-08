using Api.Database.Models;

namespace Api.Controllers.Models
{
    public struct ScheduleLocalizationMissionQuery
    {
        public string RobotId { get; set; }
        public string AreaId { get; set; }
        public Pose LocalizationPose { get; set; }
    }
}
