using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Dynamic.Core;
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
        public Task<MissionRun> Create(MissionRun missionRun, bool triggerCreatedMissionRunEvent = true);

        public Task<PagedList<MissionRun>> ReadAll(MissionRunQueryStringParameters parameters, bool readOnly = false);

        public Task<MissionRun?> ReadById(string id, bool readOnly = false);

        public Task<MissionRun?> ReadByIsarMissionId(string isarMissionId, bool readOnly = false);

        public Task<IList<MissionRun>> ReadMissionRunQueue(string robotId, bool readOnly = false);

        public Task<MissionRun?> ReadNextScheduledRunByMissionId(string missionId, bool readOnly = false);

        public Task<MissionRun?> ReadNextScheduledMissionRun(string robotId, bool readOnly = false);

        public Task<MissionRun?> ReadNextScheduledEmergencyMissionRun(string robotId, bool readOnly = false);

        public Task<MissionRun?> ReadNextScheduledLocalizationMissionRun(string robotId, bool readOnly = false);

        public Task<IList<MissionRun>> ReadMissionRuns(string robotId, MissionRunType? missionRunType, IList<MissionStatus>? filterStatuses = null, bool readOnly = false);

        public Task<MissionRun?> ReadLastExecutedMissionRunByRobot(string robotId, bool readOnly = false);

        public Task<bool> PendingLocalizationMissionRunExists(string robotId);

        public Task<bool> OngoingLocalizationMissionRunExists(string robotId);

        public Task<bool> PendingOrOngoingLocalizationMissionRunExists(string robotId);

        public Task<bool> PendingOrOngoingReturnToHomeMissionRunExists(string robotId);

        public bool IncludesUnsupportedInspectionType(MissionRun missionRun);

        public Task<MissionRun> Update(MissionRun mission);

        public Task<MissionRun> UpdateMissionRunType(string missionRunId, MissionRunType missionRunType);

        public Task<MissionRun> UpdateMissionRunStatusByIsarMissionId(
            string isarMissionId,
            MissionStatus missionStatus
        );
        public Task<MissionRun?> Delete(string id);
        public Task<bool> OngoingMission(string robotId);
        public Task<MissionRun> UpdateMissionRunProperty(string missionRunId, string propertyName, object? value);

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
        public async Task<MissionRun> Create(MissionRun missionRun, bool triggerCreatedMissionRunEvent = true)
        {
            missionRun.Id ??= Guid.NewGuid().ToString(); // Useful for signalR messages
            // Making sure database does not try to create new robot
            try
            {
                context.Entry(missionRun.Robot).State = EntityState.Unchanged;
            }
            catch (InvalidOperationException e)
            {
                throw new DatabaseUpdateException($"Unable to create mission. {e}");
            }


            if (IncludesUnsupportedInspectionType(missionRun))
            {
                throw new UnsupportedRobotCapabilityException($"Mission {missionRun.Name} contains inspection types not supported by robot: {missionRun.Robot.Name}.");
            }

            if (missionRun.Area is not null) { context.Entry(missionRun.Area).State = EntityState.Unchanged; }
            await context.MissionRuns.AddAsync(missionRun);
            await ApplyDatabaseUpdate(missionRun.Area?.Installation);
            _ = signalRService.SendMessageAsync("Mission run created", missionRun.Area?.Installation, new MissionRunResponse(missionRun));

            if (triggerCreatedMissionRunEvent)
            {
                var args = new MissionRunCreatedEventArgs(missionRun.Id);
                OnMissionRunCreated(args);
            }

            return missionRun;
        }

        public async Task<PagedList<MissionRun>> ReadAll(MissionRunQueryStringParameters parameters, bool readOnly = false)
        {
            var query = GetMissionRunsWithSubModels(readOnly: readOnly);
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

        public async Task<MissionRun?> ReadById(string id, bool readOnly = false)
        {
            return await GetMissionRunsWithSubModels(readOnly: readOnly)
                .FirstOrDefaultAsync(missionRun => missionRun.Id.Equals(id));
        }

        public async Task<IList<MissionRun>> ReadMissionRunQueue(string robotId, bool readOnly = false)
        {
            return await GetMissionRunsWithSubModels(readOnly: readOnly)
                .Where(missionRun => missionRun.Robot.Id == robotId && missionRun.Status == MissionStatus.Pending)
                .OrderBy(missionRun => missionRun.DesiredStartTime)
                .ToListAsync();
        }

        public async Task<MissionRun?> ReadNextScheduledMissionRun(string robotId, bool readOnly = false)
        {
            return await GetMissionRunsWithSubModels(readOnly: readOnly)
                .OrderBy(missionRun => missionRun.DesiredStartTime)
                .FirstOrDefaultAsync(missionRun => missionRun.Robot.Id == robotId && missionRun.Status == MissionStatus.Pending);
        }

        public async Task<MissionRun?> ReadNextScheduledEmergencyMissionRun(string robotId, bool readOnly = false)
        {
            return await GetMissionRunsWithSubModels(readOnly: readOnly)
                .OrderBy(missionRun => missionRun.DesiredStartTime)
                .FirstOrDefaultAsync(missionRun =>
                    missionRun.Robot.Id == robotId && missionRun.MissionRunType == MissionRunType.Emergency && missionRun.Status == MissionStatus.Pending);
        }

        public async Task<MissionRun?> ReadNextScheduledLocalizationMissionRun(string robotId, bool readOnly = false)
        {
            return await GetMissionRunsWithSubModels(readOnly: readOnly)
                    .OrderBy(missionRun => missionRun.DesiredStartTime)
                    .FirstOrDefaultAsync(missionRun => missionRun.Robot.Id == robotId && missionRun.Status == MissionStatus.Pending && missionRun.MissionRunType == MissionRunType.Localization);
        }

        public async Task<IList<MissionRun>> ReadMissionRuns(string robotId, MissionRunType? missionRunType, IList<MissionStatus>? filterStatuses = null, bool readOnly = false)
        {
            var missionFilter = ConstructFilter(new MissionRunQueryStringParameters
            {
                Statuses = filterStatuses as List<MissionStatus> ?? null,
                RobotId = robotId,
                MissionRunType = missionRunType,
                PageSize = 100
            });

            return await GetMissionRunsWithSubModels(readOnly: readOnly)
                .Where(missionFilter)
                .OrderBy(missionRun => missionRun.DesiredStartTime)
                .ToListAsync();
        }

        public async Task<MissionRun?> ReadNextScheduledRunByMissionId(string missionId, bool readOnly = false)
        {
            return await GetMissionRunsWithSubModels(readOnly: readOnly)
                .Where(m => m.MissionId == missionId && m.EndTime == null)
                .OrderBy(m => m.DesiredStartTime)
                .FirstOrDefaultAsync();
        }

        public async Task<MissionRun?> ReadLastExecutedMissionRunByRobot(string robotId, bool readOnly = false)
        {
            return await GetMissionRunsWithSubModels(readOnly: readOnly)
                .Where(m => m.Robot.Id == robotId)
                .Where(m => m.EndTime != null)
                .OrderByDescending(m => m.EndTime)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<bool> PendingLocalizationMissionRunExists(string robotId)
        {
            var pendingMissionRuns = await ReadMissionRunQueue(robotId, readOnly: true);
            return pendingMissionRuns.Any((m) => m.IsLocalizationMission());
        }

        public async Task<bool> OngoingLocalizationMissionRunExists(string robotId)
        {
            var ongoingMissionRuns = await GetMissionRunsWithSubModels(readOnly: true)
                .Where(missionRun => missionRun.Robot.Id == robotId && missionRun.Status == MissionStatus.Ongoing)
                .OrderBy(missionRun => missionRun.DesiredStartTime)
                .ToListAsync();
            foreach (var ongoingMissionRun in ongoingMissionRuns)
            {
                if (ongoingMissionRun.IsLocalizationMission()) { return true; }
            }
            return false;
        }

        public async Task<bool> PendingOrOngoingLocalizationMissionRunExists(string robotId)
        {
            var pendingMissionRuns = await ReadMissionRunQueue(robotId, readOnly: true);
            foreach (var pendingMissionRun in pendingMissionRuns)
            {
                if (pendingMissionRun.IsLocalizationMission()) { return true; }
            }
            var ongoingMissionRuns = await GetMissionRunsWithSubModels(readOnly: true)
                .Where(missionRun => missionRun.Robot.Id == robotId && missionRun.Status == MissionStatus.Ongoing)
                .OrderBy(missionRun => missionRun.DesiredStartTime)
                .ToListAsync();
            foreach (var ongoingMissionRun in ongoingMissionRuns)
            {
                if (ongoingMissionRun.IsLocalizationMission()) { return true; }
            }
            return false;
        }

        public async Task<bool> PendingOrOngoingReturnToHomeMissionRunExists(string robotId)
        {
            var pendingMissionRuns = await ReadMissionRunQueue(robotId, readOnly: true);
            foreach (var pendingMissionRun in pendingMissionRuns)
            {
                if (pendingMissionRun.IsReturnHomeMission()) { return true; }
            }
            var ongoingMissionRuns = await GetMissionRunsWithSubModels(readOnly: true)
                .Where(missionRun => missionRun.Robot.Id == robotId && missionRun.Status == MissionStatus.Ongoing)
                .OrderBy(missionRun => missionRun.DesiredStartTime)
                .ToListAsync();
            foreach (var ongoingMissionRun in ongoingMissionRuns)
            {
                if (ongoingMissionRun.IsReturnHomeMission()) { return true; }
            }
            return false;

        }

        public bool IncludesUnsupportedInspectionType(MissionRun missionRun)
        {
            if (missionRun.Robot.RobotCapabilities == null) return false;

            foreach (var task in missionRun.Tasks)
                foreach (var inspection in task.Inspections)
                    if (!inspection.IsSupportedInspectionType(missionRun.Robot.RobotCapabilities))
                        return true;
            return false;
        }

        public async Task<MissionRun> Update(MissionRun missionRun)
        {
            context.Entry(missionRun.Robot).State = EntityState.Unchanged;
            if (missionRun.Area is not null) { context.Entry(missionRun.Area).State = EntityState.Unchanged; }

            var entry = context.Update(missionRun);
            await ApplyDatabaseUpdate(missionRun.Area?.Installation);
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

            await UpdateMissionRunProperty(missionRun.Id, "IsDeprecated", true);
            _ = signalRService.SendMessageAsync("Mission run deleted", missionRun?.Area?.Installation, missionRun != null ? new MissionRunResponse(missionRun) : null);

            return missionRun;
        }

        public async Task<bool> OngoingMission(string robotId)
        {
            var ongoingMissions = await ReadAll(
                new MissionRunQueryStringParameters
                {
                    Statuses = [MissionStatus.Ongoing],
                    RobotId = robotId,
                    OrderBy = "DesiredStartTime",
                    PageSize = 100
                });

            return ongoingMissions.Any();
        }

        private IQueryable<MissionRun> GetMissionRunsWithSubModels(bool readOnly = false)
        {
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var query = context.MissionRuns
                .Include(missionRun => missionRun.Area)
                .ThenInclude(area => area != null ? area.Deck : null)
                .ThenInclude(deck => deck != null ? deck.Plant : null)
                .ThenInclude(plant => plant != null ? plant.Installation : null)
                .Include(missionRun => missionRun.Area)
                .ThenInclude(area => area != null ? area.Plant : null)
                .ThenInclude(plant => plant != null ? plant.Installation : null)
                .Include(missionRun => missionRun.Area)
                .ThenInclude(area => area != null ? area.Installation : null)
                .Include(missionRun => missionRun.Area)
                .ThenInclude(area => area != null ? area.Deck : null)
                .ThenInclude(deck => deck != null ? deck.DefaultLocalizationPose : null)
                .ThenInclude(defaultLocalizationPose => defaultLocalizationPose != null ? defaultLocalizationPose.Pose : null)
                .Include(missionRun => missionRun.Robot)
                .ThenInclude(robot => robot.VideoStreams)
                .Include(missionRun => missionRun.Robot)
                .ThenInclude(robot => robot.Model)
                .Include(missionRun => missionRun.Tasks)
                .ThenInclude(task => task.Inspections)
                .ThenInclude(inspections => inspections.InspectionFindings)
                .Where((m) => m.Area == null || accessibleInstallationCodes.Result.Contains(m.Area.Installation.InstallationCode.ToUpper()))
                .Where((m) => m.IsDeprecated == false);
            return readOnly ? query.AsNoTracking() : query;
        }

        protected virtual void OnMissionRunCreated(MissionRunCreatedEventArgs e)
        {
            MissionRunCreated?.Invoke(this, e);
        }

        public static event EventHandler<MissionRunCreatedEventArgs>? MissionRunCreated;

        private async Task ApplyDatabaseUpdate(Installation? installation)
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes();
            if (installation == null || accessibleInstallationCodes.Contains(installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)))
                await context.SaveChangesAsync();
            else
                throw new UnauthorizedAccessException($"User does not have permission to update mission run in installation {installation.Name}");
        }

        private static void SearchByName(ref IQueryable<MissionRun> missionRuns, string? name)
        {
            if (!missionRuns.Any() || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

#pragma warning disable CA1862
            missionRuns = missionRuns.Where(
                missionRun =>
                    missionRun.Name != null && missionRun.Name.ToLower().Contains(name.ToLower().Trim())
            );
#pragma warning restore CA1862
        }

        private static void SearchByRobotName(ref IQueryable<MissionRun> missionRuns, string? robotName)
        {
            if (!missionRuns.Any() || string.IsNullOrWhiteSpace(robotName)) { return; }

#pragma warning disable CA1862
            missionRuns = missionRuns.Where(
                missionRun => missionRun.Robot.Name.ToLower().Contains(robotName.ToLower().Trim())
            );
#pragma warning restore CA1862
        }

        private static void SearchByTag(ref IQueryable<MissionRun> missionRuns, string? tag)
        {
            if (!missionRuns.Any() || string.IsNullOrWhiteSpace(tag)) { return; }

            missionRuns = missionRuns.Where(
                missionRun =>
                    missionRun.Tasks.Any(
                        task =>
#pragma warning disable CA1307
                            task.TagId != null
                            && task.TagId.Contains(tag.Trim())
#pragma warning restore CA1307
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
        ///     <see cref="MissionRunQueryStringParameters.ExcludeLocalization" />,
        ///     <see cref="MissionRunQueryStringParameters.ExcludeReturnToHome" />,
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

            Expression<Func<MissionRun, bool>> missionTypeFilter = parameters.MissionRunType is null
                ? missionRun => true
                : missionRun => missionRun.MissionRunType.Equals(parameters.MissionRunType);

            Expression<Func<MissionRun, bool>> inspectionTypeFilter = parameters.InspectionTypes is null
                ? mission => true
                : mission => mission.Tasks.Any(
                    task =>
                        task.Inspections.Any(
                            inspection => parameters.InspectionTypes.Contains(inspection.InspectionType)
                        )
                );

            Expression<Func<MissionRun, bool>> localizationFilter = !parameters.ExcludeLocalization
                ? missionRun => true
                : missionRun => !(missionRun.Tasks.Count() == 1 && missionRun.Tasks.All(task => task.Type == MissionTaskType.Localization));

            Expression<Func<MissionRun, bool>> returnTohomeFilter = !parameters.ExcludeReturnToHome
                ? missionRun => true
                : missionRun => !(missionRun.Tasks.Count() == 1 && missionRun.Tasks.All(task => task.Type == MissionTaskType.ReturnHome));

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
                                Expression.Invoke(missionTypeFilter, missionRun),
                                Expression.AndAlso(
                                    Expression.Invoke(inspectionTypeFilter, missionRun),
                                    Expression.AndAlso(
                                        Expression.Invoke(localizationFilter, missionRun),
                                        Expression.AndAlso(
                                            Expression.Invoke(returnTohomeFilter, missionRun),
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
                        )
                    )
                )
            );

            // Constructing the resulting lambda expression by combining parameter and body
            return Expression.Lambda<Func<MissionRun, bool>>(body, missionRun);
        }

        #region ISAR Specific methods

        public async Task<MissionRun?> ReadByIsarMissionId(string isarMissionId, bool readOnly = false)
        {
            return await GetMissionRunsWithSubModels(readOnly: readOnly)
                .FirstOrDefaultAsync(
                    missionRun =>
                        missionRun.IsarMissionId != null && missionRun.IsarMissionId.Equals(isarMissionId)
                );
        }

        public async Task<MissionRun> UpdateMissionRunType(string missionRunId, MissionRunType missionRunType)
        {
            var missionRun = await ReadById(missionRunId);
            if (missionRun is null)
            {
                string errorMessage = $"Mission with mission Id {missionRunId} was not found";
                logger.LogError("{Message}", errorMessage);
                throw new MissionRunNotFoundException(errorMessage);
            }

            return await UpdateMissionRunProperty(missionRun.Id, "MissionRunType", missionRunType);
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

            missionRun = await UpdateMissionRunProperty(missionRun.Id, "MissionStatus", missionStatus);

            if (missionRun.Status == MissionStatus.Failed) { _ = signalRService.SendMessageAsync("Mission run failed", missionRun?.Area?.Installation, missionRun != null ? new MissionRunResponse(missionRun) : null); }
            return missionRun!;
        }

        #endregion ISAR Specific methods


        public async Task<MissionRun> UpdateMissionRunProperty(string missionRunId, string propertyName, object? value)
        {
            var missionRun = await ReadById(missionRunId);
            if (missionRun is null)
            {
                string errorMessage = $"Mission with ID {missionRunId} was not found in the database";
                logger.LogError("{Message}", errorMessage);
                throw new MissionRunNotFoundException(errorMessage);
            }

            foreach (var property in typeof(MissionRun).GetProperties())
            {
                if (property.Name == propertyName)
                {
                    logger.LogInformation("Setting {missionRunName} field {propertyName} from {oldValue} to {NewValue}", missionRun.Name, propertyName, property.GetValue(missionRun), value);
                    property.SetValue(missionRun, value);
                }
            }

            try { missionRun = await Update(missionRun); }
            catch (InvalidOperationException e) { logger.LogError(e, "Failed to update {missionRunName}", missionRun.Name); };
            return missionRun;
        }
    }
}
