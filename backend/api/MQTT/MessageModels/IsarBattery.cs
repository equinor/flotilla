using System.Text.Json.Serialization;
using Api.Database.Models;

namespace Api.Mqtt.MessageModels
{
#nullable disable
    public class IsarBatteryMessage : MqttMessage
    {
        [JsonPropertyName("battery_level")]
        public float BatteryLevel { get; set; }

        [JsonPropertyName("battery_state")]
        public BatteryState? BatteryState { get; set; }

        [JsonPropertyName("robot_name")]
        public string RobotName { get; set; }

        [JsonPropertyName("isar_id")]
        public string IsarId { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
