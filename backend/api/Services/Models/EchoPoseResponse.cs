#nullable disable
using System.Text.Json.Serialization;
using Api.Database.Models;
namespace Api.Services.Models
{
    public class EchoPoseResponse
    {
        [JsonPropertyName("poseId")]
        public int PoseId { get; set; }
        [JsonPropertyName("installationCode")]
        public string InstallationCode { get; set; }
        [JsonPropertyName("tag")]
        public string Tag { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("position")]
        public EnuPosition Position { get; set; }

        [JsonPropertyName("robotBodyDirectionDegrees")]
        public float RobotBodyDirectionDegrees { get; set; }
        [JsonPropertyName("isDefault")]
        public bool IsDefault { get; set; }
    }
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
    public class EchoPoseRequestBody
    {
        public string InstallationCode { get; set; }
        public List<string> Tags { get; set; }
    }
}
