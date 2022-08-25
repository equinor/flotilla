using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IMissionService
    {
        public abstract Task<Mission> Create(Mission mission);

        public abstract Task<Mission> Create(
            string isarMissionId,
            int echoMissionId,
            string log,
            MissionStatus status,
            Robot robot
        );

        public abstract Task<IList<Mission>> ReadAll(
            string? assetCode = null,
            MissionStatus? status = null
        );

        public abstract Task<Mission?> ReadById(string id);

        public abstract Task<Mission?> ReadByIsarMissionId(string isarMissionId);

        public abstract Task<IsarTask?> ReadIsarTaskById(string isarTaskId);

        public abstract Task<IsarStep?> ReadIsarStepById(string isarStepId);

        public abstract Task<Mission> Update(Mission mission);

        public abstract Task<bool> UpdateMissionStatusByIsarMissionId(
            string isarMissionId,
            MissionStatus missionStatus
        );

        public abstract Task<bool> UpdateTaskStatusByIsarTaskId(
            string isarTaskId,
            IsarTaskStatus taskStatus
        );

        public abstract Task<bool> UpdateStepStatusByIsarStepId(
            string isarStepId,
            IsarStep.IsarStepStatus stepStatus
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

        public async Task<Mission> Create(Mission mission)
        {
            await _context.Missions.AddAsync(mission);
            await _context.SaveChangesAsync();

            return mission;
        }

        public async Task<Mission> Create(
            string isarMissionId,
            int echoMissionId,
            string log,
            MissionStatus status,
            Robot robot
        )
        {
            var mission = new Mission
            {
                IsarMissionId = isarMissionId,
                EchoMissionId = echoMissionId,
                MissionStatus = status,
                StartTime = DateTimeOffset.UtcNow,
                Robot = robot
            };
            await Create(mission);

            return mission;
        }

        public async Task<IList<Mission>> ReadAll(
            string? assetCode = null,
            MissionStatus? status = null
        )
        {
            IQueryable<Mission> query = _context.Missions
                .Include(r => r.Robot)
                .Include(mission => mission.Tasks)
                .ThenInclude(task => task.Steps);

            if (assetCode is not null)
                query = query.Where(mission => mission.AssetCode.Equals(assetCode));

            if (status is not null)
                query = query.Where(mission => mission.MissionStatus.Equals(status));

            return await query.ToListAsync();
        }

        public async Task<Mission?> ReadById(string id)
        {
            return await _context.Missions
                .Include(r => r.Robot)
                .Include(mission => mission.Tasks)
                .ThenInclude(task => task.Steps)
                .FirstOrDefaultAsync(mission => mission.Id.Equals(id));
        }

        public async Task<Mission?> ReadByIsarMissionId(string isarMissionId)
        {
            return await _context.Missions
                .Include(r => r.Robot)
                .FirstOrDefaultAsync(mission => mission.IsarMissionId.Equals(isarMissionId));
        }

        public async Task<IsarTask?> ReadIsarTaskById(string isarTaskId)
        {
            return await _context.Tasks.FirstOrDefaultAsync(
                task => task.IsarTaskId.Equals(isarTaskId)
            );
        }

        public async Task<IsarStep?> ReadIsarStepById(string isarStepId)
        {
            return await _context.Steps.FirstOrDefaultAsync(
                step => step.IsarStepId.Equals(isarStepId)
            );
        }

        public async Task<bool> UpdateMissionStatusByIsarMissionId(
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
                return false;
            }

            mission.MissionStatus = missionStatus;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateTaskStatusByIsarTaskId(
            string isarTaskId,
            IsarTaskStatus taskStatus
        )
        {
            var task = await ReadIsarTaskById(isarTaskId);
            if (task is null)
            {
                _logger.LogWarning(
                    "Could not update task status for ISAR task with id: {id} as the task was not found",
                    isarTaskId
                );
                return false;
            }

            task.TaskStatus = taskStatus;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateStepStatusByIsarStepId(
            string isarStepId,
            IsarStep.IsarStepStatus stepStatus
        )
        {
            var step = await ReadIsarStepById(isarStepId);
            if (step is null)
            {
                _logger.LogWarning(
                    "Could not update step status for ISAR step with id: {id} as the step was not found",
                    isarStepId
                );
                return false;
            }

            step.StepStatus = stepStatus;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<Mission?> Delete(string id)
        {
            var mission = await _context.Missions.FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (mission is null)
            {
                return null;
            }
            _context.Missions.Remove(mission);
            await _context.SaveChangesAsync();

            return mission;
        }

        public async Task<Mission> Update(Mission mission)
        {
            var entry = _context.Update(mission);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }
    }
}
