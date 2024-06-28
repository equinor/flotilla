using Api.Database.Models;

namespace Api.Controllers.Models
{
    public struct CreateDefaultLocalizationPose
    {
        public Pose Pose { get; set; }
        public bool IsDockingStation { get; set; }
    }
}
