using System.Diagnostics.CodeAnalysis;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IMissionTaskService
    {
        public Task<PagedList<MissionTask>> ReadAll(MissionTaskQueryStringParameters parameters);

        public Task<MissionTask> UpdateMissionTaskStatus(
            string taskId,
            IsarTaskStatus isarTaskStatus,
            string? errorDescription = null
        );

        public void DetachTracking(FlotillaDbContext context, MissionTask missionTask);
    }

    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class MissionTaskService(
        FlotillaDbContext context,
        IAccessRoleService accessRoleService,
        ILogger<MissionTaskService> logger
    ) : IMissionTaskService
    {
        public async Task<PagedList<MissionTask>> ReadAll(
            MissionTaskQueryStringParameters parameters
        )
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes(
                AccessMode.Read
            );

            var minStartTime = DateTimeOffset
                .FromUnixTimeSeconds(parameters.MinStartTime)
                .UtcDateTime;
            var maxStartTime = DateTimeOffset
                .FromUnixTimeSeconds(parameters.MaxStartTime)
                .UtcDateTime;
            var minEndTime = DateTimeOffset.FromUnixTimeSeconds(parameters.MinEndTime).UtcDateTime;
            var maxEndTime = DateTimeOffset.FromUnixTimeSeconds(parameters.MaxEndTime).UtcDateTime;

            var missionRunQuery = context.MissionRuns.Where(r =>
                accessibleInstallationCodes.Contains(r.InstallationCode.ToUpper())
            );

            if (!string.IsNullOrWhiteSpace(parameters.InstallationCode))
                missionRunQuery = missionRunQuery.Where(r =>
                    r.InstallationCode.ToUpper() == parameters.InstallationCode.ToUpper().Trim()
                );

            if (!string.IsNullOrWhiteSpace(parameters.MissionRunId))
                missionRunQuery = missionRunQuery.Where(r => r.Id == parameters.MissionRunId);

            var taskQuery = missionRunQuery
                .SelectMany(r => r.Tasks)
                .Include(t => t.Inspection)
                .AsQueryable();

            if (parameters.Statuses is { Count: > 0 })
                taskQuery = taskQuery.Where(t => parameters.Statuses.Contains(t.Status));

            if (parameters.AnalysisTypes is { Count: > 0 })
                taskQuery = taskQuery.Where(t =>
                    t.AnalysisTypes != null
                    && t.AnalysisTypes.Any(a => parameters.AnalysisTypes.Contains(a))
                );

            if (parameters.InspectionTypes is { Count: > 0 })
                taskQuery = taskQuery.Where(t =>
                    t.Inspection != null
                    && parameters.InspectionTypes.Contains(t.Inspection.InspectionType)
                );

            if (!string.IsNullOrWhiteSpace(parameters.TagSearch))
                taskQuery = taskQuery.Where(t =>
                    t.TagId != null
                    && t.TagId.ToLower().Contains(parameters.TagSearch.ToLower().Trim())
                );

            taskQuery = taskQuery
                .Where(t => t.StartTime == null || t.StartTime >= minStartTime)
                .Where(t => t.StartTime == null || t.StartTime <= maxStartTime)
                .Where(t => t.EndTime == null || t.EndTime >= minEndTime)
                .Where(t => t.EndTime == null || t.EndTime <= maxEndTime);

            taskQuery = taskQuery.OrderBy(t => t.TaskOrder);

            taskQuery = taskQuery.AsNoTracking(); // Makes the query read-only

            return await PagedList<MissionTask>.ToPagedListAsync(
                taskQuery,
                parameters.PageNumber,
                parameters.PageSize
            );
        }

        public async Task<MissionTask> UpdateMissionTaskStatus(
            string taskId,
            IsarTaskStatus isarTaskStatus,
            string? errorDescription = null
        )
        {
            var missionTask = await ReadByTaskId(taskId, readOnly: true);
            if (missionTask is null)
            {
                string errorMessage = $"Inspection with ID {taskId} could not be found";
                logger.LogError("{Message}", errorMessage);
                throw new MissionTaskNotFoundException(errorMessage);
            }

            missionTask.UpdateStatus(isarTaskStatus);

            if (isarTaskStatus == IsarTaskStatus.Failed && errorDescription != null)
            {
                missionTask.ErrorDescription = errorDescription;
            }
            return await Update(missionTask);
        }

        private async Task<MissionTask> Update(MissionTask missionTask)
        {
            if (missionTask.Inspection != null)
                context.Entry(missionTask.Inspection).State = EntityState.Unchanged;

            var entry = context.Update(missionTask);
            await context.SaveChangesAsync();
            DetachTracking(context, missionTask);
            return entry.Entity;
        }

        private async Task<MissionTask?> ReadByTaskId(string id, bool readOnly = true)
        {
            return await GetMissionTasks(readOnly: readOnly)
                .FirstOrDefaultAsync(missionTask => missionTask.Id.Equals(id));
        }

        private IQueryable<MissionTask> GetMissionTasks(bool readOnly = true)
        {
            return (
                readOnly ? context.MissionTasks.AsNoTracking() : context.MissionTasks.AsTracking()
            ).Include(missionTask => missionTask.Inspection);
        }

        public void DetachTracking(FlotillaDbContext context, MissionTask missionTask)
        {
            context.Entry(missionTask).State = EntityState.Detached;
            if (missionTask.Inspection != null)
                context.Entry(missionTask.Inspection).State = EntityState.Detached;
        }
    }
}
