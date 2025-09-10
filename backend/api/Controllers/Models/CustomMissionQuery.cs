using Api.Database.Models;
using Api.Services.Models;

namespace Api.Controllers.Models
{
    public struct CustomInspectionQuery
    {
        public InspectionType InspectionType { get; set; }

        public Position InspectionTarget { get; set; }

        public float? VideoDuration { get; set; }
    }

    public struct CustomTaskQuery
    {
        public int TaskOrder { get; set; }

        public string? TagId { get; set; }

        public string? Description { get; set; }

        public Pose RobotPose { get; set; }

        public IsarZoomDescription? IsarZoomDescription { get; set; }

        public CustomInspectionQuery Inspection { get; set; }
    }

    public struct CustomMissionQuery
    {
        public string RobotId { get; set; }

        public DateTime? CreationTime { get; set; }

        public string InstallationCode { get; set; }

        public TimeSpan? InspectionFrequency { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public string? Comment { get; set; }

        public List<CustomTaskQuery> Tasks { get; set; }

        public IsarZoomDescription? IsarZoomDescription { get; set; }
    }
}
