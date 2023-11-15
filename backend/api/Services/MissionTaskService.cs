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
    }

    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class MissionTaskService : IMissionTaskService
    {
        private readonly FlotillaDbContext _context;
        private readonly ILogger<MissionTaskService> _logger;

        public MissionTaskService(FlotillaDbContext context, ILogger<MissionTaskService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MissionTask> UpdateMissionTaskStatus(string isarTaskId, IsarTaskStatus isarTaskStatus)
        {
            var missionTask = await ReadByIsarTaskId(isarTaskId);
            if (missionTask is null)
            {
                string errorMessage = $"Inspection with ID {isarTaskId} could not be found";
                _logger.LogError("{Message}", errorMessage);
                throw new MissionTaskNotFoundException(errorMessage);
            }

            missionTask.UpdateStatus(isarTaskStatus);
            return await Update(missionTask);
        }

        private async Task<MissionTask> Update(MissionTask missionTask)
        {
            foreach (var inspection in missionTask.Inspections) { _context.Entry(inspection).State = EntityState.Unchanged; }

            var entry = _context.Update(missionTask);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        private async Task<MissionTask?> ReadByIsarTaskId(string id)
        {
            return await GetMissionTasks().FirstOrDefaultAsync(missionTask => missionTask.IsarTaskId != null && missionTask.IsarTaskId.Equals(id));
        }

        private IQueryable<MissionTask> GetMissionTasks()
        {
            return _context.MissionTasks.Include(missionTask => missionTask.Inspections).ThenInclude(inspection => inspection);
        }
    }
}
