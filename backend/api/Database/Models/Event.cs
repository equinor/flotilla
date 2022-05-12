using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable
namespace Api.Models
{
    public class Event
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
        public EventStatus Status { get; set; } = EventStatus.Pending;
    }

    public enum EventStatus
    {
        Pending, Started, Completed, Failed
    }
}
