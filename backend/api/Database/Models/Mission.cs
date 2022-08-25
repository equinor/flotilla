using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable
namespace Api.Database.Models
{
    public class Mission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public string Id { get; set; }

        public string AssetCode { get; set; }

        [Required]
        public virtual Robot Robot { get; set; }

        [MaxLength(128)]
        [Required]
        public string IsarMissionId { get; set; }

        [MaxLength(128)]
        [Required]
        public int EchoMissionId { get; set; }

        [Required]
        public MissionStatus MissionStatus { get; set; }

        [Required]
        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        [Required]
        public virtual IList<IsarTask> Tasks { get; set; }

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
