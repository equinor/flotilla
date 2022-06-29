using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable
namespace Api.Database.Models
{
    public class ScheduledMission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        public virtual Robot Robot { get; set; }

        [MaxLength(128)]
        [Required]
        public string IsarMissionId { get; set; }

        [Required]
        public DateTimeOffset StartTime { get; set; }

        [Required]
        public DateTimeOffset EndTime { get; set; }

        [Required]
        public ScheduledMissionStatus Status { get; set; } = ScheduledMissionStatus.Pending;
    }

    public enum ScheduledMissionStatus
    {
        Pending, Ongoing, Successful, Aborted, Paused, Warning
    }
}
