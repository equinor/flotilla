using Api.Database.Context;
using Api.Database.Models;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

public interface IMapService
{
    public abstract Task<String> FetchMapImage(string missionId);
}
public class MapService: IMapService
{
    private readonly ILogger<MapService> _logger;
    private readonly IOptions<AzureAdOptions> _azureOptions;
    private readonly IOptions<MapBlobOptions> _blobOptions;
    private readonly FlotillaDbContext _dbContext;
    string localFilePath = Directory.GetCurrentDirectory() + "/Database/Maps/Map.png";
    public MapService(
        ILogger<MapService> logger,
        IOptions<AzureAdOptions> azureOptions,
        IOptions<MapBlobOptions> blobOptions,
        FlotillaDbContext dbContext)
    {
        _logger = logger;
        _azureOptions = azureOptions;
        _blobOptions = blobOptions;
        _dbContext = dbContext;

    }

    public async Task<String> FetchMapImage(string missionId)
    {
        Mission? currentMission = _dbContext.Missions.Find(missionId);
        if (currentMission == null)
        {
            _logger.LogError($"Mission not found for mission ID {missionId}");
            throw new DirectoryNotFoundException($"Mission not found");
        };

        String filePath = await DownloadMapImageFromBlobStorage(currentMission);
        return filePath;
    }

    private BlobContainerClient GetBlobContainerClient(string asset)
    {
        var serviceClient = new BlobServiceClient(
            new Uri($"https://{_blobOptions.Value.StorageAccount}.blob.core.windows.net"),
            new ClientSecretCredential(_azureOptions.Value.TenantId, _azureOptions.Value.ClientId, _azureOptions.Value.ClientSecret));
        var containerClient = serviceClient.GetBlobContainerClient(asset);
        return containerClient;
    }

    private async Task<String> DownloadMapImageFromBlobStorage(Mission currentMission)
    {
        BlobContainerClient blobContainer = GetBlobContainerClient(currentMission.AssetCode);
        BlobClient blobClient = blobContainer.GetBlobClient("k-lab.png");
        try
        {
            await blobClient.DownloadToAsync(localFilePath);
           
        }
        catch(RequestFailedException e)
        {
            _logger.LogError($"Directory not found: {e.Message}");
            throw e;
        }
        return localFilePath;
    } 
}
