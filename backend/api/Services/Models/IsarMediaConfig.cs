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
        public List<IsarMediaStream>? Streams { get; set; }
    }

    public class IsarMediaStream
    {
        [JsonPropertyName("camera_id")]
        public required string CameraId { get; set; }

        [JsonPropertyName("url")]
        public required string Url { get; set; }

        [JsonPropertyName("rotation_degrees")]
        public int RotationDegrees { get; set; }
    }
}
