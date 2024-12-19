using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class Area
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        public virtual InspectionArea InspectionArea { get; set; }

        [Required]
        public virtual Plant Plant { get; set; }

        [Required]
        public virtual Installation Installation { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        public MapMetadata MapMetadata { get; set; }

        public DefaultLocalizationPose? DefaultLocalizationPose { get; set; }

    }
}
