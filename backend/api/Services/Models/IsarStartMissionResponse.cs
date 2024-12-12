using System.Text.Json.Serialization;


namespace Api.Services.Models
{
    public class IsarStartMissionResponse
    {
        [JsonPropertyName("id")]
        public required string MissionId { get; set; }

        [JsonPropertyName("tasks")]
        public required IList<IsarTaskResponse> Tasks { get; set; }
    }

    public class IsarTaskResponse
    {
        [JsonPropertyName("id")]
        public required string IsarTaskId { get; set; }

        [JsonPropertyName("tag_id")]
        public required string TagId { get; set; }

        [JsonPropertyName("inspection_id")]
        public string? IsarInspectionId { get; set; }

        [JsonPropertyName("type")]
        public required string TaskType { get; set; }


    }
}
