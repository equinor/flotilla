using System.Text.Json.Serialization;

namespace Api.Services.Models
{
#nullable disable
    public class IsarMediaConfigMessage
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("media_connection_type")]
        public string MediaConnectionType { get; set; }
    }
}
