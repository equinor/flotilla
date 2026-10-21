using System.Text.Json;
using Api.Configurations;
using Api.Controllers.Models;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace Api.Services;

public interface IMissionRecordingService
{
    Task<MissionRecordingResponse> GetRecordings(string runId, CancellationToken cancellationToken);
}

/// <summary>Reads private recording manifests and provides short-lived video access.</summary>
public class MissionRecordingService(
    Lazy<BlobServiceClient> storageClient,
    IConfiguration configuration
) : IMissionRecordingService
{
    private static readonly TimeSpan VideoUrlLifetime = TimeSpan.FromMinutes(30);

    /// <summary>Returns the session with video URLs, or NotRecorded when no manifest exists.</summary>
    public async Task<MissionRecordingResponse> GetRecordings(
        string runId,
        CancellationToken cancellationToken
    )
    {
        var containerName = MissionRecordingConfigurations.GetRecordingContainerName(configuration);
        var container = storageClient.Value.GetBlobContainerClient(containerName);
        MissionRecordingResponse document;
        try
        {
            document = await DownloadManifest(container, runId, cancellationToken);
        }
        catch (RequestFailedException error) when (error.Status == 404)
        {
            return CreateNotRecordedResponse(runId);
        }
        await AddCameraVideoUrls(container, document, runId, cancellationToken);
        return document;
    }

    /// <summary>Downloads the driver's session catalog without retrieving video content.</summary>
    private async Task<MissionRecordingResponse> DownloadManifest(
        BlobContainerClient container,
        string runId,
        CancellationToken cancellationToken
    )
    {
        var metadata = container.GetBlobClient($"{runId}/session.json");
        var content = await metadata.DownloadContentAsync(cancellationToken);
        return content.Value.Content.ToObjectFromJson<MissionRecordingResponse>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        )!;
    }

    /// <summary>Adds playback and download links only for finalized, uploaded cameras.</summary>
    private async Task AddCameraVideoUrls(
        BlobContainerClient container,
        MissionRecordingResponse document,
        string runId,
        CancellationToken cancellationToken
    )
    {
        var readyCameras = document
            .Cameras.Values.Where(camera => camera.Status == "Ready")
            .ToList();
        if (readyCameras.Count == 0)
            return;

        var expiresOn = DateTimeOffset.UtcNow.Add(VideoUrlLifetime);
        UserDelegationKey? delegationKey = null;
        if (!storageClient.Value.CanGenerateAccountSasUri)
        {
            delegationKey = (
                await storageClient.Value.GetUserDelegationKeyAsync(
                    DateTimeOffset.UtcNow.AddMinutes(-5),
                    expiresOn.AddMinutes(5),
                    cancellationToken
                )
            ).Value;
        }

        foreach (var camera in readyCameras)
        {
            var blob = container.GetBlobClient(camera.BlobName);
            camera.PlaybackUrl = CreateVideoAccessUrl(blob, "inline", expiresOn, delegationKey);
            camera.DownloadUrl = CreateVideoAccessUrl(
                blob,
                $"attachment; filename=\"{runId}-{camera.Key}.mp4\"",
                expiresOn,
                delegationKey
            );
        }
    }

    /// <summary>Signs read-only access with the requested browser content disposition.</summary>
    private string CreateVideoAccessUrl(
        BlobClient blob,
        string contentDisposition,
        DateTimeOffset expiresOn,
        UserDelegationKey? delegationKey
    )
    {
        var sas = new BlobSasBuilder(BlobSasPermissions.Read, expiresOn)
        {
            BlobContainerName = blob.BlobContainerName,
            BlobName = blob.Name,
            Resource = "b",
            ContentDisposition = contentDisposition,
        };
        if (delegationKey is null)
            return blob.GenerateSasUri(sas).ToString();

        sas.Protocol = SasProtocol.Https;
        return new BlobUriBuilder(blob.Uri)
        {
            Sas = sas.ToSasQueryParameters(delegationKey, storageClient.Value.AccountName),
        }
            .ToUri()
            .ToString();
    }

    /// <summary>Preserves the catalog response shape for missions without a recording.</summary>
    private static MissionRecordingResponse CreateNotRecordedResponse(string runId) =>
        new() { RunId = runId, Status = "NotRecorded" };
}
