using System.Net;
using System.Text;
using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.Identity.Abstractions;

namespace Api.Services
{
    public interface IInspectionService
    {
        public string GetInspectionName(
            string installationCode,
            Position position,
            string? tag,
            string robotName,
            string? inspectionDescription
        );
        public Task<double> FetchCO2ConcentrationFromQuery(FetchCO2Query query);
    }

    public class InspectionService(IDownstreamApi saraApi) : IInspectionService
    {
        public const string ServiceName = "SARA";

        public static int FloorWithTolerance(double value, double tolerance = 0.06)
        {
            var floored = (int)Math.Floor(value);
            if (value - floored >= 1 - tolerance)
                return floored + 1;
            return floored;
        }

        public string GetInspectionName(
            string installationCode,
            Position position,
            string? tag,
            string robotName,
            string? inspectionDescription
        )
        {
            installationCode = installationCode.ToLowerInvariant();
            var positionAsENUString =
                $"{FloorWithTolerance(position.X)}E_{FloorWithTolerance(position.Y)}N_{FloorWithTolerance(position.Z)}U";
            string description = inspectionDescription?.Replace(" ", "-") ?? string.Empty;
            return $"{installationCode}_{positionAsENUString}_{tag}_{robotName}_{description}";
        }

        public async Task<double> FetchCO2ConcentrationFromQuery(FetchCO2Query query)
        {
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(query),
                Encoding.UTF8,
                "application/json"
            );
            string relativePath = $"TimeSeriesData/CO2";

            HttpResponseMessage response;

            response = await saraApi.CallApiForAppAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Post.Method;
                    options.RelativePath = relativePath;
                },
                jsonContent
            );

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var inspectionData = await response.Content.ReadFromJsonAsync<double>();
                return inspectionData;
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                throw new InspectionNotFoundException(
                    $"Could not find inspection data for the given query parameters - Facility: {query.Facility}, TaskStartTime: {query.TaskStartTime}, TaskEndTime: {query.TaskEndTime}, InspectionName: {query.InspectionName}"
                );
            }

            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                throw new InspectionNotFoundException(
                    "Internal server error when trying to get inspection data"
                );
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new InspectionNotFoundException(
                    $"Could not find inspection data for inspection with name {query.InspectionName}"
                );
            }

            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                throw new InspectionNotFoundException("Anonymization workflow failed");
            }

            throw new InspectionNotFoundException(
                "Unexpected error when trying to get inspection data"
            );
        }
    }
}
