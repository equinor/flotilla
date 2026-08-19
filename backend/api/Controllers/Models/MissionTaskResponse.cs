using System.Text.Json.Serialization;
using Api.Database.Models;
using Api.Services.Models;
using TaskStatus = Api.Database.Models.TaskStatus;

namespace Api.Controllers.Models
{
    public class MissionTaskResponse
    {
        public string Id { get; set; }

        public int TaskOrder { get; set; }

        public string? TagId { get; set; }

        public string? Description { get; set; }

        public TaskStatus Status { get; set; }

        public bool IsCompleted { get; set; }

        public IList<AnalysisType> AnalysisTypes { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public string? ErrorDescription { get; set; }

        public Pose RobotPose { get; set; }

        public IsarZoomDescription? IsarZoomDescription { get; set; }

        [JsonConstructor]
#nullable disable
        public MissionTaskResponse() { }

#nullable enable

        public MissionTaskResponse(MissionTask task)
        {
            Id = task.Id;
            TaskOrder = task.TaskOrder;
            TagId = task.TagId;
            Description = task.Description;
            Status = task.Status;
            IsCompleted = task.IsCompleted;
            AnalysisTypes = task.AnalysisTypes;
            StartTime = task.StartTime;
            EndTime = task.EndTime;
            ErrorDescription = task.ErrorDescription;
            RobotPose = task.RobotPose;
            IsarZoomDescription = task.IsarZoomDescription;
        }
    }
}
