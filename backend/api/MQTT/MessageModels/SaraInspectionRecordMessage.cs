using System.Text.Json.Serialization;

namespace Api.Mqtt.MessageModels
{
    public class SaraInspectionRecordMessage : MqttMessage
    {
        [JsonPropertyName("inspection_id")]
        public required string InspectionId { get; set; }
    }
}
