namespace Api.Controllers.Models
{
    public struct ScheduledMissionQuery
    {
        public string RobotId { get; set; }
        public string MissionId { get; set; }
        public DateTime? DesiredStartTime { get; set; }
        public string InstallationCode { get; set; }
        public string? AreaName { get; set; }
        public TimeSpan? InspectionFrequency { get; set; }
    }
}
