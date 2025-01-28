using System.Text.Json.Serialization;
using Api.Database.Models;

namespace Api.Controllers.Models
{
    public class InspectionGroupResponse
    {
        public string Id { get; set; }

        public string InspectionGroupName { get; set; }

        public string InstallationCode { get; set; }

        public Pose? DefaultLocalizationPose { get; set; }

        [JsonConstructor]
#nullable disable
        public InspectionGroupResponse() { }

#nullable enable

        public InspectionGroupResponse(InspectionGroup inspectionGroup)
        {
            Id = inspectionGroup.Id;
            InspectionGroupName = inspectionGroup.Name;
            InstallationCode = inspectionGroup.Installation.InstallationCode;
            DefaultLocalizationPose = inspectionGroup.DefaultLocalizationPose?.Pose;
        }
    }
}
