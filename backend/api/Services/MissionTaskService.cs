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
        public Task<MissionTask> UpdateMissionTaskStatus(string isarTaskId, IsarTaskStatus isarTaskStatus);

        public void DetachTracking(MissionTask missionTask);
    }

    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class MissionTaskService(FlotillaDbContext context, ILogger<MissionTaskService> logger) : IMissionTaskService
    {
        public async Task<MissionTask> UpdateMissionTaskStatus(string isarTaskId, IsarTaskStatus isarTaskStatus)
        {
            var missionTask = await ReadByIsarTaskId(isarTaskId);
            if (missionTask is null)
            {
                string errorMessage = $"Inspection with ID {isarTaskId} could not be found";
                logger.LogError("{Message}", errorMessage);
                throw new MissionTaskNotFoundException(errorMessage);
            }

            missionTask.UpdateStatus(isarTaskStatus);
            return await Update(missionTask);
        }

        private async Task<MissionTask> Update(MissionTask missionTask)
        {
            foreach (var inspection in missionTask.Inspections) { context.Entry(inspection).State = EntityState.Unchanged; }

            var entry = context.Update(missionTask);
            await context.SaveChangesAsync();
            return entry.Entity;
        }

        private async Task<MissionTask?> ReadByIsarTaskId(string id)
        {
            return await GetMissionTasks().FirstOrDefaultAsync(missionTask => missionTask.IsarTaskId != null && missionTask.IsarTaskId.Equals(id));
        }

        private IQueryable<MissionTask> GetMissionTasks()
        {
            return context.MissionTasks.Include(missionTask => missionTask.Inspections).ThenInclude(inspection => inspection.InspectionFindings);
        }

        public void DetachTracking(MissionTask missionTask)
        {
            context.Entry(missionTask).State = EntityState.Detached;
        }
    }
}
