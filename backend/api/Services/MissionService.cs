using Api.Database.Context;
using Api.Database.Models;
using Api.Controllers.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api.Services
{
    public interface IMissionService
    {
        public abstract Task<Mission> Create(ScheduledMissionQuery scheduledMissionQuery);

        public abstract Task<IList<Mission>> ReadAll(
            string? assetCode = null,
            MissionStatus? status = null
        );

        public abstract Task<Mission?> ReadById(string id);

        public abstract Task<Mission?> ReadByIsarMissionId(string isarMissionId);

        public abstract Task<Mission> Update(Mission mission);

        public abstract Task<bool> UpdateMissionStatusByIsarMissionId(
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
        private readonly IMapService _mapService;
        private readonly IRobotService _robotService;
        private readonly IEchoService _echoService;
        private readonly IStidService _stidService;

        public MissionService(FlotillaDbContext context, ILogger<MissionService> logger, IMapService mapService, IRobotService robotService, IEchoService echoService, IStidService stidService)
        {
            _context = context;
            _logger = logger;
            _robotService = robotService;
            _mapService = mapService;
            _echoService = echoService;
            _stidService = stidService;

        }

        private IQueryable<Mission> GetMissionsWithSubModels()
        {
            return _context.Missions
                .Include(mission => mission.Robot)
                .ThenInclude(robot => robot.VideoStreams)
                .Include(mission => mission.PlannedTasks)
                .ThenInclude(planTask => planTask.Inspections)
                .Include(mission => mission.Tasks)
                .ThenInclude(task => task.Steps);
        }

        public async Task<Mission> Create(ScheduledMissionQuery scheduledMissionQuery)
        {
            var robot = await _robotService.ReadById(scheduledMissionQuery.RobotId);
            if (robot is null)
                throw new KeyNotFoundException($"Could not find robot with id {scheduledMissionQuery.RobotId}");

            EchoMission? echoMission;
            try
            {
                echoMission = await _echoService.GetMissionById(scheduledMissionQuery.EchoMissionId);
            }
            catch (HttpRequestException e)
            {
                if (e.StatusCode.HasValue && (int)e.StatusCode.Value == 404)
                {
                    _logger.LogWarning(
                        "Could not find echo mission with id={id}",
                        scheduledMissionQuery.EchoMissionId
                    );
                    throw new KeyNotFoundException("Echo mission not found");
                }

                _logger.LogError(e, "Error getting mission from Echo");
                throw e;
            }
            catch (JsonException e)
            {
                string message = "Error deserializing mission from Echo";
                _logger.LogError(e, "{message}", message);
                throw e;
            }

            var plannedTasks = echoMission.Tags.Select(t => new PlannedTask(t)).ToList();
            foreach (PlannedTask task in plannedTasks)
            {
                try
                {
                    task.TagPosition = await _stidService.GetTagPosition(task.TagId);
                }
                catch (JsonException)
                {
                    continue;
                }
            }
            MissionMap map = await _mapService.AssignMapToMission(echoMission.AssetCode, plannedTasks);

            var scheduledMission = new Mission
            {
                Name = echoMission.Name,
                Robot = robot,
                AssetCode = echoMission.AssetCode,
                EchoMissionId = scheduledMissionQuery.EchoMissionId,
                MissionStatus = MissionStatus.Pending,
                Map = map,
                StartTime = scheduledMissionQuery.StartTime,
                PlannedTasks = plannedTasks,
                Tasks = new List<IsarTask>(),
                AssetCode = scheduledMissionQuery.AssetCode
            };

            await _context.Missions.AddAsync(scheduledMission);
            await _context.SaveChangesAsync();

            return scheduledMission;
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
                query = query.Where(mission => mission.MissionStatus.Equals(status));

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
        public async Task<Mission?> ReadByIsarMissionId(string isarMissionId)
        {
            return await GetMissionsWithSubModels()
                .FirstOrDefaultAsync(mission => mission.IsarMissionId.Equals(isarMissionId));
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

            var task = mission.ReadIsarTaskById(isarTaskId);
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
            string isarMissionId,
            string isarTaskId,
            string isarStepId,
            IsarStep.IsarStepStatus stepStatus
        )
        {
            var mission = await ReadByIsarMissionId(isarMissionId);
            if (mission is null)
            {
                _logger.LogWarning(
                    "Could not update step status for ISAR step with id: {id} in mission with id: {missionId} as the mission was not found",
                    isarStepId,
                    isarMissionId
                );
                return false;
            }

            var task = mission.ReadIsarTaskById(isarTaskId);
            if (task is null)
            {
                _logger.LogWarning(
                    "Could not update step status for ISAR step with id: {id} as the task with id: {taskId} was not found",
                    isarStepId,
                    isarTaskId
                );
                return false;
            }

            var step = task.ReadIsarStepById(isarStepId);
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
        #endregion ISAR Specific methods
    }
}
