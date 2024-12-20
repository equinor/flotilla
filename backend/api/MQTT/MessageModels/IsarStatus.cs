using System.Text.Json.Serialization;
using Api.Database.Models;

namespace Api.Mqtt.MessageModels
{
#nullable disable
    public class IsarStatusMessage : MqttMessage
    {
        [JsonPropertyName("robot_name")]
        public string RobotName { get; set; }

        [JsonPropertyName("isar_id")]
        public string IsarId { get; set; }

        [JsonPropertyName("status")]
        public RobotStatus Status { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
