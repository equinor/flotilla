using Api.Database.Models;

namespace Api.Controllers.Models
{
    public struct AddLocalizationPoseToDeckQuery
    {
        public string DeckId { get; set; }
        public Pose LocalizationPose { get; set; }
    }
}
