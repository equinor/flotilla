using System.Globalization;
using Api.Database.Models;
using Api.Options;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace Api.Services
{
    public interface IMapService
    {
        public Task<byte[]> FetchMapImage(string mapName, string assetCode);
        public Task<MapMetadata?> ChooseMapFromPositions(IList<Position> positions, string assetCode);
        public Task AssignMapToMission(MissionRun mission);
    }

    public class MapService : IMapService
    {
        private readonly IOptions<MapBlobOptions> _blobOptions;
        private readonly IBlobService _blobService;
        private readonly ILogger<MapService> _logger;


        public MapService(
            ILogger<MapService> logger,
            IOptions<MapBlobOptions> blobOptions,
            IBlobService blobService
        )
        {
            _logger = logger;
            _blobOptions = blobOptions;
            _blobService = blobService;
        }

        public async Task<byte[]> FetchMapImage(string mapName, string assetCode)
        {
            return await _blobService.DownloadBlob(mapName, assetCode, _blobOptions.Value.StorageAccount);
        }

        public async Task<MapMetadata?> ChooseMapFromPositions(IList<Position> positions, string assetCode)
        {
            var boundaries = new Dictionary<string, Boundary>();
            var imageSizes = new Dictionary<string, int[]>();

            var blobs = _blobService.FetchAllBlobs(assetCode, _blobOptions.Value.StorageAccount);

            await foreach (var blob in blobs)
            {
                try
                {
                    boundaries.Add(blob.Name, ExtractMapMetadata(blob));
                    imageSizes.Add(blob.Name, ExtractImageSize(blob));
                }
                catch (Exception e) when (e is FormatException || e is KeyNotFoundException)
                {
                    _logger.LogWarning(e, "Failed to extract boundary and image size for {MapName}", blob.Name);
                }
            }

            string mostSuitableMap = FindMostSuitableMap(boundaries, positions);
            var map = new MapMetadata
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

        public async Task AssignMapToMission(MissionRun missionRun)
        {
            MapMetadata? mapMetadata;
            var positions = new List<Position>();
            foreach (var task in missionRun.Tasks)
            {
                positions.Add(task.InspectionTarget);
            }
            try
            {
                mapMetadata = await ChooseMapFromPositions(positions, missionRun.AssetCode);
            }
            catch (ArgumentOutOfRangeException)
            {
                _logger.LogWarning("Unable to find a map for mission '{missionId}'", missionRun.Id);
                return;
            }

            if (mapMetadata == null)
            {
                return;
            }

            missionRun.MapMetadata = mapMetadata;
            _logger.LogInformation("Assigned map {map} to mission {mission}", mapMetadata.MapName, missionRun.Name);
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
                return new[]
                {
                    x, y
                };
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
                mapCoverage.Add(boundary.Key, FractionOfTagsWithinBoundary(boundary.Value, positions));
            }
            string keyOfMaxValue = mapCoverage.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;

            if (mapCoverage[keyOfMaxValue] < 0.5)
            {
                throw new ArgumentOutOfRangeException(nameof(positions));
            }

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
                    if (TagWithinBoundary(boundary, position))
                    {
                        tagsWithinBoundary++;
                    }
                }
                catch
                {
                    _logger.LogWarning("An error occurred while checking if tag was within boundary");
                }
            }

            return tagsWithinBoundary / (float)positions.Count;
        }
    }
}
