using System.Text.Json.Serialization;

namespace Api.Mqtt.MessageModels
{
    public class SaraAnalysisResultMessage : MqttMessage
    {
        [JsonPropertyName("inspection_id")]
        public required string InspectionId { get; set; }

        [JsonPropertyName("analysisType")]
        public required string AnalysisType { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }

        [JsonPropertyName("confidence")]
        public float? Confidence { get; set; }

        [JsonPropertyName("warning")]
        public string? Warning { get; set; }

        [JsonPropertyName("storageAccount")]
        public required string StorageAccount { get; set; }

        [JsonPropertyName("blobContainer")]
        public required string BlobContainer { get; set; }

        [JsonPropertyName("blobName")]
        public required string BlobName { get; set; }
    }
}
