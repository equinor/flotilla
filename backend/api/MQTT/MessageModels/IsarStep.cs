using System.Text.Json.Serialization;

#nullable disable
namespace Api.Mqtt.MessageModels
{
    public class IsarStepMessage : MqttMessage
    {
        [JsonPropertyName("robot_name")]
        public string RobotName { get; set; }

        [JsonPropertyName("isar_id")]
        public string IsarId { get; set; }

        [JsonPropertyName("mission_id")]
        public string MissionId { get; set; }

        [JsonPropertyName("task_id")]
        public string TaskId { get; set; }

        [JsonPropertyName("step_id")]
        public string StepId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
