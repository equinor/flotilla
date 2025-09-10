namespace Api.Controllers.Models
{
    public struct ScheduledMissionQuery
    {
        public string RobotId { get; set; }
        public string MissionSourceId { get; set; }
        public DateTime? CreationTime { get; set; }
        public string InstallationCode { get; set; }
        public TimeSpan? InspectionFrequency { get; set; }
    }
}
