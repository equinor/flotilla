using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Services.Models;
#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class MissionRun : SortableRecord
    {
        private MissionStatus _status;

        private IList<MissionTask> _tasks;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        //[Required] // Return home missions do not have a corresponding MissionDefinition
        public string? MissionId { get; set; }

        [Required]
        public MissionStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                if (IsCompleted && EndTime is null)
                {
                    EndTime = DateTime.UtcNow;
                }
                // If a mission is resumed
                else if (!IsCompleted && EndTime is not null)
                {
                    EndTime = null;
                }

                if (_status is MissionStatus.Ongoing && StartTime is null)
                {
                    StartTime = DateTime.UtcNow;
                }
            }
        }

        [Required]
        [MaxLength(200)]
        public string InstallationCode { get; set; }

        [Required]
        public DateTime DesiredStartTime { get; set; }

        [Required]
        public virtual Robot Robot { get; set; }

        // The tasks are always returned ordered by their order field
        [Required]
        public IList<MissionTask> Tasks
        {
            get =>
                _tasks != null
                    ? _tasks.OrderBy(t => t.TaskOrder).ToList()
                    : new List<MissionTask>();
            set => _tasks = value;
        }

        [Required]
        public MissionRunType MissionRunType { get; set; }

        [MaxLength(200)]
        public string? IsarMissionId { get; set; }

        [MaxLength(450)]
        public string? Description { get; set; }

        [MaxLength(450)]
        public string? StatusReason { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        [Required]
        public InspectionArea InspectionArea { get; set; }

        public bool IsCompleted =>
            _status
                is MissionStatus.Aborted
                    or MissionStatus.Cancelled
                    or MissionStatus.Successful
                    or MissionStatus.PartiallySuccessful
                    or MissionStatus.Failed;

        public DateTime? StartTime { get; private set; }

        public DateTime? EndTime { get; private set; }

        /// <summary>
        ///     The estimated duration of each task in the mission in seconds
        /// </summary>
        public uint? EstimatedTaskDuration { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public bool IsDeprecated { get; set; }

        public void UpdateWithIsarInfo(IsarMission isarMission)
        {
            IsarMissionId = isarMission.IsarMissionId;
            foreach (var isarTask in isarMission.Tasks)
            {
                var task = GetTaskByIsarId(isarTask.IsarTaskId);
                task?.UpdateWithIsarInfo(isarTask);
            }
        }

        public MissionTask? GetTaskByIsarId(string isarTaskId)
        {
            return Tasks.FirstOrDefault(task =>
                task.IsarTaskId != null
                && task.IsarTaskId.Equals(isarTaskId, StringComparison.Ordinal)
            );
        }

        public static MissionStatus GetMissionStatusFromString(string status)
        {
            return status switch
            {
                "successful" => MissionStatus.Successful,
                "not_started" => MissionStatus.Pending,
                "in_progress" => MissionStatus.Ongoing,
                "failed" => MissionStatus.Failed,
                "cancelled" => MissionStatus.Cancelled,
                "paused" => MissionStatus.Paused,
                "partially_successful" => MissionStatus.PartiallySuccessful,
                _ => throw new ArgumentException(
                    $"Failed to parse mission status '{status}' as it's not supported"
                ),
            };
        }

        public void SetEstimatedTaskDuration()
        {
            if (Robot.Model.AverageDurationPerTag is not null)
            {
                EstimatedTaskDuration = (uint)Robot.Model.AverageDurationPerTag;
            }
        }

        public bool IsEmergencyMission()
        {
            return MissionRunType == MissionRunType.Emergency;
        }
    }

    public enum MissionStatus
    {
        Pending,
        Ongoing,
        Paused,
        Aborted,
        Cancelled,
        Failed,
        Successful,
        PartiallySuccessful,
    }

    public enum MissionRunType
    {
        Normal,
        Emergency,
    }
}
