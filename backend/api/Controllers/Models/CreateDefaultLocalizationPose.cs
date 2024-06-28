using Api.Database.Models;

namespace Api.Controllers.Models
{
    public struct CreateDefaultLocalizationPose
    {
        public CreateDefaultLocalizationPose()
        {
            Pose = new Pose();
        }

        public Pose Pose { get; set; }
        public bool IsDockingStation { get; set; } = false;
    }
}
