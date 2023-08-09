using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable CS8618
namespace Api.Database.Models
{

    public class LocalizationPose
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        public Pose Pose { get; set; }

        public LocalizationPose()
        {
            Pose = new Pose();
        }

        public LocalizationPose(Pose pose)
        {
            Pose = pose;
        }
    }

}
