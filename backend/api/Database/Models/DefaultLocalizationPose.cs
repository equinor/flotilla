using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class DefaultLocalizationPose
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        public Pose Pose { get; set; }

        public DefaultLocalizationPose()
        {
            Pose = new Pose();
        }

        public DefaultLocalizationPose(Pose pose)
        {
            Pose = pose;
        }
    }
}
