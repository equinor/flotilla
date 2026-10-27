using System;
using System.Threading;
using System.Threading.Tasks;
using Api.Services;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Api.Test.Services;

public class MissionRecordingServiceTests
{
    private readonly Mock<BlobServiceClient> storage = new();
    private readonly Mock<BlobContainerClient> container = new();
    private readonly Mock<BlobClient> manifest = new();

    public MissionRecordingServiceTests()
    {
        storage.SetupGet(client => client.CanGenerateAccountSasUri).Returns(true);
        storage
            .Setup(client => client.GetBlobContainerClient("mission-recordings"))
            .Returns(container.Object);
        container.SetupGet(client => client.Name).Returns("mission-recordings");
        container
            .Setup(client => client.GetBlobClient("run/session.json"))
            .Returns(manifest.Object);
    }

    [Fact]
    public async Task MissingManifestReturnsNotRecorded()
    {
        manifest
            .Setup(client => client.DownloadContentAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Missing"));

        var result = await CreateService().GetRecordings("run", CancellationToken.None);

        Assert.Equal("run", result.RunId);
        Assert.Equal("NotRecorded", result.Status);
        Assert.Empty(result.Cameras);
        Assert.Empty(result.Gaps);
        Assert.Null(
            await CreateService().GetRecordingAccess("run", "front", CancellationToken.None)
        );
    }

    [Fact]
    public async Task StorageFailurePropagatesToController()
    {
        manifest
            .Setup(client => client.DownloadContentAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(503, "Unavailable"));

        await Assert.ThrowsAsync<RequestFailedException>(() =>
            CreateService().GetRecordings("run", CancellationToken.None)
        );
    }

    [Fact]
    public async Task ReadyCamerasReceiveReadOnlyExpiringUrlsOnlyWhenAccessIsRequested()
    {
        using var cancellation = new CancellationTokenSource();
        var content = BlobsModelFactory.BlobDownloadResult(
            content: BinaryData.FromString(
                """
                {"runId":"run","status":"Ready","engine":"driver","cameras":{
                  "d104-front/front":{"cameraId":"d104-front/front","status":"Ready","key":"front","blobName":"video.mp4","captureMetadata":{"sourceCodec":"H264"}},
                  "arm":{"status":"Processing","key":"arm"}
                }}
                """
            )
        );
        manifest
            .Setup(client => client.DownloadContentAsync(cancellation.Token))
            .ReturnsAsync(Response.FromValue(content, Mock.Of<Response>()));
        var video = new Mock<BlobClient>();
        video.SetupGet(client => client.BlobContainerName).Returns("mission-recordings");
        video.SetupGet(client => client.Name).Returns("video.mp4");
        container.Setup(client => client.GetBlobClient("video.mp4")).Returns(video.Object);
        var earliestExpiry = DateTimeOffset.UtcNow.AddMinutes(30);
        video
            .Setup(client => client.GenerateSasUri(It.IsAny<BlobSasBuilder>()))
            .Returns(
                (BlobSasBuilder sas) =>
                {
                    Assert.Equal("r", sas.Permissions);
                    Assert.Equal("b", sas.Resource);
                    Assert.Equal("mission-recordings", sas.BlobContainerName);
                    Assert.Equal("video.mp4", sas.BlobName);
                    Assert.InRange(
                        sas.ExpiresOn,
                        earliestExpiry,
                        DateTimeOffset.UtcNow.AddMinutes(30)
                    );
                    return new Uri(
                        $"https://storage/video.mp4?disposition={Uri.EscapeDataString(sas.ContentDisposition)}"
                    );
                }
            );

        var result = await CreateService().GetRecordings("run", cancellation.Token);

        var camera = result.Cameras["d104-front/front"];
        Assert.Equal("run", result.RunId);
        Assert.Equal("Ready", result.Status);
        Assert.Equal("driver", result.Metadata["engine"].GetString());
        Assert.Equal("d104-front/front", camera.CameraId);
        Assert.Equal(
            "H264",
            camera.Metadata["captureMetadata"].GetProperty("sourceCodec").GetString()
        );
        video.Verify(client => client.GenerateSasUri(It.IsAny<BlobSasBuilder>()), Times.Never);
        Assert.Null(await CreateService().GetRecordingAccess("run", "arm", cancellation.Token));
        Assert.Null(await CreateService().GetRecordingAccess("run", "missing", cancellation.Token));

        var access = await CreateService().GetRecordingAccess("run", "front", cancellation.Token);

        Assert.NotNull(access);
        Assert.Contains("inline", access.PlaybackUrl);
        Assert.Contains("attachment", access.DownloadUrl);
        Assert.Contains("run-front.mp4", Uri.UnescapeDataString(access.DownloadUrl));
        Assert.InRange(access.ExpiresAt, earliestExpiry, DateTimeOffset.UtcNow.AddMinutes(30));
        video.Verify(client => client.GenerateSasUri(It.IsAny<BlobSasBuilder>()), Times.Exactly(2));
    }

    private MissionRecordingService CreateService() =>
        new(
            new Lazy<BlobServiceClient>(() => storage.Object),
            Mock.Of<IConfiguration>(config =>
                config["MissionRecording:ContainerName"] == "mission-recordings"
            )
        );
}
