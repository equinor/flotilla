#nullable disable
using Api.Database.Models;
using System.Text.Json.Serialization;
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
        public EchoVector Position { get; set; }
        [JsonPropertyName("lookDirectionNormalized")]
        public EchoVector LookDirectionNormalized { get; set; }
        [JsonPropertyName("tiltDegClockwise")]
        public float TiltDegreesClockwise { get; set; }
        [JsonPropertyName("isDefault")]
        public bool IsDefault { get; set; }
    }
    public class EchoVector
    {
        [JsonPropertyName("e")]
        public float East { get; set; }
        [JsonPropertyName("n")]
        public float North { get; set; }
        [JsonPropertyName("u")]
        public float Up { get; set; }
    }
    public class EchoPoseRequestBody
    {
        public string InstallationCode { get; set; }
        public List<string> Tags { get; set; }

    }
}
