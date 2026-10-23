using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Controllers.Models;

public class MissionRecordingResponse
{
    public string RunId { get; set; } = "";
    public string Status { get; set; } = "";
    public Dictionary<string, CameraRecordingResponse> Cameras { get; set; } = [];
    public List<RecordingGapResponse> Gaps { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Metadata { get; set; } = [];
}

public class CameraRecordingResponse
{
    public string CameraId { get; set; } = "";
    public string Key { get; set; } = "";
    public string Status { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BlobName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PlaybackUrl { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DownloadUrl { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Metadata { get; set; } = [];
}

public class RecordingGapResponse
{
    public string At { get; set; } = "";
    public string Reason { get; set; } = "";
}
