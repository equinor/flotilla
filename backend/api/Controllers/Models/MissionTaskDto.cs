using Api.Database.Models;

#pragma warning disable CS8618
namespace Api.Controllers.Models
{
    public class MissionTaskDto
    {
        public MissionTaskDto(MissionTask task)
        {
            Id = task.Id;
            IsarTaskId = task.IsarTaskId;
            TaskOrder = task.TaskOrder;
            TagId = task.TagId;
            Description = task.Description;
            EchoTagLink = task.EchoTagLink;
            InspectionTarget = task.InspectionTarget;
            RobotPose = task.RobotPose;
            EchoPoseId = task.EchoPoseId;
            Status = task.Status;
            IsCompleted = task.IsCompleted;
            Error = task.GetError(); // TODO: rethink whether nested errors are needed here
            StartTime = task.StartTime;
            EndTime = task.EndTime;
            Inspections = task.Inspections.Select(i => new InspectionDto(i)).ToList();
        }

        public string Id { get; set; }

        public string? IsarTaskId { get; set; }

        public int TaskOrder { get; set; }

        public string? TagId { get; set; }

        public string? Description { get; set; }

        public Uri? EchoTagLink { get; set; }

        public Position InspectionTarget { get; set; }

        public Pose RobotPose { get; set; }

        public int? EchoPoseId { get; set; }

        public Database.Models.TaskStatus Status { get; set; }

        public bool IsCompleted { get; set; }

        public string? Error { get; set; }

        public DateTimeOffset? StartTime { get; private set; }

        public DateTimeOffset? EndTime { get; private set; }

        public IList<InspectionDto> Inspections { get; set; }
    }
}
