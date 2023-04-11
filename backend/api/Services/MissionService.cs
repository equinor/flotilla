using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Api.Database.Models.TaskStatus;

namespace Api.Services
{
    public interface IMissionService
    {
        public abstract Task<Mission> Create(Mission mission);

        public abstract Task<PagedList<Mission>> ReadAll(MissionQueryStringParameters parameters);

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
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1304:Specify CultureInfo",
        Justification = "Entity framework does not support translating culture info to SQL calls"
    )]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1307:Specify CultureInfo",
        Justification = "Entity framework does not support translating culture info to SQL calls"
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

        public async Task<PagedList<Mission>> ReadAll(MissionQueryStringParameters parameters)
        {
            var query = GetMissionsWithSubModels();
            var filter = ConstructFilter(parameters);

            query = query.Where(filter);

            SearchByName(ref query, parameters.NameSearch);
            SearchByRobotName(ref query, parameters.RobotNameSearch);
            SearchByTag(ref query, parameters.TagSearch);

            ApplySort(ref query, parameters.OrderBy);

            return await PagedList<Mission>.ToPagedListAsync(
                query,
                parameters.PageNumber,
                parameters.PageSize
            );
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
                .FirstOrDefaultAsync(
                    mission =>
                        mission.IsarMissionId != null && mission.IsarMissionId.Equals(isarMissionId)
                );
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
            if (taskStatus == IsarTaskStatus.InProgress && mission.Status != MissionStatus.Ongoing)
            {
                // If mission was set to failed and then ISAR recovered connection, we need to reset the coming tasks
                mission.Status = MissionStatus.Ongoing;
                foreach (
                    var taskItem in mission.Tasks.Where(
                        taskItem => taskItem.TaskOrder > task.TaskOrder
                    )
                )
                {
                    taskItem.Status = TaskStatus.NotStarted;
                    foreach (var inspection in taskItem.Inspections)
                    {
                        inspection.Status = InspectionStatus.NotStarted;
                    }
                }
            }

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

        private static void SearchByName(ref IQueryable<Mission> missions, string? name)
        {
            if (!missions.Any() || string.IsNullOrWhiteSpace(name))
                return;

            missions = missions.Where(
                mission =>
                    mission.Name != null && mission.Name.ToLower().Contains(name.Trim().ToLower())
            );
        }

        private static void SearchByRobotName(ref IQueryable<Mission> missions, string? robotName)
        {
            if (!missions.Any() || string.IsNullOrWhiteSpace(robotName))
                return;

            missions = missions.Where(
                mission => mission.Robot.Name.ToLower().Contains(robotName.Trim().ToLower())
            );
        }

        private static void SearchByTag(ref IQueryable<Mission> missions, string? tag)
        {
            if (!missions.Any() || string.IsNullOrWhiteSpace(tag))
                return;

            missions = missions.Where(
                mission =>
                    mission.Tasks.Any(
                        task =>
                            task.TagId != null
                            && task.TagId.ToLower().Contains(tag.Trim().ToLower())
                    )
            );
        }

        /// <summary>
        /// Filters by <see cref="MissionQueryStringParameters.AssetCode"/> and <see cref="MissionQueryStringParameters.Status"/>
        ///
        /// <para>Uses LINQ Expression trees (see <seealso href="https://docs.microsoft.com/en-us/dotnet/csharp/expression-trees"/>)</para>
        /// </summary>
        /// <param name="parameters"> The variable containing the filter params </param>
        private static Expression<Func<Mission, bool>> ConstructFilter(
            MissionQueryStringParameters parameters
        )
        {
            Expression<Func<Mission, bool>> assetFilter = parameters.AssetCode is null
                ? mission => true
                : mission =>
                      mission.AssetCode.ToLower().Equals(parameters.AssetCode.Trim().ToLower());

            Expression<Func<Mission, bool>> statusFilter = parameters.Status is null
                ? mission => true
                : mission => mission.Status.Equals(parameters.Status);

            Expression<Func<Mission, bool>> robotIdFilter = parameters.RobotId is null
                ? mission => true
                : mission => mission.Robot.Id.Equals(parameters.RobotId);

            var minStartTime = DateTimeOffset.FromUnixTimeSeconds(parameters.MinStartTime);
            var maxStartTime = DateTimeOffset.FromUnixTimeSeconds(parameters.MaxStartTime);
            Expression<Func<Mission, bool>> startTimeFilter = mission =>
                mission.StartTime == null
                || (
                    DateTimeOffset.Compare(mission.StartTime.Value, minStartTime) >= 0
                    && DateTimeOffset.Compare(mission.StartTime.Value, maxStartTime) <= 0
                );

            var minEndTime = DateTimeOffset.FromUnixTimeSeconds(parameters.MinEndTime);
            var maxEndTime = DateTimeOffset.FromUnixTimeSeconds(parameters.MaxEndTime);
            Expression<Func<Mission, bool>> endTimeFilter = mission =>
                mission.EndTime == null
                || (
                    DateTimeOffset.Compare(mission.EndTime.Value, minEndTime) >= 0
                    && DateTimeOffset.Compare(mission.EndTime.Value, maxEndTime) <= 0
                );

            var minDesiredStartTime = DateTimeOffset.FromUnixTimeSeconds(
                parameters.MinDesiredStartTime
            );
            var maxDesiredStartTime = DateTimeOffset.FromUnixTimeSeconds(
                parameters.MaxDesiredStartTime
            );
            Expression<Func<Mission, bool>> desiredStartTimeFilter = mission =>
                DateTimeOffset.Compare(mission.DesiredStartTime, minDesiredStartTime) >= 0
                && DateTimeOffset.Compare(mission.DesiredStartTime, maxDesiredStartTime) <= 0;

            // The parameter of the filter expression
            var mission = Expression.Parameter(typeof(Mission));

            // Combining the body of the filters to create the combined filter, using invoke to force parameter substitution
            Expression body = Expression.AndAlso(
                Expression.Invoke(assetFilter, mission),
                Expression.AndAlso(
                    Expression.Invoke(statusFilter, mission),
                    Expression.AndAlso(
                        Expression.Invoke(robotIdFilter, mission),
                        Expression.AndAlso(
                            Expression.Invoke(desiredStartTimeFilter, mission),
                            Expression.AndAlso(
                                Expression.Invoke(startTimeFilter, mission),
                                Expression.Invoke(endTimeFilter, mission)
                            )
                        )
                    )
                )
            );

            // Constructing the resulting lambda expression by combining parameter and body
            return Expression.Lambda<Func<Mission, bool>>(body, mission);
        }

        private static void ApplySort(ref IQueryable<Mission> missions, string orderByQueryString)
        {
            if (!missions.Any())
                return;

            if (string.IsNullOrWhiteSpace(orderByQueryString))
            {
                missions = missions.OrderBy(x => x.Name);
                return;
            }

            string[] orderParams = orderByQueryString
                .Trim()
                .Split(',')
                .Select(parameterString => parameterString.Trim())
                .ToArray();

            var propertyInfos = typeof(Mission).GetProperties(
                BindingFlags.Public | BindingFlags.Instance
            );
            var orderQueryBuilder = new StringBuilder();

            foreach (string param in orderParams)
            {
                if (string.IsNullOrWhiteSpace(param))
                    continue;

                string propertyFromQueryName = param.Split(" ")[0];
                var objectProperty = propertyInfos.FirstOrDefault(
                    pi =>
                        pi.Name.Equals(
                            propertyFromQueryName,
                            StringComparison.InvariantCultureIgnoreCase
                        )
                );

                if (objectProperty == null)
                    throw new InvalidDataException(
                        $"Mission has no property '{propertyFromQueryName}' for ordering"
                    );

                string sortingOrder = param.EndsWith(" desc", StringComparison.OrdinalIgnoreCase)
                  ? "descending"
                  : "ascending";

                string sortParameter = $"{objectProperty.Name} {sortingOrder}, ";
                orderQueryBuilder.Append(sortParameter);
            }

            string orderQuery = orderQueryBuilder.ToString().TrimEnd(',', ' ');

            missions = string.IsNullOrWhiteSpace(orderQuery)
              ? missions.OrderBy(mission => mission.Name)
              : missions.OrderBy(orderQuery);
        }
    }
}
