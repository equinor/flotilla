using System.Text.Json.Serialization;
namespace Api.Services.Models
{
    public class StidTagPositionResponse
    {
        [JsonPropertyName("xCoordinate")]
        public float XCoordinate { get; set; }

        [JsonPropertyName("yCoordinate")]
        public float YCoordinate { get; set; }

        [JsonPropertyName("zCoordinate")]
        public float ZCoordinate { get; set; }
    }
}
