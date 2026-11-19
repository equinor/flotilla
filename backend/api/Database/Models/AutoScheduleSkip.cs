using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Api.Database.Models;

[Index("MissionDefinitionId", nameof(ScheduledAt), IsUnique = true)]
public class AutoScheduleSkip
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    public required MissionDefinition MissionDefinition { get; set; }

    public DateTimeOffset ScheduledAt { get; set; }
}
