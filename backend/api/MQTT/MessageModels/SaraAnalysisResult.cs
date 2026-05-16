using System.Text.Json.Serialization;

namespace Api.Mqtt.MessageModels
{
    public class SaraAnalysisResultMessage : MqttMessage
    {
        [JsonPropertyName("inspection_ids")]
        public required List<string> InspectionIds { get; set; }

        [JsonPropertyName("analysis_group_id")]
        public string? AnalysisGroupId { get; set; }

        [JsonPropertyName("workflow_id")]
        public required Guid WorkflowId { get; set; }

        [JsonPropertyName("analysis_run_id")]
        public required Guid AnalysisRunId { get; set; }

        [JsonPropertyName("analysis_id")]
        public required Guid AnalysisId { get; set; }

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
        public string? StorageAccount { get; set; }

        [JsonPropertyName("blobContainer")]
        public string? BlobContainer { get; set; }

        [JsonPropertyName("blobName")]
        public string? BlobName { get; set; }
    }
}
