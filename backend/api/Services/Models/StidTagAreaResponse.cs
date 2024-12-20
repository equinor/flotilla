using System.Text.Json.Serialization;

namespace Api.Services.Models
{
    public class StidTagAreaResponse
    {
        [JsonPropertyName("locationCode")]
        public string? LocationCode { get; set; }
    }
}
