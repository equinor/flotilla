using System.Text.Json.Serialization;
namespace Api.Services.Models
{
    public struct IsarMediaConfig
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("authToken")]
        public string? AuthToken { get; set; }

        [JsonPropertyName("robotId")]
        public string? RobotId { get; set; }

        [JsonPropertyName("mediaConnectionType")]
        public MediaConnectionType MediaConnectionType { get; set; }
    }

    public enum MediaConnectionType { LiveKit };
}
