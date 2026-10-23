using System.Text.Json;
using Api.Controllers.Models;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace Api.Services;

public interface IMissionRecordingService
{
    Task<MissionRecordingResponse> GetRecordings(string runId, CancellationToken cancellationToken);
}

/// <summary>Reads private recording manifests and provides short-lived video access.</summary>
public class MissionRecordingService(Lazy<BlobContainerClient> containerClient)
    : IMissionRecordingService
{
    public const string ContainerName = "mission-recordings";
    private static readonly TimeSpan VideoUrlLifetime = TimeSpan.FromMinutes(15);

    /// <summary>Returns the session with video URLs, or NotRecorded when no manifest exists.</summary>
    public async Task<MissionRecordingResponse> GetRecordings(
        string runId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var document = await DownloadManifest(runId, cancellationToken);
            AddCameraVideoUrls(document, runId);
            return document;
        }
        catch (RequestFailedException error) when (error.Status == 404)
        {
            return CreateNotRecordedResponse(runId);
        }
    }

    /// <summary>Downloads the driver's session catalog without retrieving video content.</summary>
    private async Task<MissionRecordingResponse> DownloadManifest(
        string runId,
        CancellationToken cancellationToken
    )
    {
        var metadata = containerClient.Value.GetBlobClient($"{runId}/session.json");
        var content = await metadata.DownloadContentAsync(cancellationToken);
        return content.Value.Content.ToObjectFromJson<MissionRecordingResponse>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        )!;
    }

    /// <summary>Adds playback and download links only for finalized, uploaded cameras.</summary>
    private void AddCameraVideoUrls(MissionRecordingResponse document, string runId)
    {
        foreach (var camera in document.Cameras.Values)
        {
            if (camera.Status != "Ready")
                continue;

            var blob = containerClient.Value.GetBlobClient(camera.BlobName);
            camera.PlaybackUrl = CreateVideoAccessUrl(blob, "inline");
            camera.DownloadUrl = CreateVideoAccessUrl(
                blob,
                $"attachment; filename=\"{runId}-{camera.Key}.mp4\""
            );
        }
    }

    /// <summary>Signs read-only access with the requested browser content disposition.</summary>
    private string CreateVideoAccessUrl(BlobClient blob, string contentDisposition)
    {
        var sas = new BlobSasBuilder(
            BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.Add(VideoUrlLifetime)
        )
        {
            BlobContainerName = containerClient.Value.Name,
            BlobName = blob.Name,
            Resource = "b",
            ContentDisposition = contentDisposition,
        };
        return blob.GenerateSasUri(sas).ToString();
    }

    /// <summary>Preserves the catalog response shape for missions without a recording.</summary>
    private static MissionRecordingResponse CreateNotRecordedResponse(string runId) =>
        new() { RunId = runId, Status = "NotRecorded" };
}
