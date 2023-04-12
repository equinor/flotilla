using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable
namespace Api.Database.Models
{
    public class AssetDeck
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string AssetCode { get; set; }

        [Required]
        [MaxLength(200)]
        public string DeckName { get; set; }

        [Required]
        public Pose DefaultLocalizationPose { get; set; }
    }
}
