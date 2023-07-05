using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Services.Models;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class MissionRun : SortableRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        //[Required] // See "Drive to Safe Position" mission in RobotController.cs
        public string? MissionId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        public MissionStatus Status
        {
            get { return _status; }
            set
            {
                _status = value;
                if (IsCompleted && EndTime is null)
                    EndTime = DateTimeOffset.UtcNow;

                if (_status is MissionStatus.Ongoing && StartTime is null)
                    StartTime = DateTimeOffset.UtcNow;
            }
        }

        [Required]
        [MaxLength(200)]
        public string AssetCode { get; set; }

        [Required]
        public DateTimeOffset DesiredStartTime { get; set; }

        [Required]
        public virtual Robot Robot { get; set; }

        // The tasks are always returned ordered by their order field
        [Required]
        public IList<MissionTask> Tasks
        {
            get { return _tasks.OrderBy(t => t.TaskOrder).ToList(); }
            set { _tasks = value; }
        }

        [MaxLength(200)]
        public string? IsarMissionId { get; set; }

        [MaxLength(450)]
        public string? Description { get; set; }

        [MaxLength(450)]
        public string? StatusReason { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public Area? Area { get; set; }

        private MissionStatus _status;

        public bool IsCompleted =>
            _status
                is MissionStatus.Aborted
                    or MissionStatus.Cancelled
                    or MissionStatus.Successful
                    or MissionStatus.PartiallySuccessful
                    or MissionStatus.Failed;

        public MapMetadata? MapMetadata { get; set; }

        public DateTimeOffset? StartTime { get; private set; }

        public DateTimeOffset? EndTime { get; private set; }

        /// <summary>
        /// The estimated duration of the mission in seconds
        /// </summary>
        public uint? EstimatedDuration { get; set; }

        private IList<MissionTask> _tasks;

        public void UpdateWithIsarInfo(IsarMission isarMission)
        {
            IsarMissionId = isarMission.IsarMissionId;
            foreach (var isarTask in isarMission.Tasks)
            {
                var task = GetTaskByIsarId(isarTask.IsarTaskId);
                task?.UpdateWithIsarInfo(isarTask);
            }
        }

#nullable enable
        public MissionTask? GetTaskByIsarId(string isarTaskId)
        {
            return Tasks.FirstOrDefault(
                task =>
                    task.IsarTaskId != null
                    && task.IsarTaskId.Equals(isarTaskId, StringComparison.Ordinal)
            );
        }

#nullable disable

        public static MissionStatus MissionStatusFromString(string status)
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
                _
                  => throw new ArgumentException(
                      $"Failed to parse mission status '{status}' as it's not supported"
                  )
            };
        }

        public void CalculateEstimatedDuration()
        {
            if (Robot.Model.AverageDurationPerTag is not null)
            {
                float totalInspectionDuration = Tasks.Sum(
                    task => task.Inspections.Sum(inspection => inspection.VideoDuration ?? 0)
                );
                EstimatedDuration = (uint)(
                    (Robot.Model.AverageDurationPerTag * Tasks.Count) + totalInspectionDuration
                );
            }
            else
            {
                const double RobotVelocity = 1.5 * 1000 / 60; // km/t => m/min
                const double EfficiencyFactor = 0.20;
                const double InspectionTime = 2; // min/tag
                const int AssumedXyMetersFromFirst = 20;

                double distance = 0;
                int numberOfTags = 0;
                var prevPosition = new Position(
                    Tasks.First().RobotPose.Position.X + AssumedXyMetersFromFirst,
                    Tasks.First().RobotPose.Position.Y + AssumedXyMetersFromFirst,
                    Tasks.First().RobotPose.Position.Z
                );
                foreach (var task in Tasks)
                {
                    numberOfTags += task.Inspections.Count;
                    var currentPosition = task.RobotPose.Position;
                    distance +=
                        Math.Abs(currentPosition.X - prevPosition.X)
                        + Math.Abs(currentPosition.Y - prevPosition.Y);
                    prevPosition = currentPosition;
                }
                int estimate = (int)(
                    (distance / (RobotVelocity * EfficiencyFactor))
                    + (numberOfTags * InspectionTime)
                );
                EstimatedDuration = (uint)estimate * 60;
            }
        }

        public void SetToFailed()
        {
            Status = MissionStatus.Failed;
            StatusReason = "Lost connection to ISAR during mission";
            foreach (var task in Tasks.Where(task => !task.IsCompleted))
            {
                task.Status = TaskStatus.Failed;
                foreach (
                    var inspection in task.Inspections.Where(inspection => !inspection.IsCompleted)
                )
                {
                    inspection.Status = InspectionStatus.Failed;
                }
            }
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
        PartiallySuccessful
    }
}
