using Api.Database.Models;

namespace Api.Controllers.Models
{
    public struct CreateAssetDeckQuery
    {
        public string AssetCode { get; set; }

        public string DeckName { get; set; }

        public Pose DefaultLocalizationPose { get; set; }
    }
}
