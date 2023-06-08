using System.ComponentModel.DataAnnotations;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class MissionDefinition
    {
        [Key]
        [Required]
        [MaxLength(200)]
        public string Id { get; set; }

        [Required]
        public Source Source { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public TimeSpan? InspectionFrequency { get; set; }

        public MissionRun? LastRun { get; set; }

        [Required]
        public string AssetCode { get; set; }

        public Area? Area { get; set; }
    }
}
