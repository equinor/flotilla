using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Api.Database.Models.TaskStatus;
namespace Api.Services
{
    public interface IMissionRunService
    {
        public Task<MissionRun> Create(MissionRun missionRun);

        public Task<PagedList<MissionRun>> ReadAll(MissionRunQueryStringParameters parameters);

        public Task<MissionRun?> ReadById(string id);

        public Task<MissionRun?> ReadNextScheduledRunByMissionId(string missionId);

        public Task<MissionRun> Update(MissionRun mission);

        public Task<MissionRun?> UpdateMissionRunStatusByIsarMissionId(
            string isarMissionId,
            MissionStatus missionStatus
        );

        public Task<bool> UpdateTaskStatusByIsarTaskId(
            string isarMissionId,
            string isarTaskId,
            IsarTaskStatus taskStatus
        );

        public Task<bool> UpdateStepStatusByIsarStepId(
            string isarMissionId,
            string isarTaskId,
            string isarStepId,
            IsarStepStatus stepStatus
        );

        public Task<MissionRun?> Delete(string id);
    }

    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    [SuppressMessage(
        "Globalization",
        "CA1304:Specify CultureInfo",
        Justification = "Entity framework does not support translating culture info to SQL calls"
    )]
    [SuppressMessage(
        "Globalization",
        "CA1307:Specify CultureInfo",
        Justification = "Entity framework does not support translating culture info to SQL calls"
    )]
    public class MissionRunService : IMissionRunService
    {
        private readonly FlotillaDbContext _context;
        private readonly ILogger<MissionRunService> _logger;

