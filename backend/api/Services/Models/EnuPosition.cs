#nullable disable
using System.Text.Json.Serialization;
using Api.Database.Models;

namespace Api.Services.Models
{
    public class EnuPosition(float east, float north, float up)
    {
        [JsonPropertyName("e")]
        public float East { get; } = east;

        [JsonPropertyName("n")]
        public float North { get; } = north;

        [JsonPropertyName("u")]
        public float Up { get; } = up;

        public Position ToPosition()
        {
            return new Position(East, North, Up);
        }
    }
}
