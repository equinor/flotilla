using System.Text.Json.Serialization;
# nullable disable

namespace Api.Controllers.Models
{
    public class IsarControlMissionResponse
    {
        [JsonPropertyName("mission_id")]
        public string IsarMissionId { get; set; }

        [JsonPropertyName("mission_status")]
        public string IsarMissionStatus { get; set; }

        [JsonPropertyName("task_id")]
        public string IsarTaskId { get; set; }

        [JsonPropertyName("task_status")]
        public string IsarTaskStatus { get; set; }

        [JsonPropertyName("step_id")]
        public string IsarStepId { get; set; }

        [JsonPropertyName("step_status")]
        public string IsarStepStatus { get; set; }
    }
}
