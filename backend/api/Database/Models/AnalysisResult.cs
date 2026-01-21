using System.ComponentModel.DataAnnotations;

namespace Api.Database.Models
{
    public class AnalysisResult
    {
        [Key]
        public required string InspectionId { get; set; }

        [Required]
        public required string AnalysisType { get; set; }

        public string? Value { get; set; }

        public string? Unit { get; set; }

        public float? Confidence { get; set; }

        public string? Warning { get; set; }

        public string? StorageAccount { get; set; }

        public string? BlobContainer { get; set; }

        public string? BlobName { get; set; }
    }
}
