#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class AreaResponse
    {
        public string Id { get; set; }

        public string DeckName { get; set; }

        public string InstallationCode { get; set; }

        public string AssetCode { get; set; }

        public string AreaName { get; set; }

        public MapMetadata MapMetadata { get; set; }

        public Pose DefaultLocalizationPose { get; set; }

        public IList<SafePosition> SafePositions { get; set; }
    }
}
