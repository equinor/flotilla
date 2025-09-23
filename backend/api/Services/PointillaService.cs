using System.Text.Json;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.Identity.Abstractions;

namespace Api.Services
{
    public interface IPointillaService
    {
        public Task<PointillaMapResponse?> GetFloorMap(string plantCode, string floorId);
        public Task<List<PointillaMapResponse>?> GetMap(string plantCode);
        public Task<byte[]?> GetMapTiles(
            string plantCode,
            string floorId,
            int zoomLevel,
            int x,
            int y
        );
    }

    public class PointillaService(ILogger<PointillaService> logger, IDownstreamApi pointillaApi)
        : IPointillaService
    {
        public const string ServiceName = "PointillaApi";

        public async Task<PointillaMapResponse?> GetFloorMap(string plantCode, string floorId)
        {
            string relativePath = $"Map/{plantCode}/{floorId}";

            var response = await pointillaApi.CallApiForUserAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get.Method;
                    options.RelativePath = relativePath;
                }
            );

            response.EnsureSuccessStatusCode();

            try
            {
                var mapInfo = await response.Content.ReadFromJsonAsync<PointillaMapResponse>();

                logger.LogInformation("Successfully fetched available images from Pointilla");
                return mapInfo;
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Failed to deserialize map information from Pointilla");
                throw new JsonException("Failed to deserialize map information from Pointilla");
            }
        }

        public async Task<List<PointillaMapResponse>?> GetMap(string plantCode)
        {
            string relativePath = $"Map/{plantCode}";

            var response = await pointillaApi.CallApiForUserAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get.Method;
                    options.RelativePath = relativePath;
                }
            );

            response.EnsureSuccessStatusCode();

            try
            {
                var mapInfo = await response.Content.ReadFromJsonAsync<
                    List<PointillaMapResponse>
                >();

                if (mapInfo == null || mapInfo.Count == 0)
                {
                    logger.LogWarning("No map information found for plant {PlantCode}", plantCode);
                    return null;
                }
                logger.LogInformation("Successfully fetched available images from Pointilla");
                return mapInfo;
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Failed to deserialize map information from Pointilla");
                throw new JsonException("Failed to deserialize map information from Pointilla");
            }
        }

        public async Task<byte[]?> GetMapTiles(
            string plantCode,
            string floorId,
            int zoomLevel,
            int x,
            int y
        )
        {
            string relativePath = $"Map/tiles/{plantCode}/{floorId}/{zoomLevel}/{x}/{y}";

            var response = await pointillaApi.CallApiForUserAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get.Method;
                    options.RelativePath = relativePath;
                }
            );

            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentType?.MediaType != "image/png")
                throw new InvalidOperationException(
                    $"Unexpected content type: {response.Content.Headers.ContentType}"
                );

            byte[] png = await response.Content.ReadAsByteArrayAsync();
            File.WriteAllBytes("image.png", png);

            return png;
        }
    }
}
