using System.Text.Json.Serialization;
using Api.Services.Models;

namespace Api.Mqtt.MessageModels
{
#nullable disable
    public class IsarTelemetyUpdateMessage : MqttMessage
    {
        [JsonPropertyName("robot_name")]
        public string RobotName { get; set; }

        [JsonPropertyName("isar_id")]
        public string IsarId { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("mediaConnectionType")]
        public MediaConnectionType MediaConnectionType { get; set; }

    }
}
