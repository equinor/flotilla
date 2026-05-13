using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Services.Models;
#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class MissionTask
    {
        private TaskStatus _status;

        // ReSharper disable once NotNullOrRequiredMemberIsNotInitialized
        public MissionTask() { }

        // Creates a copy of the provided task
        public MissionTask(MissionTask copy)
        {
            TaskOrder = copy.TaskOrder;
            TagId = copy.TagId;
            Description = copy.Description;
            RobotPose = new Pose(copy.RobotPose);
            Status = TaskStatus.NotStarted;
            IsarZoomDescription = copy.IsarZoomDescription;
            AnalysisTypes = copy.AnalysisTypes;
            if (copy.Inspection is not null)
            {
                Inspection = new Inspection(copy.Inspection);
            }
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        public int TaskOrder { get; set; }

        [MaxLength(200)]
        public string? TagId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public Pose RobotPose { get; set; }

        public IList<AnalysisType>? AnalysisTypes { get; set; } = [];

        [Required]
        public TaskStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                if (IsCompleted && EndTime is null)
                {
                    EndTime = DateTime.UtcNow;
                }

                if (_status is TaskStatus.InProgress && StartTime is null)
                {
                    StartTime = DateTime.UtcNow;
                }
            }
        }

        public bool IsCompleted =>
            _status
                is TaskStatus.Cancelled
                    or TaskStatus.Successful
                    or TaskStatus.Failed
                    or TaskStatus.PartiallySuccessful;

        public DateTime? StartTime { get; private set; }

        public DateTime? EndTime { get; private set; }

        public IsarZoomDescription? IsarZoomDescription { get; set; }

        public Inspection? Inspection { get; set; }

        public string? ErrorDescription { get; set; }

        public void UpdateWithIsarInfo(IsarTask isarTask)
        {
            if (isarTask.TaskType != IsarTaskType.ReturnToHome)
            {
                Inspection?.UpdateWithIsarInfo(isarTask);
            }
        }

        public void UpdateStatus(IsarTaskStatus isarStatus)
        {
            Status = isarStatus switch
            {
                IsarTaskStatus.NotStarted => TaskStatus.NotStarted,
                IsarTaskStatus.InProgress => TaskStatus.InProgress,
                IsarTaskStatus.Successful => TaskStatus.Successful,
                IsarTaskStatus.PartiallySuccessful => TaskStatus.PartiallySuccessful,
                IsarTaskStatus.Cancelled => TaskStatus.Cancelled,
                IsarTaskStatus.Paused => TaskStatus.Paused,
                IsarTaskStatus.Failed => TaskStatus.Failed,
                _ => throw new ArgumentException($"ISAR Task status '{isarStatus}' not supported"),
            };
        }

        public static string GetIsarInspectionTaskType()
        {
            return "inspection";
        }

        public TaskDefinition ToMissionTaskDefinition()
        {
            return new TaskDefinition
            {
                Index = this.TaskOrder,
                TagId = this.TagId,
                Description = this.Description,
                RobotPose = this.RobotPose,
                ZoomDescription = this.IsarZoomDescription,
                AnalysisTypes = this.AnalysisTypes ?? [],
            };
        }
    }

    public enum TaskStatus
    {
        Successful,
        PartiallySuccessful,
        NotStarted,
        InProgress,
        Failed,
        Cancelled,
        Paused,
    }
}
