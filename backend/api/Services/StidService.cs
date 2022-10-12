using System.Text.Json;
using Api.Database.Models;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.Identity.Web;

namespace Api.Services
{
    public interface IStidService
    {
        public abstract Task<Position> GetTagPosition(string tag);
    }

    public class StidService : IStidService
    {
        public const string ServiceName = "StidApi";
        private readonly IDownstreamWebApi _stidApi;
        private readonly string _installationCode;

        public StidService(IConfiguration config, IDownstreamWebApi downstreamWebApi)
        {
            _stidApi = downstreamWebApi;
            _installationCode = config.GetValue<string>("InstallationCode");
        }
        public async Task<Position> GetTagPosition(string tag)
        {
            string relativePath =
                $"{_installationCode}/tag?tagNo={tag}";

            var response = await _stidApi.CallWebApiForAppAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get;
                    options.RelativePath = relativePath;
                }
            );
            response.EnsureSuccessStatusCode();

            var stidTagPositionResponse = await response.Content.ReadFromJsonAsync<StidTagPositionResponse>();
            if (stidTagPositionResponse is null)
                throw new JsonException("Failed to deserialize tag position from STID");

            // Convert from millimeter to meter
            return new Position(
                x: stidTagPositionResponse.XCoordinate / 1000,
                y: stidTagPositionResponse.YCoordinate / 1000,
                z: stidTagPositionResponse.ZCoordinate / 1000
            );
        }
    }
}
