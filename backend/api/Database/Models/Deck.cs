using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class Deck
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        public virtual Plant Plant { get; set; }

        public virtual Installation Installation { get; set; }

        public string? DefaultLocalizationAreaId { get; set; }

        [ForeignKey("DefaultLocalizationAreaId")]
        public virtual Area? DefaultLocalizationArea { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }
    }
}
