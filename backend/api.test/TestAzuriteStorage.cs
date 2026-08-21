using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Testcontainers.Azurite;
using Xunit;

namespace Api.Test;

public class TestAzuriteStorage : IAsyncLifetime
{
    private AzuriteContainer _container = null!;
    private string _connectionString = null!;

    public async ValueTask InitializeAsync()
    {
        (_container, _connectionString) = await TestSetupHelpers.ConfigureAzuriteStorage();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CheckThatJsonFileCanBeStored()
    {
        var clientOptions = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2025_11_05);
        var serviceClient = new BlobServiceClient(_connectionString, clientOptions);
        var containerClient = serviceClient.GetBlobContainerClient("test-container");
        await containerClient.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);

        var blobClient = containerClient.GetBlobClient("test.json");
        await blobClient.UploadAsync(
            BinaryData.FromString("{\"message\":\"hello\"}"),
            TestContext.Current.CancellationToken
        );

        Assert.True(await blobClient.ExistsAsync(TestContext.Current.CancellationToken));
    }
}
