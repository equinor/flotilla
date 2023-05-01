using System.Globalization;
using Api.Database.Models;
using Api.Options;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace Api.Services
{
    public interface IMapService
    {
        public abstract Task<byte[]> FetchMapImage(Mission mission);
        public abstract Task AssignMapToMission(Mission mission);
    }

    public class MapService : IMapService
    {
        private readonly ILogger<MapService> _logger;
        private readonly IOptions<AzureAdOptions> _azureOptions;
        private readonly IOptions<MapBlobOptions> _blobOptions;

        public MapService(
            ILogger<MapService> logger,
            IOptions<AzureAdOptions> azureOptions,
            IOptions<MapBlobOptions> blobOptions
        )
        {
            _logger = logger;
            _azureOptions = azureOptions;
            _blobOptions = blobOptions;
        }

        public async Task<byte[]> FetchMapImage(Mission mission)
        {
            return await DownloadMapImageFromBlobStorage(mission);
        }

        public async Task AssignMapToMission(Mission mission)
        {
            string mostSuitableMap;
            var boundaries = new Dictionary<string, Boundary>();
            var imageSizes = new Dictionary<string, int[]>();
            var blobContainerClient = GetBlobContainerClient(
                mission.AssetCode.ToLower(CultureInfo.CurrentCulture)
            );
            try
            {
                var resultSegment = blobContainerClient
                    .GetBlobsAsync(BlobTraits.Metadata)
                    .AsPages();

                await foreach (var blobPage in resultSegment)
                {
                    foreach (var blobItem in blobPage.Values)
                    {
                        try
                        {
                            boundaries.Add(blobItem.Name, ExtractMapMetadata(blobItem));
                            imageSizes.Add(blobItem.Name, ExtractImageSize(blobItem));
                        }
                        catch (FormatException)
                        {
                            continue;
                        }
                        catch (KeyNotFoundException)
                        {
                            continue;
                        }
                    }
                }
            }
            catch (RequestFailedException e)
            {
                _logger.LogWarning(
                    "Unable to find any map files for asset code {AssetCode}: {error message}",
                    mission.AssetCode,
                    e.Message
                );
                return;
            }
            try
            {
                mostSuitableMap = FindMostSuitableMap(boundaries, mission.Tasks);
            }
            catch (ArgumentOutOfRangeException)
            {
                _logger.LogWarning("Unable to find a map for mission '{missionId}'", mission.Id);
                return;
            }
            var map = new MissionMap
            {
                MapName = mostSuitableMap,
                Boundary = boundaries[mostSuitableMap],
                TransformationMatrices = new TransformationMatrices(
                    boundaries[mostSuitableMap].As2DMatrix()[0],
                    boundaries[mostSuitableMap].As2DMatrix()[1],
                    imageSizes[mostSuitableMap][0],
                    imageSizes[mostSuitableMap][1]
                )
            };
            mission.Map = map;
            _logger.LogInformation("Assigned map {map} to mission {mission}", mostSuitableMap, mission.Name);
        }

        private BlobContainerClient GetBlobContainerClient(string asset)
        {
            var serviceClient = new BlobServiceClient(
                new Uri($"https://{_blobOptions.Value.StorageAccount}.blob.core.windows.net"),
                new ClientSecretCredential(
                    _azureOptions.Value.TenantId,
                    _azureOptions.Value.ClientId,
                    _azureOptions.Value.ClientSecret
                )
            );
            var containerClient = serviceClient.GetBlobContainerClient(asset);
            return containerClient;
        }

        private async Task<byte[]> DownloadMapImageFromBlobStorage(Mission currentMission)
        {
            if (currentMission.Map is null)
                throw new ArgumentNullException(
                    nameof(currentMission.Map),
                    "Cannot fetch map image from blob when map is null"
                );

            var blobContainerClient = GetBlobContainerClient(
                currentMission.AssetCode.ToLower(CultureInfo.CurrentCulture)
            );
            var blobClient = blobContainerClient.GetBlobClient(currentMission.Map.MapName);

            await using var stream = await blobClient.OpenReadAsync();

            byte[] result = new byte[stream.Length];
            // ReSharper disable once MustUseReturnValue
            await stream.ReadAsync(result);

            return result;
        }

        private Boundary ExtractMapMetadata(BlobItem map)
        {
            try
            {
                double lowerLeftX =
                    double.Parse(map.Metadata["lowerLeftX"], CultureInfo.CurrentCulture) / 1000;
                double lowerLeftY =
                    double.Parse(map.Metadata["lowerLeftY"], CultureInfo.CurrentCulture) / 1000;
                double upperRightX =
                    double.Parse(map.Metadata["upperRightX"], CultureInfo.CurrentCulture) / 1000;
                double upperRightY =
                    double.Parse(map.Metadata["upperRightY"], CultureInfo.CurrentCulture) / 1000;
                double minElevation =
                    double.Parse(map.Metadata["minElevation"], CultureInfo.CurrentCulture) / 1000;
                double maxElevation =
                    double.Parse(map.Metadata["maxElevation"], CultureInfo.CurrentCulture) / 1000;
                return new Boundary(
                    lowerLeftX,
                    lowerLeftY,
                    upperRightX,
                    upperRightY,
                    minElevation,
                    maxElevation
                );
            }
            catch (FormatException e)
            {
                _logger.LogWarning(
                    "Unable to extract metadata from map {map.Name}: {e.Message}",
                    map.Name,
                    e.Message
                );
                throw e;
            }
            catch (KeyNotFoundException e)
            {
                _logger.LogWarning(
                    "Map {map.Name} is missing required metadata: {e.message}",
                    map.Name,
                    e.Message
                );
                throw e;
            }
        }

        private int[] ExtractImageSize(BlobItem map)
        {
            try
            {
                int x = int.Parse(map.Metadata["imageWidth"], CultureInfo.CurrentCulture);
                int y = int.Parse(map.Metadata["imageHeight"], CultureInfo.CurrentCulture);
                return new int[] { x, y };
            }
            catch (FormatException e)
            {
                _logger.LogWarning(
                    "Unable to extract image size from map {map.Name}: {e.Message}",
                    map.Name,
                    e.Message
                );
                throw e;
            }
        }

        private string FindMostSuitableMap(
            Dictionary<string, Boundary> boundaries,
            IList<MissionTask> tasks
        )
        {
            var mapCoverage = new Dictionary<string, int>();
            foreach (var boundary in boundaries)
            {
                mapCoverage.Add(boundary.Key, FractionOfTagsWithinBoundary(boundary: boundary.Value, tasks: tasks));
            }
            string keyOfMaxValue = mapCoverage.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;

            if (mapCoverage[keyOfMaxValue] < 0.5) throw new ArgumentOutOfRangeException(nameof(tasks));

            return keyOfMaxValue;
        }

        private static bool TagWithinBoundary(Boundary boundary, MissionTask task)
        {
            return task.InspectionTarget.X > boundary.X1
                   && task.InspectionTarget.X < boundary.X2 && task.InspectionTarget.Y > boundary.Y1
                   && task.InspectionTarget.Y < boundary.Y2 && task.InspectionTarget.Z > boundary.Z1
                   && task.InspectionTarget.Z < boundary.Z2;
        }

        private int FractionOfTagsWithinBoundary(Boundary boundary, IList<MissionTask> tasks)
        {
            int tagsWithinBoundary = 0;
            foreach (var task in tasks)
            {
                try
                {
                    if (TagWithinBoundary(boundary: boundary, task: task)) tagsWithinBoundary++;
                }
                catch
                {
                    _logger.LogWarning("An error occurred while checking if tag was within boundary");
                }
            }
            tagsWithinBoundary++;

            return tagsWithinBoundary / tasks.Count;
        }
    }
}
