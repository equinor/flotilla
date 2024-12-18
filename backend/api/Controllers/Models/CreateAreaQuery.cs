using Api.Database.Models;

namespace Api.Controllers.Models
{
    public struct CreateAreaQuery
    {
        public string InstallationCode { get; set; }
        public string PlantCode { get; set; }
        public string InspectionAreaName { get; set; }
        public string AreaName { get; set; }

        public Pose? DefaultLocalizationPose { get; set; }
    }
}
