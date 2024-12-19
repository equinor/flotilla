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

        //[Required] // See "Drive to Docking Station" mission in RobotController.cs
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
            get => _tasks.OrderBy(t => t.TaskOrder).ToList();
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

        public InspectionArea? InspectionArea { get; set; }

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
        ///     The estimated duration of the mission in seconds
        /// </summary>
        public uint? EstimatedDuration { get; set; }

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
            return Tasks.FirstOrDefault(
                task =>
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
                    task => task.Inspection?.VideoDuration ?? 0
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
                var prevPosition = new Position(
                    Tasks.First().RobotPose.Position.X + AssumedXyMetersFromFirst,
                    Tasks.First().RobotPose.Position.Y + AssumedXyMetersFromFirst,
                    Tasks.First().RobotPose.Position.Z
                );
                foreach (var task in Tasks)
                {
                    var currentPosition = task.RobotPose.Position;
                    distance +=
                        Math.Abs(currentPosition.X - prevPosition.X)
                        + Math.Abs(currentPosition.Y - prevPosition.Y);
                    prevPosition = currentPosition;
                }
                int estimate = (int)(
                    (distance / (RobotVelocity * EfficiencyFactor))
                    + InspectionTime
                );
                EstimatedDuration = (uint)estimate * 60;
            }
        }

        public bool IsReturnHomeMission() { return MissionRunType == MissionRunType.ReturnHome; }

        public bool IsEmergencyMission() { return MissionRunType == MissionRunType.Emergency; }
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

    public enum MissionRunType
    {
        Normal,
        ReturnHome,
        Emergency
    }
}
