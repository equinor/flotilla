using System.Text.Json.Serialization;

namespace Api.Mqtt.MessageModels
{
#nullable disable
    public class IsarStartupMessage : MqttMessage
    {
        [JsonPropertyName("isar_id")]
        public string IsarId { get; set; }

        [JsonPropertyName("installation_code")]
        public string InstallationCode { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
