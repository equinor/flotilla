using System.Text.Json.Serialization;

namespace Api.Services.Models
{
    public class IsarMediaConfigMessage
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("media_connection_type")]
        public required string MediaConnectionType { get; set; }

        [JsonPropertyName("streams")]
        public List<MediaStream>? Streams { get; set; }
    }
}
