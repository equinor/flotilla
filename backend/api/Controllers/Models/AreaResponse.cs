using System.Text.Json.Serialization;
using Api.Database.Models;
namespace Api.Controllers.Models
{
    public class AreaResponse
    {
        public string Id { get; set; } = string.Empty;

        public string DeckName { get; set; }

        public string PlantCode { get; set; }

        public string PlantName { get; set; }

        public string InstallationCode { get; set; }

        public string AreaName { get; set; }

        public MapMetadata MapMetadata { get; set; }

        public Pose? DefaultLocalizationPose { get; set; }

        public IList<SafePosition> SafePositions { get; set; }

        [JsonConstructor]
#nullable disable
        public AreaResponse() { }
#nullable enable

        public AreaResponse(Area area)
        {
            Id = area.Id;
            DeckName = area.Deck!.Name;
            PlantCode = area.Plant.PlantCode;
            PlantName = area.Plant.Name;
            InstallationCode = area.Installation.InstallationCode;
            AreaName = area.Name;
            MapMetadata = area.MapMetadata;
            DefaultLocalizationPose = area.DefaultLocalizationPose?.Pose;
            SafePositions = area.SafePositions;
        }
    }
}
