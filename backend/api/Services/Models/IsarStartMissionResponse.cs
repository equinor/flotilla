using System.Text.Json.Serialization;

#nullable disable
namespace Api.Services.Models
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

        [JsonPropertyName("type")]
        public string TaskType { get; set; }

        [JsonPropertyName("task_action_id")]
        public string TaskActionId { get; set; }

    }
}
