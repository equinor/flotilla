using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services.MissionLoaders;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Abstractions;

namespace Api.Services
{
    public interface IInspectionService
    {
        public Task<byte[]?> FetchInspectionImageFromIsarInspectionId(string isarInspectionId);
        public Task<byte[]?> FetchAnalysisFromIsarInspectionId(string isarInspectionId);
        public Task<Inspection> UpdateInspectionAnalysisResults(
            string inspectionId,
            AnalysisResult analysisResult
        );
        public Task<Inspection?> ReadByInspectionId(string id, bool readOnly = true);
        public Task<Inspection?> ReadByIsarInspectionId(string id, bool readOnly = true);
        public Task<TagInspectionMetadata> CreateOrUpdateTagInspectionMetadata(
            TagInspectionMetadata metadata
        );
        public Task<IsarZoomDescription?> FindInspectionZoom(EchoTag echoTag);
        public string GetInspectionName(
            string installationCode,
            Position position,
            string? tag,
            string robotName,
            string? inspectionDescription
        );
        public Task<double> FetchCO2ConcentrationFromQuery(FetchCO2Query query);
    }

    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class InspectionService(
        FlotillaDbContext context,
        ILogger<InspectionService> logger,
        IDownstreamApi saraApi,
        IAccessRoleService accessRoleService,
        IBlobService blobService
    ) : IInspectionService
    {
        public const string ServiceName = "SARA";

        public async Task<byte[]?> FetchInspectionImageFromIsarInspectionId(string isarInspectionId)
        {
            var inspectionData = await GetInspectionStorageInfo(isarInspectionId);

            return await blobService.DownloadBlob(
                inspectionData.BlobName,
                inspectionData.BlobContainer,
                inspectionData.StorageAccount
            );
        }

        public async Task<byte[]?> FetchAnalysisFromIsarInspectionId(string isarInspectionId)
        {
            var inspectionData = await GetAnalysisStorageInfo(isarInspectionId);
            if (inspectionData == null)
            {
                return null;
            }
            return await blobService.DownloadBlob(
                inspectionData.BlobName,
                inspectionData.BlobContainer,
                inspectionData.StorageAccount
            );
        }

        private async Task<SaraAnalysisDataResponse?> GetAnalysisStorageInfo(
            string isarInspectionId
        )
        {
            var inspection = await ReadByIsarInspectionId(isarInspectionId, readOnly: true);
            if (inspection is null)
            {
                string errorMessage =
                    $"Inspection with task ID {isarInspectionId} could not be found when trying to update analysis result.";
                logger.LogError("{Message}", errorMessage);
                throw new InspectionNotFoundException(errorMessage);
            }

            var existingAnalysisResult = context
                .AnalysisResults.Where(a => a.InspectionId == inspection.Id)
                .FirstOrDefault();

            if (existingAnalysisResult == null)
            {
                string errorMessage =
                    $"Analysis result for inspection with isar Id {isarInspectionId} could not be found when trying to fetch analysis data.";
                logger.LogError("{Message}", errorMessage);
                throw new InspectionNotFoundException(errorMessage);
            }
            if (
                existingAnalysisResult.StorageAccount == null
                || existingAnalysisResult.BlobContainer == null
                || existingAnalysisResult.BlobName == null
            )
            {
                return null;
            }
            return new SaraAnalysisDataResponse
            {
                StorageAccount = existingAnalysisResult.StorageAccount,
                BlobContainer = existingAnalysisResult.BlobContainer,
                BlobName = existingAnalysisResult.BlobName,
            };
        }

        public async Task<Inspection> UpdateInspectionAnalysisResults(
            string inspectionId,
            AnalysisResult analysisResult
        )
        {
            var inspection = await ReadByInspectionId(inspectionId, readOnly: true);
            if (inspection is null)
            {
                string errorMessage =
                    $"Inspection with task ID {inspectionId} could not be found when trying to update analysis result.";
                logger.LogError("{Message}", errorMessage);
                throw new InspectionNotFoundException(errorMessage);
            }

            var existingAnalysisResult = context
                .AnalysisResults.Where(a => a.InspectionId == inspectionId)
                .FirstOrDefault();

            if (
                existingAnalysisResult != null
                && inspection.AnalysisResult.InspectionId == existingAnalysisResult.InspectionId
            )
            {
                context.AnalysisResults.Add(analysisResult);
                inspection.AnalysisResult = analysisResult;
                await Update(inspection);
                context.AnalysisResults.Remove(existingAnalysisResult);
                return inspection;
            }

            context.AnalysisResults.Add(analysisResult);
            inspection.AnalysisResult = analysisResult;
            await Update(inspection);
            return inspection;
        }

        private async Task ApplyDatabaseUpdate(Installation? installation)
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes(
                AccessMode.Write
            );
            if (
                installation == null
                || accessibleInstallationCodes.Contains(
                    installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)
                )
            )
                await context.SaveChangesAsync();
            else
                throw new UnauthorizedAccessException(
                    $"User does not have permission to update area in installation {installation.Name}"
                );
        }

        private async Task Update(Inspection inspection)
        {
            var entry = context.Update(inspection);

            var missionRun = await context
                .MissionRuns.Include(missionRun => missionRun.InspectionArea)
                    .ThenInclude(area => area != null ? area.Installation : null)
                .Include(missionRun => missionRun.Robot)
                .Where(missionRun =>
                    missionRun.Tasks.Any(missionTask =>
                        missionTask.Inspection != null && missionTask.Inspection.Id == inspection.Id
                    )
                )
                .AsNoTracking()
                .FirstOrDefaultAsync();
            var installation = missionRun?.InspectionArea.Installation;

            await ApplyDatabaseUpdate(installation);
            DetachTracking(context, inspection);
        }

        public async Task<Inspection?> ReadByInspectionId(string id, bool readOnly = true)
        {
            return await GetInspections(readOnly: readOnly)
                .FirstOrDefaultAsync(inspection => inspection.Id.Equals(id));
        }

        private IQueryable<Inspection> GetInspections(bool readOnly = true)
        {
            var query = context.Inspections.Include(i => i.AnalysisResult);
            if (accessRoleService.IsUserAdmin() || !accessRoleService.IsAuthenticationAvailable())
                return (readOnly ? query.AsNoTracking() : query.AsTracking());
            throw new UnauthorizedAccessException(
                "User does not have permission to view inspections"
            );
        }

        public async Task<Inspection?> ReadByIsarInspectionId(string id, bool readOnly = true)
        {
            return await GetInspections(readOnly: readOnly)
                .FirstOrDefaultAsync(inspection => inspection.IsarInspectionId.Equals(id));
        }

        private async Task<SaraInspectionDataResponse> GetInspectionStorageInfo(string inspectionId)
        {
            string relativePath = $"PlantData/{inspectionId}/inspection-data-storage-location";

            HttpResponseMessage response;

            response = await saraApi.CallApiForAppAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get.Method;
                    options.RelativePath = relativePath;
                }
            );

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var inspectionData =
                    await response.Content.ReadFromJsonAsync<SaraInspectionDataResponse>()
                    ?? throw new JsonException("Failed to deserialize inspection data from SARA.");
                return inspectionData;
            }

            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                throw new InspectionNotAvailableYetException(
                    "Inspection data storage location is not yet available."
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
                throw new InspectionNotFoundException("Could not find inspection data");
            }

            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                throw new InspectionNotFoundException("Anonymization workflow failed");
            }

            throw new InspectionNotFoundException(
                "Unexpected error when trying to get inspection data"
            );
        }

        public async Task<TagInspectionMetadata> CreateOrUpdateTagInspectionMetadata(
            TagInspectionMetadata metadata
        )
        {
            var existingMetadata = await context
                .TagInspectionMetadata.Where(e => e.TagId == metadata.TagId)
                .FirstOrDefaultAsync();
            if (existingMetadata == null)
            {
                await context.TagInspectionMetadata.AddAsync(metadata);
            }
            else
            {
                existingMetadata.ZoomDescription = metadata.ZoomDescription;
                context.TagInspectionMetadata.Update(existingMetadata);
            }

            await context.SaveChangesAsync();
            return metadata;
        }

        public async Task<IsarZoomDescription?> FindInspectionZoom(EchoTag echoTag)
        {
            return (
                await context
                    .TagInspectionMetadata.Where(e => e.TagId == echoTag.TagId)
                    .FirstOrDefaultAsync()
            )?.ZoomDescription;
        }

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

        public void DetachTracking(FlotillaDbContext context, Inspection inspection)
        {
            context.Entry(inspection).State = EntityState.Detached;
        }
    }
}
