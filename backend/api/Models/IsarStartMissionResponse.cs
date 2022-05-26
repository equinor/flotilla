using System.Text.Json.Serialization;

# nullable disable
namespace Api.Models
{
    public class IsarStartMissionResponse
    {
        [JsonPropertyName("id")]
        public string MissionId { get; set; }

        [JsonPropertyName("tasks")]
        public IList<IsarTaskResponse> Tasks { get; set; }
    }

    public class IsarTaskResponse
    {
        [JsonPropertyName("id")]
        public string IsarTaskId { get; set; }

        [JsonPropertyName("tag_id")]
        public string TagId { get; set; }

        [JsonPropertyName("steps")]
        public IList<IsarStepResponse> Steps { get; set; }
    }

    public class IsarStepResponse
    {
        [JsonPropertyName("id")]
        public string IsarStepId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}
