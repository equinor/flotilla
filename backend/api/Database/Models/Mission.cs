using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable
namespace Api.Database.Models
{
    public class Mission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(450)]
        public string Description { get; set; }

        [MaxLength(450)]
        public string StatusReason { get; set; }

        [MaxLength(200)]
        public string AssetCode { get; set; }

        [Required]
        public virtual Robot Robot { get; set; }

        [MaxLength(200)]
        public string IsarMissionId { get; set; }

        [Required]
        [MaxLength(200)]
        public int EchoMissionId { get; set; }

        private MissionStatus _missionStatus;

        [Required]
        public MissionStatus MissionStatus
        {
            get { return _missionStatus; }
            set
            {
                _missionStatus = value;
                if (IsCompleted)
                    EndTime = DateTimeOffset.UtcNow;
            }
        }

        [Required]
        public MissionMap Map { get; set; }

        [Required]
        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        public TimeSpan EstimatedDuration { get; set; }

        [Required]
        public IList<IsarTask> Tasks { get; set; }

        [Required]
        public IList<PlannedTask> PlannedTasks { get; set; }

        public bool IsCompleted =>
            new[]
            {
                MissionStatus.Aborted,
                MissionStatus.Cancelled,
                MissionStatus.Successful,
                MissionStatus.Failed
            }.Contains(_missionStatus);

#nullable enable
        public IsarTask? ReadIsarTaskById(string isarTaskId)
        {
            return Tasks.FirstOrDefault(
                task => task.IsarTaskId.Equals(isarTaskId, StringComparison.Ordinal)
            );
        }

#nullable disable

        public static MissionStatus MissionStatusFromString(string status)
        {
            return status switch
            {
                "completed" => MissionStatus.Successful,
                "not_started" => MissionStatus.Pending,
                "in_progress" => MissionStatus.Ongoing,
                "failed" => MissionStatus.Failed,
                "cancelled" => MissionStatus.Cancelled,
                "paused" => MissionStatus.Paused,
                _
                  => throw new ArgumentException(
                      $"Failed to parse mission status '{status}' as it's not supported"
                  )
            };
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
        Successful
    }
}
