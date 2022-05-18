using System.Text.Json.Serialization;

#nullable disable
namespace Api.Mqtt.MessageModels
{
    public class IsarStep : MqttMessage
    {
        [JsonPropertyName("robot_id")]
        public string RobotId { get; set; }

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
