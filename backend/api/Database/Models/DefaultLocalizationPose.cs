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

        [Required]
        public bool DockingEnabled { get; set; } = false;

        public DefaultLocalizationPose()
        {
            Pose = new Pose();
        }

        public DefaultLocalizationPose(Pose pose)
        {
            Pose = pose;
        }

        public DefaultLocalizationPose(Pose pose, bool dockingEnabled)
        {
            Pose = pose;
            DockingEnabled = dockingEnabled;
        }
    }
}
