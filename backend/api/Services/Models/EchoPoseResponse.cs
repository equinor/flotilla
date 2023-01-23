#nullable disable
using Api.Database.Models;
using System.Text.Json.Serialization;
namespace Api.Services.Models
{
    public class EchoPoseResponse
    {
        [JsonPropertyName("poseID")]
        public int PoseId { get; set; }
        [JsonPropertyName("installationCode")]
        public string InstallationCode { get; set; }
        [JsonPropertyName("tag")]
        public string Tag { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("position")]
        public Position Position { get; set; }
        [JsonPropertyName("lookDirectionNormalized")]
        public Position LookDirectionNormalized { get; set; }
        [JsonPropertyName("tiltDegClockwise")]
        public float TiltDegreesClockwize { get; set; }
        [JsonPropertyName("isDefault")]
        public float isDefault { get; set; }
    }
    public class EchoPoseBody
    {
        public string installationCode { get; set; }
        public List<string> tags { get; set; }

    }
}
