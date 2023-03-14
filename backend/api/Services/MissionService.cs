using Api.Database.Context;
using Api.Database.Models;
using Api.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IMissionService
    {
        public abstract Task<Mission> Create(Mission mission);

        public abstract Task<IList<Mission>> ReadAll(
            string? assetCode = null,
            MissionStatus? status = null
        );

        public abstract Task<Mission?> ReadById(string id);

        public abstract Task<Mission> Update(Mission mission);

        public abstract Task<Mission?> UpdateMissionStatusByIsarMissionId(
            string isarMissionId,
            MissionStatus missionStatus
        );

        public abstract Task<bool> UpdateTaskStatusByIsarTaskId(
            string isarMissionId,
            string isarTaskId,
            IsarTaskStatus taskStatus
        );

        public abstract Task<bool> UpdateStepStatusByIsarStepId(
            string isarMissionId,
            string isarTaskId,
            string isarStepId,
            IsarStepStatus stepStatus
        );

        public abstract Task<Mission?> Delete(string id);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class MissionService : IMissionService
    {
        private readonly FlotillaDbContext _context;
        private readonly ILogger<MissionService> _logger;

        public MissionService(FlotillaDbContext context, ILogger<MissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private IQueryable<Mission> GetMissionsWithSubModels()
        {
            return _context.Missions
                .Include(mission => mission.Robot)
                .ThenInclude(robot => robot.VideoStreams)
                .Include(mission => mission.Tasks)
                .ThenInclude(planTask => planTask.Inspections)
                .Include(mission => mission.Tasks)
                .ThenInclude(task => task.Inspections);
        }

        public async Task<Mission> Create(Mission mission)
        {
            await _context.Missions.AddAsync(mission);
            await _context.SaveChangesAsync();

            return mission;
        }

        public async Task<IList<Mission>> ReadAll(
            string? assetCode = null,
            MissionStatus? status = null
        )
        {
            var query = GetMissionsWithSubModels();

            if (assetCode is not null)
                query = query.Where(mission => mission.AssetCode.Equals(assetCode));

            if (status is not null)
                query = query.Where(mission => mission.Status.Equals(status));

            return await query.ToListAsync();
        }

        public async Task<Mission?> ReadById(string id)
        {
            return await GetMissionsWithSubModels()
                .FirstOrDefaultAsync(mission => mission.Id.Equals(id));
        }

        public async Task<Mission> Update(Mission mission)
        {
            var entry = _context.Update(mission);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<Mission?> Delete(string id)
        {
            var mission = await GetMissionsWithSubModels()
                .FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (mission is null)
            {
                return null;
            }
            _context.Missions.Remove(mission);
            await _context.SaveChangesAsync();

            return mission;
        }

        #region ISAR Specific methods
        private async Task<Mission?> ReadByIsarMissionId(string isarMissionId)
        {
            return await GetMissionsWithSubModels()
                .FirstOrDefaultAsync(mission => mission.IsarMissionId.Equals(isarMissionId));
        }

        public async Task<Mission?> UpdateMissionStatusByIsarMissionId(
            string isarMissionId,
            MissionStatus missionStatus
        )
        {
            var mission = await ReadByIsarMissionId(isarMissionId);
            if (mission is null)
            {
                _logger.LogWarning(
                    "Could not update mission status for ISAR mission with id: {id} as the mission was not found",
                    isarMissionId
                );
                return null;
            }

            mission.Status = missionStatus;

            await _context.SaveChangesAsync();

            return mission;
        }

        public async Task<bool> UpdateTaskStatusByIsarTaskId(
            string isarMissionId,
            string isarTaskId,
            IsarTaskStatus taskStatus
        )
        {
            var mission = await ReadByIsarMissionId(isarMissionId);
            if (mission is null)
            {
                _logger.LogWarning(
                    "Could not update task status for ISAR task with id: {id} in mission with id: {missionId} as the mission was not found",
                    isarTaskId,
                    isarMissionId
                );
                return false;
            }

            var task = mission.GetTaskByIsarId(isarTaskId);
            if (task is null)
            {
                _logger.LogWarning(
                    "Could not update task status for ISAR task with id: {id} as the task was not found",
                    isarTaskId
                );
                return false;
            }

            task.UpdateStatus(taskStatus);

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateStepStatusByIsarStepId(
            string isarMissionId,
            string isarTaskId,
            string isarStepId,
            IsarStepStatus stepStatus
        )
        {
            var mission = await ReadByIsarMissionId(isarMissionId);
            if (mission is null)
            {
                _logger.LogWarning(
                    "Could not update step status for ISAR inspection with id: {id} in mission with id: {missionId} as the mission was not found",
                    isarStepId,
                    isarMissionId
                );
                return false;
            }

            var task = mission.GetTaskByIsarId(isarTaskId);
            if (task is null)
            {
                _logger.LogWarning(
                    "Could not update step status for ISAR inspection with id: {id} as the task with id: {taskId} was not found",
                    isarStepId,
                    isarTaskId
                );
                return false;
            }

            var inspection = task.GetInspectionByIsarStepId(isarStepId);
            if (inspection is null)
            {
                _logger.LogWarning(
                    "Could not update step status for ISAR inspection with id: {id} as the step was not found",
                    isarStepId
                );
                return false;
            }

            inspection.UpdateStatus(stepStatus);

            await _context.SaveChangesAsync();

            return true;
        }
        #endregion ISAR Specific methods
    }
}
