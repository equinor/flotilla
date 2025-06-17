using System.Diagnostics.CodeAnalysis;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IMissionTaskService
    {
        public Task<MissionTask> UpdateMissionTaskStatus(
            string taskId,
            IsarTaskStatus isarTaskStatus
        );

        public void DetachTracking(FlotillaDbContext context, MissionTask missionTask);
    }

    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class MissionTaskService(FlotillaDbContext context, ILogger<MissionTaskService> logger)
        : IMissionTaskService
    {
        public async Task<MissionTask> UpdateMissionTaskStatus(
            string taskId,
            IsarTaskStatus isarTaskStatus
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
