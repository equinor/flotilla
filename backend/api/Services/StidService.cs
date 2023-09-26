using System.Text.Json;
using Api.Database.Models;
using Api.Services.Models;
using Microsoft.Identity.Abstractions;

namespace Api.Services
{
    public interface IStidService
    {
        public abstract Task<Position> GetTagPosition(string tag, string installationCode);
    }

    public class StidService : IStidService
    {
        public const string ServiceName = "StidApi";
        private readonly IDownstreamApi _stidApi;

        public StidService(IDownstreamApi downstreamWebApi)
        {
            _stidApi = downstreamWebApi;
        }

        public async Task<Position> GetTagPosition(string tag, string installationCode)
        {
            string relativePath = $"{installationCode}/tag?tagNo={tag}";

            var response = await _stidApi.CallApiForAppAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get;
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
    }
}
