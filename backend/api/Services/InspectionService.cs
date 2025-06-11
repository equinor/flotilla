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
            var inspectionData = await GetInspectionStorageInfo(isarInspectionId);

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
                string errorMessage =
                    $"Inspection with task ID {isarTaskId} could not be found when trying to update status to {isarTaskStatus}.";
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
            var installation = missionRun?.InspectionArea.Installation;

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
                throw new InspectionNotFoundException(
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

        public void DetachTracking(FlotillaDbContext context, Inspection inspection)
        {
            context.Entry(inspection).State = EntityState.Detached;
        }
    }
}
