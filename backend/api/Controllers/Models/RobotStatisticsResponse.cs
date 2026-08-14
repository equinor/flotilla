namespace Api.Controllers.Models
{
    public class RobotStatisticsResponse
    {
        public string RobotId { get; set; } = string.Empty;

        public DateTime FromTime { get; set; }

        public DateTime ToTime { get; set; }

        public MissionStatisticsResponse Missions { get; set; } = new();

        public TaskStatisticsResponse Tasks { get; set; } = new();

        public IList<WeeklyMissionCountResponse> MissionsPerWeek { get; set; } = [];
    }
}
