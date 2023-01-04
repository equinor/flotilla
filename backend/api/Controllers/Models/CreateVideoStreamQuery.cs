using System.Text.Json.Serialization;

namespace Api.Controllers.Models
{
    public struct CreateVideoStreamQuery
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}
