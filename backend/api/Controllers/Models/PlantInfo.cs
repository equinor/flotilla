#nullable disable
using Api.Database.Models;

namespace Api.Controllers.Models
{
    public class PlantInfo
    {
        public string PlantCode { get; set; }
        public string ProjectDescription { get; set; }

        public PlantInfo() { }

        public PlantInfo(string plantCode, string projectDescription)
        {
            PlantCode = plantCode;
            ProjectDescription = projectDescription;
        }

        public PlantInfo(Installation installation)
        {
            PlantCode = installation.InstallationCode;
            ProjectDescription = installation.Name;
        }
    }
}
