using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Abstractions;

namespace Api.Services
{
    public interface IInspectionService
    {
        public Task<byte[]?> FetchInspectionImageFromIsarInspectionId(string isarInspectionId);
        public Task<Inspection> UpdateInspectionStatus(
            string isarTaskId,
            IsarTaskStatus isarTaskStatus
        );
        public Task<Inspection?> ReadByIsarTaskId(string id, bool readOnly = true);
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
            var inspectionData =
                await GetInspectionStorageInfo(isarInspectionId)
                ?? throw new InspectionNotFoundException(
                    $"Could not find inspection data for inspection with ISAR Inspection Id {isarInspectionId}."
                );
            return await blobService.DownloadBlob(
                inspectionData.BlobName,
                inspectionData.BlobContainer,
                inspectionData.StorageAccount
            );
        }

        public async Task<Inspection> UpdateInspectionStatus(
            string isarTaskId,
            IsarTaskStatus isarTaskStatus
        )
        {
            var inspection = await ReadByIsarTaskId(isarTaskId, readOnly: true);
            if (inspection is null)
            {
                string errorMessage = $"Inspection with task ID {isarTaskId} could not be found";
                logger.LogError("{Message}", errorMessage);
                throw new InspectionNotFoundException(errorMessage);
            }

            inspection.UpdateStatus(isarTaskStatus);
            await Update(inspection);
            return inspection;
        }

        private async Task ApplyDatabaseUpdate(Installation? installation)
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes();
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
            var installation = missionRun?.InspectionArea?.Installation;

            await ApplyDatabaseUpdate(installation);
            DetachTracking(context, inspection);
        }

        public async Task<Inspection?> ReadByIsarTaskId(string id, bool readOnly = true)
        {
            return await GetInspections(readOnly: readOnly)
                .FirstOrDefaultAsync(inspection =>
                    inspection.IsarTaskId != null && inspection.IsarTaskId.Equals(id)
                );
        }

        private IQueryable<Inspection> GetInspections(bool readOnly = true)
        {
            if (accessRoleService.IsUserAdmin() || !accessRoleService.IsAuthenticationAvailable())
                return (
                    readOnly ? context.Inspections.AsNoTracking() : context.Inspections.AsTracking()
                );
            throw new UnauthorizedAccessException(
                "User does not have permission to view inspections"
            );
        }

        private async Task<SaraInspectionDataResponse?> GetInspectionStorageInfo(
            string inspectionId
        )
        {
            string relativePath = $"PlantData/{inspectionId}/inspection-data-storage-location";

            HttpResponseMessage response;
            try
            {
                response = await saraApi.CallApiForAppAsync(
                    ServiceName,
                    options =>
                    {
                        options.HttpMethod = HttpMethod.Get.Method;
                        options.RelativePath = relativePath;
                    }
                );
            }
            catch (Exception e)
            {
                logger.LogError(e, "{ErrorMessage}", e.Message);
                return null;
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var inspectionData =
                    await response.Content.ReadFromJsonAsync<SaraInspectionDataResponse>()
                    ?? throw new JsonException("Failed to deserialize inspection data from SARA.");
                return inspectionData;
            }

            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                logger.LogInformation(
                    "Inspection data storage location for inspection with Id {inspectionId} is not yet available",
                    inspectionId
                );
                return null;
            }

            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                logger.LogError(
                    "Inetrnal server error when trying to get inspection data for inspection with Id {inspectionId}",
                    inspectionId
                );
                return null;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogError(
                    "Could not find inspection data for inspection with Id {inspectionId}",
                    inspectionId
                );
                return null;
            }

            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                logger.LogError(
                    "Anonymization workflow failed for inspection with Id {inspectionId}",
                    inspectionId
                );
                return null;
            }

            logger.LogError(
                "Unexpected error when trying to get inspection data for inspection with Id {inspectionId}",
                inspectionId
            );
            return null;
        }

        public void DetachTracking(FlotillaDbContext context, Inspection inspection)
        {
            context.Entry(inspection).State = EntityState.Detached;
        }
    }
}
