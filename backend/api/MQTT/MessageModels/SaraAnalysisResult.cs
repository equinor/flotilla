using System.Text.Json.Serialization;

namespace Api.Mqtt.MessageModels
{
#nullable disable
    public class SaraAnalysisResultMessage : MqttMessage
    {
        [JsonPropertyName("inspection_id")]
        public string InspectionId { get; set; }

        [JsonPropertyName("analysisName")]
        public required string AnalysisType { get; set; }

        [JsonPropertyName("regressionResult")]
        public float RegressionResult { get; set; }

        [JsonPropertyName("classResult")]
        public string ClassResult { get; set; }

        [JsonPropertyName("storageAccount")]
        public required string StorageAccount { get; set; }

        [JsonPropertyName("blobContainer")]
        public required string BlobContainer { get; set; }

        [JsonPropertyName("blobName")]
        public required string BlobName { get; set; }
    }
}
