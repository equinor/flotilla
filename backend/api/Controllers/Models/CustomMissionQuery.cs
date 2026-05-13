using Api.Database.Models;
using Api.Services.Models;

namespace Api.Controllers.Models
{
    public struct TaskQuery
    {
        public string? TagId { get; set; }

        public string? Description { get; set; }

        public Pose RobotPose { get; set; }

        public Position TargetPosition { get; set; }

        public IsarZoomDescription? ZoomDescription { get; set; }

        public SensorType SensorType { get; set; }

        public IList<AnalysisType> AnalysisTypes { get; set; }

        public float? VideoDuration { get; set; }
    }

    public struct MissionQuery
    {
        public string RobotId { get; set; }

        public string InstallationCode { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public List<TaskQuery> Tasks { get; set; }
    }
}
