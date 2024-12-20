using System.Text.Json.Serialization;
using Api.Database.Models;

namespace Api.Controllers.Models
{
    public class InspectionAreaResponse
    {
        public string Id { get; set; }

        public string InspectionAreaName { get; set; }

        public string PlantCode { get; set; }

        public string InstallationCode { get; set; }

        public Pose? DefaultLocalizationPose { get; set; }

        [JsonConstructor]
#nullable disable
        public InspectionAreaResponse() { }
#nullable enable

        public InspectionAreaResponse(InspectionArea inspectionArea)
        {
            Id = inspectionArea.Id;
            InspectionAreaName = inspectionArea.Name;
            PlantCode = inspectionArea.Plant.PlantCode;
            InstallationCode = inspectionArea.Installation.InstallationCode;
            DefaultLocalizationPose = inspectionArea.DefaultLocalizationPose?.Pose;
        }
    }
}
