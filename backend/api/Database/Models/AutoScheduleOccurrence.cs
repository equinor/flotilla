using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public enum AutoScheduleOccurrenceStatus
    {
        Scheduled,
        Skipped,
        Dispatched,
        Cancelled,
        Missed,
        Failed,
    }

    [Index(nameof(MissionDefinitionId), nameof(ScheduledAt), IsUnique = true)]
    public class AutoScheduleOccurrence
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public required string MissionDefinitionId { get; set; }

        [Required]
        [ForeignKey(nameof(MissionDefinitionId))]
        [DeleteBehavior(DeleteBehavior.Cascade)]
        public virtual MissionDefinition MissionDefinition { get; set; }

        public DateTimeOffset ScheduledAt { get; set; }
        public AutoScheduleOccurrenceStatus Status { get; set; }
        public string? JobId { get; set; }
        public string? SchedulerInstance { get; set; }
    }
}