        public MissionRunService(FlotillaDbContext context, ILogger<MissionRunService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MissionRun> Create(MissionRun missionRun)
        {
            await _context.MissionRuns.AddAsync(missionRun);
            await _context.SaveChangesAsync();

            return missionRun;
        }

        public async Task<PagedList<MissionRun>> ReadAll(MissionRunQueryStringParameters parameters)
        {
            var query = GetMissionRunsWithSubModels();
            var filter = ConstructFilter(parameters);

            query = query.Where(filter);

            SearchByName(ref query, parameters.NameSearch);
            SearchByRobotName(ref query, parameters.RobotNameSearch);
            SearchByTag(ref query, parameters.TagSearch);

            SortingService.ApplySort(ref query, parameters.OrderBy);

            return await PagedList<MissionRun>.ToPagedListAsync(
                query,
                parameters.PageNumber,
                parameters.PageSize
            );
        }

        public async Task<MissionRun?> ReadById(string id)
        {
            return await GetMissionRunsWithSubModels()
                .FirstOrDefaultAsync(missionRun => missionRun.Id.Equals(id));
        }

        public async Task<MissionRun?> ReadNextScheduledRunByMissionId(string missionId)
        {
            return await GetMissionRunsWithSubModels()
                .Where(m => m.MissionId == missionId && m.EndTime == null)
                .OrderBy(m => m.DesiredStartTime)
                .FirstOrDefaultAsync();
        }

        public async Task<MissionRun> Update(MissionRun missionRun)
        {
            var entry = _context.Update(missionRun);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<MissionRun?> Delete(string id)
        {
            var missionRun = await GetMissionRunsWithSubModels()
                .FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (missionRun is null)
            {
                return null;
            }

            _context.MissionRuns.Remove(missionRun);
            await _context.SaveChangesAsync();

            return missionRun;
        }

        private IQueryable<MissionRun> GetMissionRunsWithSubModels()
        {
            return _context.MissionRuns
                .Include(missionRun => missionRun.Area)
                .ThenInclude(area => area.Deck)
                .ThenInclude(deck => deck.Plant)
                .ThenInclude(plant => plant.Installation)
                .Include(missionRun => missionRun.Robot)
                .ThenInclude(robot => robot.VideoStreams)
                .Include(missionRun => missionRun.Robot)
                .ThenInclude(robot => robot.Model)
                .Include(missionRun => missionRun.Tasks)
                .ThenInclude(planTask => planTask.Inspections)
                .Include(missionRun => missionRun.Tasks)
                .ThenInclude(task => task.Inspections);
        }

        private static void SearchByName(ref IQueryable<MissionRun> missionRuns, string? name)
        {
            if (!missionRuns.Any() || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            missionRuns = missionRuns.Where(
                missionRun =>
                    missionRun.Name != null && missionRun.Name.ToLower().Contains(name.Trim().ToLower())
            );
        }

        private static void SearchByRobotName(ref IQueryable<MissionRun> missionRuns, string? robotName)
        {
            if (!missionRuns.Any() || string.IsNullOrWhiteSpace(robotName))
            {
                return;
            }

            missionRuns = missionRuns.Where(
                missionRun => missionRun.Robot.Name.ToLower().Contains(robotName.Trim().ToLower())
            );
        }

        private static void SearchByTag(ref IQueryable<MissionRun> missionRuns, string? tag)
        {
            if (!missionRuns.Any() || string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            missionRuns = missionRuns.Where(
                missionRun =>
                    missionRun.Tasks.Any(
                        task =>
                            task.TagId != null
                            && task.TagId.ToLower().Contains(tag.Trim().ToLower())
                    )
            );
        }

        /// <summary>
        ///     Filters by <see cref="MissionRunQueryStringParameters.InstallationCode" /> and
        ///     <see cref="MissionRunQueryStringParameters.Status" />
        ///     <para>
        ///         Uses LINQ Expression trees (see
        ///         <seealso href="https://docs.microsoft.com/en-us/dotnet/csharp/expression-trees" />)
        ///     </para>
        /// </summary>
        /// <param name="parameters"> The variable containing the filter params </param>
        private static Expression<Func<MissionRun, bool>> ConstructFilter(
            MissionRunQueryStringParameters parameters
        )
        {
            Expression<Func<MissionRun, bool>> areaFilter = parameters.Area is null
                ? missionRun => true
                : missionRun =>
                    missionRun.Area.Name.ToLower().Equals(parameters.Area.Trim().ToLower());

            Expression<Func<MissionRun, bool>> installationFilter = parameters.InstallationCode is null
                ? missionRun => true
                : missionRun =>
                    missionRun.InstallationCode.ToLower().Equals(parameters.InstallationCode.Trim().ToLower());

            Expression<Func<MissionRun, bool>> statusFilter = parameters.Statuses is null
                ? mission => true
                : mission => parameters.Statuses.Contains(mission.Status);

            Expression<Func<MissionRun, bool>> robotTypeFilter = parameters.RobotModelType is null
                ? missionRun => true
                : missionRun => missionRun.Robot.Model.Type.Equals(parameters.RobotModelType);

            Expression<Func<MissionRun, bool>> robotIdFilter = parameters.RobotId is null
                ? missionRun => true
                : missionRun => missionRun.Robot.Id.Equals(parameters.RobotId);

            Expression<Func<MissionRun, bool>> inspectionTypeFilter = parameters.InspectionTypes is null
                ? mission => true
                : mission => mission.Tasks.Any(
                    task =>
                        task.Inspections.Any(
                            inspection => parameters.InspectionTypes.Contains(inspection.InspectionType)
                        )
                );

            var minStartTime = DateTimeOffset.FromUnixTimeSeconds(parameters.MinStartTime);
            var maxStartTime = DateTimeOffset.FromUnixTimeSeconds(parameters.MaxStartTime);
            Expression<Func<MissionRun, bool>> startTimeFilter = missionRun =>
                missionRun.StartTime == null
                || DateTimeOffset.Compare(missionRun.StartTime.Value, minStartTime) >= 0
                && DateTimeOffset.Compare(missionRun.StartTime.Value, maxStartTime) <= 0;

            var minEndTime = DateTimeOffset.FromUnixTimeSeconds(parameters.MinEndTime);
            var maxEndTime = DateTimeOffset.FromUnixTimeSeconds(parameters.MaxEndTime);
            Expression<Func<MissionRun, bool>> endTimeFilter = missionRun =>
                missionRun.EndTime == null
                || DateTimeOffset.Compare(missionRun.EndTime.Value, minEndTime) >= 0
                && DateTimeOffset.Compare(missionRun.EndTime.Value, maxEndTime) <= 0;

            var minDesiredStartTime = DateTimeOffset.FromUnixTimeSeconds(
                parameters.MinDesiredStartTime
            );
            var maxDesiredStartTime = DateTimeOffset.FromUnixTimeSeconds(
                parameters.MaxDesiredStartTime
            );
            Expression<Func<MissionRun, bool>> desiredStartTimeFilter = missionRun =>
                DateTimeOffset.Compare(missionRun.DesiredStartTime, minDesiredStartTime) >= 0
                && DateTimeOffset.Compare(missionRun.DesiredStartTime, maxDesiredStartTime) <= 0;

            // The parameter of the filter expression
            var missionRun = Expression.Parameter(typeof(MissionRun));

            // Combining the body of the filters to create the combined filter, using invoke to force parameter substitution
            Expression body = Expression.AndAlso(
                Expression.Invoke(installationFilter, missionRun),
                Expression.AndAlso(
                    Expression.Invoke(statusFilter, missionRun),
                    Expression.AndAlso(
                        Expression.Invoke(robotIdFilter, missionRun),
                        Expression.AndAlso(
                            Expression.Invoke(inspectionTypeFilter, missionRun),
                            Expression.AndAlso(
                                Expression.Invoke(desiredStartTimeFilter, missionRun),
                                Expression.AndAlso(
                                    Expression.Invoke(startTimeFilter, missionRun),
                                    Expression.AndAlso(
                                        Expression.Invoke(endTimeFilter, missionRun),
                                        Expression.Invoke(robotTypeFilter, missionRun)
                                    )
                                )
                            )
                        )
                    )
                )
            );

            // Constructing the resulting lambda expression by combining parameter and body
            return Expression.Lambda<Func<MissionRun, bool>>(body, missionRun);
        }

        #region ISAR Specific methods

        private async Task<MissionRun?> ReadByIsarMissionId(string isarMissionId)
        {
            return await GetMissionRunsWithSubModels()
                .FirstOrDefaultAsync(
                    missionRun =>
                        missionRun.IsarMissionId != null && missionRun.IsarMissionId.Equals(isarMissionId)
                );
        }

        public async Task<MissionRun?> UpdateMissionRunStatusByIsarMissionId(
            string isarMissionId,
            MissionStatus missionStatus
        )
        {
            var missionRun = await ReadByIsarMissionId(isarMissionId);
            if (missionRun is null)
            {
                _logger.LogWarning(
                    "Could not update mission status for ISAR mission with id: {id} as the mission was not found",
                    isarMissionId
                );
                return null;
            }

            missionRun.Status = missionStatus;

            await _context.SaveChangesAsync();

            return missionRun;
        }

        public async Task<bool> UpdateTaskStatusByIsarTaskId(
            string isarMissionId,
            string isarTaskId,
            IsarTaskStatus taskStatus
        )
        {
            var missionRun = await ReadByIsarMissionId(isarMissionId);
            if (missionRun is null)
            {
                _logger.LogWarning(
                    "Could not update task status for ISAR task with id: {id} in mission run with id: {missionId} as the mission was not found",
                    isarTaskId,
                    isarMissionId
                );
                return false;
            }

            var task = missionRun.GetTaskByIsarId(isarTaskId);
            if (task is null)
            {
                _logger.LogWarning(
                    "Could not update task status for ISAR task with id: {id} as the task was not found",
                    isarTaskId
                );
                return false;
            }

            task.UpdateStatus(taskStatus);
            if (taskStatus == IsarTaskStatus.InProgress && missionRun.Status != MissionStatus.Ongoing)
            {
                // If mission was set to failed and then ISAR recovered connection, we need to reset the coming tasks
                missionRun.Status = MissionStatus.Ongoing;
                foreach (
                    var taskItem in missionRun.Tasks.Where(
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
            var missionRun = await ReadByIsarMissionId(isarMissionId);
            if (missionRun is null)
            {
                _logger.LogWarning(
                    "Could not update step status for ISAR inspection with id: {id} in mission with id: {missionId} as the mission was not found",
                    isarStepId,
                    isarMissionId
                );
                return false;
            }

            var task = missionRun.GetTaskByIsarId(isarTaskId);
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
