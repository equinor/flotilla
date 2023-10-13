#nullable disable
using System.Text.Json.Serialization;
using Api.Database.Models;
namespace Api.Services.Models
{
    public class EnuPosition
    {
        public EnuPosition(float east, float north, float up)
        {
            East = east;
            North = north;
            Up = up;
        }
        [JsonPropertyName("e")]
        public float East { get; set; }
        [JsonPropertyName("n")]
        public float North { get; set; }
        [JsonPropertyName("u")]
        public float Up { get; set; }

        public Position ToPosition()
        {
            return new Position(East, North, Up);
        }
    }
}
