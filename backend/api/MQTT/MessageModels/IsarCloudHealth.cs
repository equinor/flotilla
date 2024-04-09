using System.Text.Json.Serialization;

namespace Api.Mqtt.MessageModels
{
#nullable disable
    public class IsarCloudHealthMessage : MqttMessage
    {
        [JsonPropertyName("robot_name")]
        public string RobotName { get; set; }

        [JsonPropertyName("isar_id")]
        public string IsarId { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
