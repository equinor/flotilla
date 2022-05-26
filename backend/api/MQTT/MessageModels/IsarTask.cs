using System.Text.Json.Serialization;

namespace Api.Mqtt.MessageModels
{
#nullable disable
    public class IsarTaskMessage : MqttMessage
    {
        [JsonPropertyName("robot_id")]
        public string RobotId { get; set; }

        [JsonPropertyName("mission_id")]
        public string MissionId { get; set; }

        [JsonPropertyName("task_id")]
        public string TaskId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
