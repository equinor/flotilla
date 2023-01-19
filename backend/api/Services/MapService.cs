using System.Globalization;
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
    public abstract Task<MissionMap> AssignMapToMission(string assetCode, List<PlannedTask> tasks);
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
        FlotillaDbContext dbContext
    )
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

    public async Task<MissionMap> AssignMapToMission(string assetCode, List<PlannedTask> tasks)
    {
        string mostSuitableMap;
        Dictionary<string,Boundary> boundaries = new Dictionary<string,Boundary>();
        Dictionary<string, int[]> imageSizes = new Dictionary<string, int[]>();
        BlobContainerClient blobContainerClient = GetBlobContainerClient(assetCode.ToLower(CultureInfo.CurrentCulture));
        try
        {
            var resultSegment = blobContainerClient.GetBlobsAsync(BlobTraits.Metadata).AsPages();

            await foreach (Page<BlobItem> blobPage in resultSegment)
            {
                foreach (BlobItem blobItem in blobPage.Values)
                {
                    try
                    {
                        boundaries.Add(blobItem.Name, ExtractMapMetadata(blobItem));
                        imageSizes.Add(blobItem.Name, ExtractImageSize(blobItem));
                    }
                    catch(FormatException)
                    {
                        continue;
                    }
                }
            }
        }
        catch (RequestFailedException e)
        {
            _logger.LogError($"Unable to extract all metadata from map: {e.Message}");
            return new MissionMap{MapName = "error"};
        }
        try
        {
            mostSuitableMap = FindMostSuitableMap(boundaries, tasks);
        }
        catch (ArgumentOutOfRangeException)
        {
            return new MissionMap{MapName = "error"};
        }
        return new MissionMap{
            MapName = mostSuitableMap,
            Boundary = boundaries[mostSuitableMap],
            TransformationMatrices = new TransformationMatrices(
                boundaries[mostSuitableMap].getBoundary()[0],
                boundaries[mostSuitableMap].getBoundary()[1],
                imageSizes[mostSuitableMap][0],
                imageSizes[mostSuitableMap][1]
            )};
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
        BlobContainerClient blobContainerClient = GetBlobContainerClient(currentMission.AssetCode.ToLower(CultureInfo.CurrentCulture));
        BlobClient blobClient = blobContainerClient.GetBlobClient(currentMission.Map.MapName);
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

    private Boundary ExtractMapMetadata(BlobItem map)
    {
        try
        {
            var lowerLeftX = double.Parse(map.Metadata["lowerLeftX"], CultureInfo.CurrentCulture)/1000;
            var lowerLeftY = double.Parse(map.Metadata["lowerLeftY"], CultureInfo.CurrentCulture)/1000;
            var upperRightX = double.Parse(map.Metadata["upperRightX"], CultureInfo.CurrentCulture)/1000;
            var upperRightY = double.Parse(map.Metadata["upperRightY"], CultureInfo.CurrentCulture)/1000;
            return new Boundary(lowerLeftX, lowerLeftY, upperRightX, upperRightY);
        }
        catch(FormatException e)
        {
            _logger.LogWarning($"Unable to extract metadata from map {map.Name}: {e.Message}");
            throw e;
        }
    }

    private int[] ExtractImageSize(BlobItem map)
    {
        try
        {
            var x = int.Parse(map.Metadata["imageWidth"], CultureInfo.CurrentCulture);
            var y = int.Parse(map.Metadata["imageHeight"], CultureInfo.CurrentCulture); 
            return new int[] {x, y};
        }
        catch(FormatException e)
        {
            _logger.LogWarning($"Unable to extract image size from map {map.Name}: {e.Message}");
            throw e;
        }
    }

    private string FindMostSuitableMap(Dictionary<string, Boundary> boundaries, List<PlannedTask> tasks)
    {
        string mostSuitableMap = "";
        string referenceMap = "";
        foreach(var boundary in boundaries)
        {
            if (!string.IsNullOrEmpty(mostSuitableMap))
            {
                referenceMap = mostSuitableMap;
                //If the current map is lower resolution than the best map, it's not worth checking.
                if(!CheckMapIsHigherResolution(boundary.Value.getBoundary(), boundaries[referenceMap].getBoundary()))
                {
                    continue;
                }
            }
            if (CheckTagsInBoundary(boundary.Value.getBoundary(), tasks))
            {
                mostSuitableMap = boundary.Key;
            }
        }
        if (string.IsNullOrEmpty(mostSuitableMap))
        {
            _logger.LogWarning("Unable to find a map for the given tasks.");
            throw new ArgumentOutOfRangeException(nameof(tasks));
        }
        return mostSuitableMap;
    }
    
    private bool CheckTagsInBoundary(List<double[]> boundary, List<PlannedTask> tasks)
    {
        foreach(PlannedTask task in tasks)
            {
                if(task.TagPosition.X < boundary[0][0] | task.TagPosition.X > boundary[1][0])
                {
                    return false;
                }
                if(task.TagPosition.Y < boundary[0][1] | task.TagPosition.Y > boundary[1][1])
                {
                    return false;
                }
            }
        return true;
    }

    private bool CheckMapIsHigherResolution(List<double[]> checkMap, List<double[]> referenceMap)
    {
        if(checkMap[0][0] < referenceMap[0][0] | checkMap[0][1] < referenceMap[0][1])
        {
            return false;
        }
        if(checkMap[1][0] > referenceMap[1][0] | checkMap[1][1] > referenceMap[1][1])
        {
            return false;
        }
        return true;
    }
}
