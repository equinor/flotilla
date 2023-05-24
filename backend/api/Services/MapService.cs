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
        public abstract Task<byte[]> FetchMapImage(string mapName, string assetCode);
        public abstract Task<MissionMap?> ChooseMapFromPositions(IList<Position> positions, string assetCode);
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

        public async Task<byte[]> FetchMapImage(string mapName, string assetCode)
        {
            return await DownloadMapImageFromBlobStorage(mapName, assetCode);
        }

        public async Task<MissionMap?> ChooseMapFromPositions(IList<Position> positions, string assetCode)
        {
            var boundaries = new Dictionary<string, Boundary>();
            var imageSizes = new Dictionary<string, int[]>();
            var blobContainerClient = GetBlobContainerClient(
                assetCode.ToLower(CultureInfo.CurrentCulture)
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
                    assetCode,
                    e.Message
                );
                return null;
            }

            string mostSuitableMap = FindMostSuitableMap(boundaries, positions);

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
            return map;
        }

        public async Task AssignMapToMission(Mission mission)
        {
            MissionMap? map;
            var positions = new List<Position>();
            foreach (var task in mission.Tasks)
            {
                positions.Add(task.InspectionTarget);
            }
            try
            {
                map = await ChooseMapFromPositions(positions, mission.AssetCode);
            }
            catch (ArgumentOutOfRangeException)
            {
                _logger.LogWarning("Unable to find a map for mission '{missionId}'", mission.Id);
                return;
            }

            if (map == null)
            {
                return;
            }

            mission.Map = map;
            _logger.LogInformation("Assigned map {map} to mission {mission}", map.MapName, mission.Name);
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

        private async Task<byte[]> DownloadMapImageFromBlobStorage(string mapName, string assetCode)
        {
            var blobContainerClient = GetBlobContainerClient(
                assetCode.ToLower(CultureInfo.CurrentCulture)
            );
            var blobClient = blobContainerClient.GetBlobClient(mapName);

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
            IList<Position> positions
        )
        {
            var mapCoverage = new Dictionary<string, float>();
            foreach (var boundary in boundaries)
            {
                mapCoverage.Add(boundary.Key, FractionOfTagsWithinBoundary(boundary: boundary.Value, positions: positions));
            }
            string keyOfMaxValue = mapCoverage.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;

            if (mapCoverage[keyOfMaxValue] < 0.5) throw new ArgumentOutOfRangeException(nameof(positions));

            return keyOfMaxValue;
        }

        private static bool TagWithinBoundary(Boundary boundary, Position position)
        {
            return position.X > boundary.X1
                   && position.X < boundary.X2 && position.Y > boundary.Y1
                   && position.Y < boundary.Y2 && position.Z > boundary.Z1
                   && position.Z < boundary.Z2;
        }

        private float FractionOfTagsWithinBoundary(Boundary boundary, IList<Position> positions)
        {
            int tagsWithinBoundary = 0;
            foreach (var position in positions)
            {
                try
                {
                    if (TagWithinBoundary(boundary: boundary, position: position)) tagsWithinBoundary++;
                }
                catch
                {
                    _logger.LogWarning("An error occurred while checking if tag was within boundary");
                }
            }
            tagsWithinBoundary++;

            return tagsWithinBoundary / (float)positions.Count;
        }
    }
}
