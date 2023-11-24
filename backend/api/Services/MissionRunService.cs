using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services.Events;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;
namespace Api.Services
{
    public interface IMissionRunService
    {
        public Task<MissionRun> Create(MissionRun missionRun);

        public Task<PagedList<MissionRun>> ReadAll(MissionRunQueryStringParameters parameters);

        public Task<MissionRun?> ReadById(string id);

        public Task<MissionRun?> ReadByIsarMissionId(string isarMissionId);

        public Task<IList<MissionRun>> ReadMissionRunQueue(string robotId);

        public Task<MissionRun?> ReadNextScheduledRunByMissionId(string missionId);

        public Task<MissionRun?> ReadNextScheduledMissionRun(string robotId);

        public Task<MissionRun?> ReadNextScheduledEmergencyMissionRun(string robotId);

        public Task<MissionRun> Update(MissionRun mission);

        public Task<MissionRun> UpdateMissionRunStatusByIsarMissionId(
            string isarMissionId,
            MissionStatus missionStatus
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
    public class MissionRunService(
        FlotillaDbContext context,
        ISignalRService signalRService,
        ILogger<MissionRunService> logger,
        IAccessRoleService accessRoleService) : IMissionRunService
    {
        public async Task<MissionRun> Create(MissionRun missionRun)
        {
            missionRun.Id ??= Guid.NewGuid().ToString(); // Useful for signalR messages
            // Making sure database does not try to create new robot
            context.Entry(missionRun.Robot).State = EntityState.Unchanged;
            if (missionRun.Area is not null) { context.Entry(missionRun.Area).State = EntityState.Unchanged; }

            await context.MissionRuns.AddAsync(missionRun);
            await context.SaveChangesAsync();
            _ = signalRService.SendMessageAsync("Mission run created", missionRun?.Area?.Installation, missionRun);

            var args = new MissionRunCreatedEventArgs(missionRun!.Id);
            OnMissionRunCreated(args);

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

        public async Task<IList<MissionRun>> ReadMissionRunQueue(string robotId)
        {
            return await GetMissionRunsWithSubModels()
                .Where(missionRun => missionRun.Robot.Id == robotId && missionRun.Status == MissionStatus.Pending)
                .OrderBy(missionRun => missionRun.DesiredStartTime)
                .ToListAsync();
        }

        public async Task<MissionRun?> ReadNextScheduledMissionRun(string robotId)
        {
            return await GetMissionRunsWithSubModels()
                .OrderBy(missionRun => missionRun.DesiredStartTime)
                .FirstOrDefaultAsync(missionRun => missionRun.Robot.Id == robotId && missionRun.Status == MissionStatus.Pending);
        }

        public async Task<MissionRun?> ReadNextScheduledEmergencyMissionRun(string robotId)
        {
            return await GetMissionRunsWithSubModels()
                .OrderBy(missionRun => missionRun.DesiredStartTime)
                .FirstOrDefaultAsync(missionRun =>
                    missionRun.Robot.Id == robotId && missionRun.MissionRunPriority == MissionRunPriority.Emergency && missionRun.Status == MissionStatus.Pending);
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
            context.Entry(missionRun.Robot).State = EntityState.Unchanged;
            if (missionRun.Area is not null) { context.Entry(missionRun.Area).State = EntityState.Unchanged; }

            var entry = context.Update(missionRun);
            await context.SaveChangesAsync();
            _ = signalRService.SendMessageAsync("Mission run updated", missionRun?.Area?.Installation, missionRun != null ? new MissionRunResponse(missionRun) : null);
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

            context.MissionRuns.Remove(missionRun);
            await context.SaveChangesAsync();
            _ = signalRService.SendMessageAsync("Mission run deleted", missionRun?.Area?.Installation,  missionRun != null ? new MissionRunResponse(missionRun) : null);

            return missionRun;
        }

        private IQueryable<MissionRun> GetMissionRunsWithSubModels()
        {
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            return context.MissionRuns
                .Include(missionRun => missionRun.Area)
                .ThenInclude(area => area != null ? area.Deck : null)
                .Include(missionRun => missionRun.Area)
                .ThenInclude(area => area != null ? area.Plant : null)
                .Include(missionRun => missionRun.Area)
                .ThenInclude(area => area != null ? area.Installation : null)
                .Include(missionRun => missionRun.Robot)
                .ThenInclude(robot => robot.VideoStreams)
                .Include(missionRun => missionRun.Robot)
                .ThenInclude(robot => robot.Model)
                .Include(missionRun => missionRun.Tasks)
                .ThenInclude(task => task.Inspections)
                .Where((m) => m.Area == null || accessibleInstallationCodes.Result.Contains(m.Area.Installation.InstallationCode.ToUpper())); ;
        }

        protected virtual void OnMissionRunCreated(MissionRunCreatedEventArgs e)
        {
            MissionRunCreated?.Invoke(this, e);
        }

        public static event EventHandler<MissionRunCreatedEventArgs>? MissionRunCreated;

        private static void SearchByName(ref IQueryable<MissionRun> missionRuns, string? name)
        {
            if (!missionRuns.Any() || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            missionRuns = missionRuns.Where(
                missionRun =>
                    missionRun.Name != null && missionRun.Name.Contains(name.Trim(), StringComparison.OrdinalIgnoreCase)
            );
        }

        private static void SearchByRobotName(ref IQueryable<MissionRun> missionRuns, string? robotName)
        {
            if (!missionRuns.Any() || string.IsNullOrWhiteSpace(robotName)) { return; }

            missionRuns = missionRuns.Where(
                missionRun => missionRun.Robot.Name.Contains(robotName.Trim(), StringComparison.OrdinalIgnoreCase)
            );
        }

        private static void SearchByTag(ref IQueryable<MissionRun> missionRuns, string? tag)
        {
            if (!missionRuns.Any() || string.IsNullOrWhiteSpace(tag)) { return; }

            missionRuns = missionRuns.Where(
                missionRun =>
                    missionRun.Tasks.Any(
                        task =>
                            task.TagId != null
                            && task.TagId.Contains(tag.Trim(), StringComparison.OrdinalIgnoreCase)
                    )
            );
        }

        /// <summary>
        ///     Filters by <see cref="MissionRunQueryStringParameters.InstallationCode" />,
        ///     <see cref="MissionRunQueryStringParameters.Area" />,
        ///     <see cref="MissionRunQueryStringParameters.Statuses" />,
        ///     <see cref="MissionRunQueryStringParameters.RobotId" />,
        ///     <see cref="MissionRunQueryStringParameters.RobotModelType" />,
        ///     <see cref="MissionRunQueryStringParameters.NameSearch" />,
        ///     <see cref="MissionRunQueryStringParameters.RobotNameSearch" />,
        ///     <see cref="MissionRunQueryStringParameters.TagSearch" />,
        ///     <see cref="MissionRunQueryStringParameters.InspectionTypes" />,
        ///     <see cref="MissionRunQueryStringParameters.MinStartTime" />,
        ///     <see cref="MissionRunQueryStringParameters.MaxStartTime" />,
        ///     <see cref="MissionRunQueryStringParameters.MinEndTime" />,
        ///     <see cref="MissionRunQueryStringParameters.MaxEndTime" />,
        ///     <see cref="MissionRunQueryStringParameters.MinDesiredStartTime" /> and
        ///     <see cref="MissionRunQueryStringParameters.MaxDesiredStartTime" />
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
                    missionRun.Area != null &&
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

            Expression<Func<MissionRun, bool>> missionIdFilter = parameters.MissionId is null
                ? missionRun => true
                : missionRun => missionRun.MissionId != null && missionRun.MissionId.Equals(parameters.MissionId);

            Expression<Func<MissionRun, bool>> inspectionTypeFilter = parameters.InspectionTypes is null
                ? mission => true
                : mission => mission.Tasks.Any(
                    task =>
                        task.Inspections.Any(
                            inspection => parameters.InspectionTypes.Contains(inspection.InspectionType)
                        )
                );

            var minStartTime = DateTimeUtilities.UnixTimeStampToDateTime(parameters.MinStartTime);
            var maxStartTime = DateTimeUtilities.UnixTimeStampToDateTime(parameters.MaxStartTime);
            Expression<Func<MissionRun, bool>> startTimeFilter = missionRun =>
                missionRun.StartTime == null
                || (DateTime.Compare(missionRun.StartTime.Value, minStartTime) >= 0
                && DateTime.Compare(missionRun.StartTime.Value, maxStartTime) <= 0);

            var minEndTime = DateTimeUtilities.UnixTimeStampToDateTime(parameters.MinEndTime);
            var maxEndTime = DateTimeUtilities.UnixTimeStampToDateTime(parameters.MaxEndTime);
            Expression<Func<MissionRun, bool>> endTimeFilter = missionRun =>
                missionRun.EndTime == null
                || (DateTime.Compare(missionRun.EndTime.Value, minEndTime) >= 0
                && DateTime.Compare(missionRun.EndTime.Value, maxEndTime) <= 0);

            var minDesiredStartTime = DateTimeUtilities.UnixTimeStampToDateTime(parameters.MinDesiredStartTime);
            var maxDesiredStartTime = DateTimeUtilities.UnixTimeStampToDateTime(parameters.MaxDesiredStartTime);
            Expression<Func<MissionRun, bool>> desiredStartTimeFilter = missionRun =>
                DateTime.Compare(missionRun.DesiredStartTime, minDesiredStartTime) >= 0
                && DateTime.Compare(missionRun.DesiredStartTime, maxDesiredStartTime) <= 0;

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
                            Expression.Invoke(missionIdFilter, missionRun),
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
                )
            );

            // Constructing the resulting lambda expression by combining parameter and body
            return Expression.Lambda<Func<MissionRun, bool>>(body, missionRun);
        }

        #region ISAR Specific methods

        public async Task<MissionRun?> ReadByIsarMissionId(string isarMissionId)
        {
            return await GetMissionRunsWithSubModels()
                .FirstOrDefaultAsync(
                    missionRun =>
                        missionRun.IsarMissionId != null && missionRun.IsarMissionId.Equals(isarMissionId)
                );
        }

        public async Task<MissionRun> UpdateMissionRunStatusByIsarMissionId(string isarMissionId, MissionStatus missionStatus)
        {
            var missionRun = await ReadByIsarMissionId(isarMissionId);
            if (missionRun is null)
            {
                string errorMessage = $"Mission with isar mission Id {isarMissionId} was not found";
                logger.LogError("{Message}", errorMessage);
                throw new MissionRunNotFoundException(errorMessage);
            }

            missionRun.Status = missionStatus;

            missionRun = await Update(missionRun);

            if (missionRun.Status == MissionStatus.Failed) { _ = signalRService.SendMessageAsync("Mission run failed", missionRun?.Area?.Installation,  missionRun != null ? new MissionRunResponse(missionRun) : null); }
            return missionRun!;
        }

        #endregion ISAR Specific methods

    }
}
