using System.Text.Json.Serialization;

namespace Api.Mqtt.MessageModels
{
#nullable disable
    public class IsarConnectMessage : MqttMessage
    {
        [JsonPropertyName("robot_name")]
        public string RobotName { get; set; }

        [JsonPropertyName("isar_id")]
        public string IsarId { get; set; }

        [JsonPropertyName("host")]
        public string Host { get; set; }

        [JsonPropertyName("port")]
        public int Port { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
