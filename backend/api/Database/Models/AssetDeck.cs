using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable CS8618
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

        public IList<SafePosition> SafePositions { get; set; }

        public AssetDeck()
        {
            AssetCode = "defaultAsset";
            DeckName = "defaultDeckName";
            DefaultLocalizationPose = new Pose();
        }
    }

    public class SafePosition
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        public Pose Pose { get; set; }

        public SafePosition()
        {
            Pose = new Pose();
        }

        public SafePosition(Pose pose)
        {
            Pose = pose;
        }
    }
}
