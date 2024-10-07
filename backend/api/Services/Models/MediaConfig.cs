using System.Text.Json.Serialization;
namespace Api.Services.Models
{
    public struct MediaConfig
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("robotId")]
        public string? RobotId { get; set; }

        [JsonPropertyName("mediaConnectionType")]
        public MediaConnectionType MediaConnectionType { get; set; }
    }

    public enum MediaConnectionType { LiveKit };
}
