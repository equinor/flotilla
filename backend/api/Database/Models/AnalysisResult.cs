
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Api.Database.Models
{
    public class AnalysisResult
    {
        [JsonPropertyName("inspectionId")]
        [Key]
        public required string InspectionId { get; set; }

        [JsonPropertyName("analysisType")]
        public required string AnalysisType { get; set; }

        [JsonPropertyName("displayText")]
        public required string DisplayText { get; set; }

        [JsonPropertyName("value")]
        public float? Value { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }

        [JsonPropertyName("class")]
        public string? Class { get; set; }

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
