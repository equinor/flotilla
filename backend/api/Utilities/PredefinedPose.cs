using System.ComponentModel.DataAnnotations;
using Api.Database.Models;
namespace Api.Utilities
{
    public class PredefinedPose
    {
        [Required]
        public string Tag { get; set; }
        public Pose Pose { get; set; }
        public PredefinedPose(string tag, Pose pose)
        {
            Tag = tag;
            Pose = pose;
        }
    }
}
