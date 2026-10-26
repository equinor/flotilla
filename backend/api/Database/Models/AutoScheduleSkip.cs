using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Api.Database.Models;

[Index(nameof(MissionDefinitionId), nameof(ScheduledAt), IsUnique = true)]
public class AutoScheduleSkip
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    public required string MissionDefinitionId { get; set; }
    public MissionDefinition MissionDefinition { get; set; } = null!;

    public DateTimeOffset ScheduledAt { get; set; }
}
