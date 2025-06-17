using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Controllers.Models;
using Api.Services.Models;
using Api.Utilities;
#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class MissionTask
    {
        private TaskStatus _status;

        // ReSharper disable once NotNullOrRequiredMemberIsNotInitialized
        public MissionTask() { }

        // ReSharper disable once NotNullOrRequiredMemberIsNotInitialized
        public MissionTask(
            Inspection? inspection,
            Pose robotPose,
            int taskOrder,
            Uri? tagLink,
            string? tagId,
            int? poseId,
            string? taskDescription,
            IsarZoomDescription? zoomDescription = null,
            TaskStatus status = TaskStatus.NotStarted
        )
        {
            TagLink = tagLink;
            TagId = tagId;
            RobotPose = robotPose;
            PoseId = poseId;
            TaskOrder = taskOrder;
            Description = taskDescription;
            Status = status;
            IsarZoomDescription = zoomDescription;
            if (inspection != null)
                Inspection = new Inspection(inspection);
        }

        public MissionTask(CustomTaskQuery taskQuery)
        {
            TagId = taskQuery.TagId;
            Description = taskQuery.Description;
            RobotPose = taskQuery.RobotPose;
            TaskOrder = taskQuery.TaskOrder;
            Status = TaskStatus.NotStarted;
            IsarZoomDescription = taskQuery.IsarZoomDescription;
            Inspection = new Inspection(taskQuery.Inspection);
        }

        public MissionTask(Pose robotPose)
        {
            Description = "Inspection";
            RobotPose = robotPose;
            TaskOrder = 0;
            Status = TaskStatus.NotStarted;
            Inspection = new Inspection();
        }

        // Creates a copy of the provided task
        public MissionTask(MissionTask copy)
        {
            TaskOrder = copy.TaskOrder;
            TagId = copy.TagId;
            IsarTaskId = Guid.NewGuid().ToString();
            Description = copy.Description;
            TagLink = copy.TagLink;
            RobotPose = new Pose(copy.RobotPose);
            PoseId = copy.PoseId;
            Status = TaskStatus.NotStarted;
            IsarZoomDescription = copy.IsarZoomDescription;
            if (copy.Inspection is not null)
            {
                Inspection = new Inspection(copy.Inspection, InspectionStatus.NotStarted);
            }
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [MaxLength(200)]
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string? IsarTaskId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public int TaskOrder { get; set; }

        [MaxLength(200)]
        public string? TagId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public Uri? TagLink { get; set; }

        [Required]
        public Pose RobotPose { get; set; }

        public int? PoseId { get; set; }

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

        public void UpdateWithIsarInfo(IsarTask isarTask)
        {
            if (
                isarTask.TaskType != IsarTaskType.ReturnToHome
                && isarTask.TaskType != IsarTaskType.MoveArm
            )
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

        public static string CalculateHashFromTasks(IList<MissionTask> tasks)
        {
            var genericTasks = new List<MissionTask>();
            foreach (var task in tasks)
            {
                var taskCopy = new MissionTask(task) { Id = "", IsarTaskId = "" };
                if (taskCopy.Inspection is not null)
                    taskCopy.Inspection = new Inspection(taskCopy.Inspection, useEmptyID: true);

                genericTasks.Add(taskCopy);
            }

            string json = JsonSerializer.Serialize(genericTasks);
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            return BitConverter
                .ToString(hash)
                .Replace("-", "", StringComparison.CurrentCulture)
                .ToUpperInvariant();
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
