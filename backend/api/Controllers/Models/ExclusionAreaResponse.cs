using System.Text.Json.Serialization;
using Api.Database.Models;

namespace Api.Controllers.Models
{
    public class ExclusionAreaResponse
    {
        public string Id { get; set; }

        public string? ExclusionAreaName { get; set; }

        public string PlantCode { get; set; }

        public string InstallationCode { get; set; }
        public AreaPolygon AreaPolygon { get; set; }

        [JsonConstructor]
#nullable disable
        public ExclusionAreaResponse() { }

#nullable enable

        public ExclusionAreaResponse(ExclusionArea exclusionArea)
        {
            Id = exclusionArea.Id;
            ExclusionAreaName = exclusionArea.Name;
            PlantCode = exclusionArea.Plant.PlantCode;
            InstallationCode = exclusionArea.Installation.InstallationCode;
            AreaPolygon = exclusionArea.AreaPolygon;
        }
    }
}
