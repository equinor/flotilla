using System.Text.Json.Serialization;

namespace Api.Mqtt.MessageModels
{
#nullable disable
    public class IsarBatteryMessage : MqttMessage
    {
        [JsonPropertyName("battery_level")]
        public float BatteryLevel { get; set; }

        [JsonPropertyName("robot_id")]
        public string RobotId { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
