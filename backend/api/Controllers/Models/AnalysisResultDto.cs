using System.Text.Json.Serialization;
using Api.Database.Models;

namespace Api.Controllers.Models;

public class AnalysisResultDto
{
    [JsonPropertyName("inspectionId")]
    public string InspectionId { get; set; } = "";

    [JsonPropertyName("workflowId")]
    public Guid WorkflowId { get; set; }

    [JsonPropertyName("analysisId")]
    public Guid AnalysisId { get; set; }

    [JsonPropertyName("analysisType")]
    public AnalysisType AnalysisType { get; set; }

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
}
