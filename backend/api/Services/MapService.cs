using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

public interface IMapService
{
    public abstract Task<String> GetMap();
}
public class MapService: IMapService
{
    private readonly ILogger<MapService> _logger;
    private readonly IOptions<AzureAdOptions> _azureOptions;
    private readonly IOptions<MapBlobOptions> _blobOptions;
    public MapService(
        ILogger<MapService> logger,
        IOptions<AzureAdOptions> azureOptions,
        IOptions<MapBlobOptions> blobOptions)
    {
        _logger = logger;
        _azureOptions = azureOptions;
        _blobOptions = blobOptions;
    }
    public async Task<String> GetMap()
    {
        BlobContainerClient blobContainer = GetBlobContainerClient("kaa"); //, "20190413-2854_bew_02.jpg"
        try
        {
            await ListBlobsFlatListing(blobContainer, 1);
        }
        catch(RequestFailedException e)
        {
            throw e;
        }
        return "success";
    }

    private static async Task ListBlobsFlatListing(BlobContainerClient blobContainerClient, 
                                                int? segmentSize)
    {
        try
        {
            // Call the listing operation and return pages of the specified size.
            var resultSegment = blobContainerClient.GetBlobsAsync()
                .AsPages(default, segmentSize);

            // Enumerate the blobs returned for each page.
            await foreach (Azure.Page<BlobItem> blobPage in resultSegment)
            {
                foreach (BlobItem blobItem in blobPage.Values)
                {
                    Console.WriteLine("Blob name: {0}", blobItem.Name);
                }

                Console.WriteLine();
            }
        }
        catch (RequestFailedException e)
        {
            Console.WriteLine(e.Message);
            throw e;
        }
    }
    private BlobContainerClient GetBlobContainerClient(string asset)
    {
        var serviceClient = new BlobServiceClient(
            new Uri($"https://{_blobOptions.Value.StorageAccount}.blob.core.windows.net"),
            new ClientSecretCredential(_azureOptions.Value.TenantId, _azureOptions.Value.ClientId, _azureOptions.Value.ClientSecret));
        var containerClient = serviceClient.GetBlobContainerClient(asset);
        return containerClient;
    }
}
