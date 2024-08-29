using System.Text.Json.Serialization;

namespace Api.Controllers.Models
{
    public struct CreateDocumentationQuery
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}
