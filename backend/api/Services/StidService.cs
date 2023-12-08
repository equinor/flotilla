using System.Text.Json;
using Api.Database.Models;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.Identity.Abstractions;

namespace Api.Services
{
    public interface IStidService
    {
        public abstract Task<Position> GetTagPosition(string tag, string installationCode);
        public abstract Task<Area> GetTagArea(string tag, string installationCode);
    }

    public class StidService(ILogger<StidService> logger, IDownstreamApi stidApi, IAreaService areaService) : IStidService
    {
        public const string ServiceName = "StidApi";

        public async Task<Position> GetTagPosition(string tag, string installationCode)
        {
            string relativePath = $"{installationCode}/tag?tagNo={tag}";

            var response = await stidApi.CallApiForAppAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get.Method;
                    options.RelativePath = relativePath;
                }
            );
            response.EnsureSuccessStatusCode();

            var stidTagPositionResponse =
                await response.Content.ReadFromJsonAsync<StidTagPositionResponse>() ?? throw new JsonException("Failed to deserialize tag position from STID");

            // Convert from millimeter to meter
            return new Position(
                x: stidTagPositionResponse.XCoordinate / 1000,
                y: stidTagPositionResponse.YCoordinate / 1000,
                z: stidTagPositionResponse.ZCoordinate / 1000
            );
        }

        public async Task<Area> GetTagArea(string tag, string installationCode)
        {
            string relativePath = $"{installationCode}/tag?tagNo={tag}";

            var response = await stidApi.CallApiForAppAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get.Method;
                    options.RelativePath = relativePath;
                }
            );
            response.EnsureSuccessStatusCode();

            var stidTagAreaResponse =
                await response.Content.ReadFromJsonAsync<StidTagAreaResponse>() ?? throw new JsonException("Failed to deserialize tag position from STID");

            if (stidTagAreaResponse.LocationCode == null)
            {
                string errorMessage = $"Could not get area name from STID for tag {tag}";
                logger.LogError("{Message}", errorMessage);
                throw new AreaNotFoundException(errorMessage);
            }

            var area = await areaService.ReadByInstallationAndName(installationCode, stidTagAreaResponse.LocationCode);

            if (area == null)
            {
                string errorMessage = $"Could not find area for area name {stidTagAreaResponse.LocationCode}";
                logger.LogError("{Message}", errorMessage);
                throw new AreaNotFoundException(errorMessage);
            }

            return area;
        }
    }
}
