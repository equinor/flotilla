using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class MissionDefinition : SortableRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        public Source Source { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        public string AssetCode { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public TimeSpan? InspectionFrequency { get; set; }

        public virtual MissionRun? LastRun { get; set; }

        public Area? Area { get; set; }

        public bool Deprecated { get; set; }
    }
}
