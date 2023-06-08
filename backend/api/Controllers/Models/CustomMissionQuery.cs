using Api.Database.Models;

namespace Api.Controllers.Models
{
    public struct CustomInspectionQuery
    {
        public InspectionType InspectionType { get; set; }

        public float? VideoDuration { get; set; }

        public string? AnalysisTypes { get; set; }
    }

    public struct CustomTaskQuery
    {
        public int TaskOrder { get; set; }

        public Position InspectionTarget { get; set; }

        public string? TagId { get; set; }

        public string? Description { get; set; }

        public Pose RobotPose { get; set; }

        public List<CustomInspectionQuery> Inspections { get; set; }
    }

    public struct CustomMissionQuery
    {
        public string RobotId { get; set; }

        public DateTimeOffset? DesiredStartTime { get; set; }

        public string AssetCode { get; set; }

        public TimeSpan? InspectionFrequency { get; set; }

        public string AreaName { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public string? Comment { get; set; }

        public List<CustomTaskQuery> Tasks { get; set; }
    }
}
