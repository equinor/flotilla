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
            IList<Inspection> inspections,
            Pose robotPose,
            int taskOrder,
            Uri? tagLink,
            string? tagId,
            int? poseId,
            TaskStatus status = TaskStatus.NotStarted,
            MissionTaskType type = MissionTaskType.Inspection)
        {
            Inspections = inspections
                .Select(inspection => new Inspection(inspection))
                .ToList();
            TagLink = tagLink;
            TagId = tagId;
            RobotPose = robotPose;
            PoseId = poseId;
            TaskOrder = taskOrder;
            Status = status;
            Type = type;
        }

        public MissionTask(CustomTaskQuery taskQuery)
        {
            Inspections = taskQuery.Inspections
                .Select(inspection => new Inspection(inspection))
                .ToList();
            TagId = taskQuery.TagId;
            Description = taskQuery.Description;
            RobotPose = taskQuery.RobotPose;
            TaskOrder = taskQuery.TaskOrder;
            Status = TaskStatus.NotStarted;
            Type = MissionTaskType.Inspection;
        }

        public MissionTask(Pose robotPose, MissionTaskType type)
        {
            switch (type)
            {
                case MissionTaskType.Localization:
                    Type = type;
                    Description = "Localization";
                    RobotPose = robotPose;
                    TaskOrder = 0;
                    Status = TaskStatus.NotStarted;
                    Inspections = new List<Inspection>();
                    break;
                case MissionTaskType.ReturnHome:
                    Type = type;
                    Description = "Return to home";
                    RobotPose = robotPose;
                    TaskOrder = 0;
                    Status = TaskStatus.NotStarted;
                    Inspections = new List<Inspection>();
                    break;
                case MissionTaskType.Inspection:
                    Type = type;
                    Description = "Inspection";
                    RobotPose = robotPose;
                    TaskOrder = 0;
                    Status = TaskStatus.NotStarted;
                    Inspections = new List<Inspection>();
                    break;
                default:
                    throw new MissionTaskNotFoundException("MissionTaskType should be Localization, ReturnHome or Inspection");
            }
        }

        // Creates a copy of the provided task
        public MissionTask(MissionTask copy, TaskStatus? status = null)
        {
            TaskOrder = copy.TaskOrder;
            TagId = copy.TagId;
            IsarTaskId = Guid.NewGuid().ToString();
            Description = copy.Description;
            TagLink = copy.TagLink;
            RobotPose = new Pose(copy.RobotPose);
            PoseId = copy.PoseId;
            Status = status ?? copy.Status;
            Inspections = copy.Inspections.Select(i => new Inspection(i, InspectionStatus.NotStarted)).ToList();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [MaxLength(200)]
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string? IsarTaskId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public int TaskOrder { get; set; }

        [Required]
        public MissionTaskType Type { get; set; }

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
                if (IsCompleted && EndTime is null) { EndTime = DateTime.UtcNow; }

                if (_status is TaskStatus.InProgress && StartTime is null) { StartTime = DateTime.UtcNow; }
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

        public IList<Inspection> Inspections { get; set; }

        public void UpdateWithIsarInfo(IsarTask isarTask)
        {
            UpdateStatus(isarTask.TaskStatus);
            foreach (var inspection in Inspections)
            {
                var correspondingStep = isarTask.Steps.Single(
                    step => step.IsarStepId.Equals(inspection.IsarStepId, StringComparison.Ordinal)
                );
                inspection.UpdateWithIsarInfo(correspondingStep);
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
                _ => throw new ArgumentException($"ISAR Task status '{isarStatus}' not supported")
            };
        }

        public Inspection? GetInspectionByIsarStepId(string isarStepId)
        {
            return Inspections.FirstOrDefault(
                inspection =>
                    inspection.IsarStepId != null
                    && inspection.IsarStepId.Equals(isarStepId, StringComparison.Ordinal)
            );
        }

        public static string ConvertMissionTaskTypeToIsarTaskType(MissionTaskType missionTaskType)
        {
            return missionTaskType switch
            {
                MissionTaskType.ReturnHome => "return_to_home",
                MissionTaskType.Localization => "localization",
                MissionTaskType.Inspection => "inspection",
                _ => throw new ArgumentException($"ISAR Mission task type '{missionTaskType}' not supported"),
            };
            ;
        }

        public static string CalculateHashFromTasks(IList<MissionTask> tasks)
        {
            var genericTasks = new List<MissionTask>();
            foreach (var task in tasks)
            {
                var taskCopy = new MissionTask(task)
                {
                    Id = "",
                    IsarTaskId = ""
                };
                taskCopy.Inspections = taskCopy.Inspections.Select(i => new Inspection(i, useEmptyIDs: true)).ToList();
                genericTasks.Add(taskCopy);
            }

            string json = JsonSerializer.Serialize(genericTasks);
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            return BitConverter.ToString(hash).Replace("-", "", StringComparison.CurrentCulture).ToUpperInvariant();
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
        Paused
    }

    public enum MissionTaskType
    {
        Inspection,
        Localization,
        ReturnHome
    }
}
