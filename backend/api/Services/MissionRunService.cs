﻿using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services.Events;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IMissionRunService
    {
        public Task<MissionRun> Create(
            MissionRun missionRun,
            bool triggerCreatedMissionRunEvent = true
        );

        public Task<PagedList<MissionRun>> ReadAll(
            MissionRunQueryStringParameters parameters,
            bool readOnly = true
        );

        public Task<MissionRun?> ReadById(string id, bool readOnly = true);

        public Task<MissionRun?> ReadByIsarMissionId(string isarMissionId, bool readOnly = true);

        public Task<IList<MissionRun>> ReadMissionRunQueue(
            string robotId,
            MissionRunType type = MissionRunType.Normal,
            bool readOnly = true
        );

        public Task<MissionRun?> ReadNextScheduledRunByMissionId(
            string missionId,
            bool readOnly = true
        );

        public Task<MissionRun?> ReadNextScheduledMissionRun(
            string robotId,
            MissionRunType type = MissionRunType.Normal,
            bool readOnly = true
        );

        public Task<IList<MissionRun>> ReadMissionRuns(
            string robotId,
            MissionRunType? missionRunType,
            IList<MissionStatus>? filterStatuses = null,
            bool readOnly = true
        );

        public Task<MissionRun?> ReadLastExecutedMissionRunByRobot(
            string robotId,
            bool readOnly = true
        );

        public bool IncludesUnsupportedInspectionType(MissionRun missionRun);

        public Task<MissionRun> UpdateMissionRunType(
            string missionRunId,
            MissionRunType missionRunType
        );

        public Task<MissionRun> UpdateMissionRunStatusByIsarMissionId(
            string isarMissionId,
            MissionStatus missionStatus,
            string? errorDescription = null
        );

        public Task<MissionRun?> Delete(string id);

        public Task<MissionRun> UpdateMissionRunProperty(
            string missionRunId,
            string propertyName,
            object? value
        );

        public Task<MissionRun> UpdateWithIsarInfo(string missionRunId, IsarMission isarMission);

        public Task<MissionRun> SetMissionRunToFailed(
            string missionRunId,
            string failureDescription
        );

        public Task UpdateCurrentRobotMissionToFailed(string robotId);

        public void DetachTracking(FlotillaDbContext context, MissionRun missionRun);
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
        IAccessRoleService accessRoleService,
        IMissionTaskService missionTaskService,
        IInspectionAreaService inspectionAreaService,
        IRobotService robotService,
        IUserInfoService userInfoService
    ) : IMissionRunService
    {
        public async Task<MissionRun> Create(
            MissionRun missionRun,
            bool triggerCreatedMissionRunEvent = true
        )
        {
            missionRun.Id ??= Guid.NewGuid().ToString(); // Useful for signalR messages

            if (IncludesUnsupportedInspectionType(missionRun))
            {
                throw new UnsupportedRobotCapabilityException(
                    $"Mission {missionRun.Name} contains inspection types not supported by robot: {missionRun.Robot.Name}."
                );
            }

            context.Entry(missionRun.InspectionArea).State = EntityState.Unchanged;
            if (missionRun.Robot is not null)
            {
                context.Entry(missionRun.Robot).State = EntityState.Unchanged;
            }
            await context.MissionRuns.AddAsync(missionRun);
            await ApplyDatabaseUpdate(missionRun.InspectionArea.Installation);

            _ = signalRService.SendMessageAsync(
                "Mission run created",
                missionRun.InspectionArea.Installation,
                new MissionRunResponse(missionRun)
            );

            DetachTracking(context, missionRun);

            if (triggerCreatedMissionRunEvent)
            {
                var args = new MissionRunCreatedEventArgs(missionRun);
                OnMissionRunCreated(args);
            }

            var userInfo = await userInfoService.GetRequestedUserInfo();
            if (userInfo != null)
            {
                logger.LogInformation($"Mission run created by user with Id {userInfo.Id}");
            }

            return missionRun;
        }

        public async Task<PagedList<MissionRun>> ReadAll(
            MissionRunQueryStringParameters parameters,
            bool readOnly = true
        )
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

        public async Task<MissionRun?> ReadById(string id, bool readOnly = true)
        {
            return await GetMissionRunsWithSubModels(readOnly: readOnly)
                .FirstOrDefaultAsync(missionRun => missionRun.Id.Equals(id));
        }

        public async Task<IList<MissionRun>> ReadMissionRunQueue(
            string robotId,
            MissionRunType type = MissionRunType.Normal,
            bool readOnly = true
        )
        {
            return await GetMissionRunsWithSubModels(readOnly: readOnly)
                .Where(missionRun =>
                    missionRun.Robot.Id == robotId
                    && missionRun.Status == MissionStatus.Pending
                    && missionRun.MissionRunType == type
                )
                .OrderBy(missionRun => missionRun.DesiredStartTime)
                .ToListAsync();
        }

        public async Task<MissionRun?> ReadNextScheduledMissionRun(
            string robotId,
            MissionRunType type = MissionRunType.Normal,
            bool readOnly = true
        )
        {
            return await GetMissionRunsWithSubModels(readOnly: readOnly)
                .OrderBy(missionRun => missionRun.DesiredStartTime)
                .FirstOrDefaultAsync(missionRun =>
                    missionRun.Robot.Id == robotId
                    && missionRun.Status == MissionStatus.Pending
                    && missionRun.MissionRunType == type
                );
        }

        public async Task<IList<MissionRun>> ReadMissionRuns(
            string robotId,
            MissionRunType? missionRunType,
            IList<MissionStatus>? filterStatuses = null,
            bool readOnly = true
        )
        {
            var missionFilter = ConstructFilter(
                new MissionRunQueryStringParameters
                {
                    Statuses = filterStatuses as List<MissionStatus> ?? null,
                    RobotId = robotId,
                    MissionRunType = missionRunType,
                    PageSize = 100,
                }
            );

            return await GetMissionRunsWithSubModels(readOnly: readOnly)
                .Where(missionFilter)
                .OrderBy(missionRun => missionRun.DesiredStartTime)
                .ToListAsync();
        }

        public async Task<MissionRun?> ReadNextScheduledRunByMissionId(
            string missionId,
            bool readOnly = true
        )
        {
            return await GetMissionRunsWithSubModels(readOnly: readOnly)
                .Where(m => m.MissionId == missionId && m.EndTime == null)
                .OrderBy(m => m.DesiredStartTime)
                .FirstOrDefaultAsync();
        }

        public async Task<MissionRun?> ReadLastExecutedMissionRunByRobot(
            string robotId,
            bool readOnly = true
        )
        {
            return await GetMissionRunsWithSubModels(readOnly: readOnly)
                .Where(m => m.Robot.Id == robotId)
                .Where(m => m.EndTime != null)
                .OrderByDescending(m => m.EndTime)
                .FirstOrDefaultAsync();
        }

        public bool IncludesUnsupportedInspectionType(MissionRun missionRun)
        {
            if (missionRun.Robot.RobotCapabilities == null)
                return false;

            return missionRun.Tasks.Any(task =>
                task.Inspection != null
                && !task.Inspection.IsSupportedInspectionType(missionRun.Robot.RobotCapabilities)
            );
        }

        public async Task Update(MissionRun missionRun)
        {
            if (missionRun.Robot is not null)
            {
                context.Entry(missionRun.Robot).State = EntityState.Unchanged;
            }
            context.Entry(missionRun.InspectionArea).State = EntityState.Unchanged;
            foreach (var task in missionRun.Tasks)
            {
                if (task.Inspection != null)
                    context.Entry(task.Inspection).State = EntityState.Unchanged;
            }

            var entry = context.Update(missionRun);
            await ApplyDatabaseUpdate(missionRun.InspectionArea.Installation);
            _ = signalRService.SendMessageAsync(
                "Mission run updated",
                missionRun?.InspectionArea.Installation,
                missionRun != null ? new MissionRunResponse(missionRun) : null
            );
            DetachTracking(context, missionRun!);
        }

        public async Task UpdateWithInspections(MissionRun missionRun)
        {
            if (missionRun.Robot is not null)
            {
                context.Entry(missionRun.Robot).State = EntityState.Unchanged;
            }
            context.Entry(missionRun.InspectionArea).State = EntityState.Unchanged;

            var entry = context.Update(missionRun);
            await ApplyDatabaseUpdate(missionRun.InspectionArea.Installation);
            _ = signalRService.SendMessageAsync(
                "Mission run updated",
                missionRun?.InspectionArea.Installation,
                missionRun != null ? new MissionRunResponse(missionRun) : null
            );
            DetachTracking(context, missionRun!);
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
            _ = signalRService.SendMessageAsync(
                "Mission run deleted",
                missionRun?.InspectionArea.Installation,
                missionRun != null ? new MissionRunResponse(missionRun) : null
            );

            return missionRun;
        }

        private IQueryable<MissionRun> GetMissionRunsWithSubModels(bool readOnly = true)
        {
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var query = context
                .MissionRuns.Include(missionRun => missionRun.InspectionArea)
                .ThenInclude(inspectionArea => inspectionArea != null ? inspectionArea.Plant : null)
                .ThenInclude(plant => plant != null ? plant.Installation : null)
                .Include(missionRun => missionRun.InspectionArea)
                .ThenInclude(area => area != null ? area.Plant : null)
                .ThenInclude(plant => plant != null ? plant.Installation : null)
                .Include(missionRun => missionRun.InspectionArea)
                .ThenInclude(area => area != null ? area.Installation : null)
                .Include(missionRun => missionRun.InspectionArea)
                .Include(missionRun => missionRun.Robot)
                .ThenInclude(robot => robot.Model)
                .Include(missionRun => missionRun.Tasks)
                .ThenInclude(task => task.Inspection)
                .Include(missionRun => missionRun.Robot)
                .ThenInclude(robot => robot.CurrentInstallation)
                .Where(m =>
                    accessibleInstallationCodes.Result.Contains(
                        m.InspectionArea.Installation.InstallationCode.ToUpper()
                    )
                )
                .Where(m => m.IsDeprecated == false);
            return readOnly ? query.AsNoTracking() : query.AsTracking();
        }

        protected virtual void OnMissionRunCreated(MissionRunCreatedEventArgs e)
        {
            MissionRunCreated?.Invoke(this, e);
        }

        public static event EventHandler<MissionRunCreatedEventArgs>? MissionRunCreated;

        private async Task ApplyDatabaseUpdate(Installation? installation)
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes();
            if (
                installation == null
                || accessibleInstallationCodes.Contains(
                    installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)
                )
            )
                await context.SaveChangesAsync();
            else
                throw new UnauthorizedAccessException(
                    $"User does not have permission to update mission run in installation {installation.Name}"
                );
        }

        private static void SearchByName(ref IQueryable<MissionRun> missionRuns, string? name)
        {
            if (!missionRuns.Any() || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

#pragma warning disable CA1862
            missionRuns = missionRuns.Where(missionRun =>
                missionRun.Name != null && missionRun.Name.ToLower().Contains(name.ToLower().Trim())
            );
#pragma warning restore CA1862
        }

        private static void SearchByRobotName(
            ref IQueryable<MissionRun> missionRuns,
            string? robotName
        )
        {
            if (!missionRuns.Any() || string.IsNullOrWhiteSpace(robotName))
            {
                return;
            }

#pragma warning disable CA1862
            missionRuns = missionRuns.Where(missionRun =>
                missionRun.Robot.Name.ToLower().Contains(robotName.ToLower().Trim())
            );
#pragma warning restore CA1862
        }

        private static void SearchByTag(ref IQueryable<MissionRun> missionRuns, string? tag)
        {
            if (!missionRuns.Any() || string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            missionRuns = missionRuns.Where(missionRun =>
                missionRun.Tasks.Any(task =>
#pragma warning disable CA1307
                    task.TagId != null && task.TagId.Contains(tag.Trim())
#pragma warning restore CA1307
                )
            );
        }

        /// <summary>
        ///     Filters by <see cref="MissionRunQueryStringParameters.InstallationCode" />,
        ///     <see cref="MissionRunQueryStringParameters.InspectionArea" />,
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
            Expression<Func<MissionRun, bool>> inspectionAreaFilter = parameters.InspectionArea
                is null
                ? missionRun => true
                : missionRun =>
                    missionRun.InspectionArea != null
                    && missionRun
                        .InspectionArea.Name.ToLower()
                        .Equals(parameters.InspectionArea.Trim().ToLower());

            Expression<Func<MissionRun, bool>> installationFilter = parameters.InstallationCode
                is null
                ? missionRun => true
                : missionRun =>
                    missionRun
                        .InstallationCode.ToLower()
                        .Equals(parameters.InstallationCode.Trim().ToLower());

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
                : missionRun =>
                    missionRun.MissionId != null
                    && missionRun.MissionId.Equals(parameters.MissionId);

            Expression<Func<MissionRun, bool>> missionTypeFilter = parameters.MissionRunType is null
                ? missionRun => true
                : missionRun => missionRun.MissionRunType.Equals(parameters.MissionRunType);

            Expression<Func<MissionRun, bool>> inspectionTypeFilter = parameters.InspectionTypes
                is null
                ? mission => true
                : mission =>
                    mission.Tasks.Any(task =>
                        task.Inspection != null
                        && parameters.InspectionTypes.Contains(task.Inspection.InspectionType)
                    );

            var minStartTime = DateTimeUtilities.UnixTimeStampToDateTime(parameters.MinStartTime);
            var maxStartTime = DateTimeUtilities.UnixTimeStampToDateTime(parameters.MaxStartTime);
            Expression<Func<MissionRun, bool>> startTimeFilter = missionRun =>
                missionRun.StartTime == null
                || (
                    DateTime.Compare(missionRun.StartTime.Value, minStartTime) >= 0
                    && DateTime.Compare(missionRun.StartTime.Value, maxStartTime) <= 0
                );

            var minEndTime = DateTimeUtilities.UnixTimeStampToDateTime(parameters.MinEndTime);
            var maxEndTime = DateTimeUtilities.UnixTimeStampToDateTime(parameters.MaxEndTime);
            Expression<Func<MissionRun, bool>> endTimeFilter = missionRun =>
                missionRun.EndTime == null
                || (
                    DateTime.Compare(missionRun.EndTime.Value, minEndTime) >= 0
                    && DateTime.Compare(missionRun.EndTime.Value, maxEndTime) <= 0
                );

            var minDesiredStartTime = DateTimeUtilities.UnixTimeStampToDateTime(
                parameters.MinDesiredStartTime
            );
            var maxDesiredStartTime = DateTimeUtilities.UnixTimeStampToDateTime(
                parameters.MaxDesiredStartTime
            );
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
                                        Expression.Invoke(desiredStartTimeFilter, missionRun),
                                        Expression.AndAlso(
                                            Expression.Invoke(startTimeFilter, missionRun),
                                            Expression.AndAlso(
                                                Expression.Invoke(endTimeFilter, missionRun),
                                                Expression.AndAlso(
                                                    Expression.Invoke(robotTypeFilter, missionRun),
                                                    Expression.Invoke(
                                                        inspectionAreaFilter,
                                                        missionRun
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

        public async Task<MissionRun?> ReadByIsarMissionId(
            string isarMissionId,
            bool readOnly = true
        )
        {
            return await GetMissionRunsWithSubModels(readOnly: readOnly)
                .FirstOrDefaultAsync(missionRun =>
                    missionRun.IsarMissionId != null
                    && missionRun.IsarMissionId.Equals(isarMissionId)
                );
        }

        public async Task<MissionRun> UpdateMissionRunType(
            string missionRunId,
            MissionRunType missionRunType
        )
        {
            var missionRun = await ReadById(missionRunId, readOnly: true);
            if (missionRun is null)
            {
                string errorMessage = $"Mission with mission Id {missionRunId} was not found";
                logger.LogError("{Message}", errorMessage);
                throw new MissionRunNotFoundException(errorMessage);
            }

            return await UpdateMissionRunProperty(missionRun.Id, "MissionRunType", missionRunType);
        }

        public async Task<MissionRun> UpdateMissionRunStatusByIsarMissionId(
            string isarMissionId,
            MissionStatus missionStatus,
            string? errorDescription = null
        )
        {
            var missionRun = await ReadByIsarMissionId(isarMissionId, readOnly: true);
            if (missionRun is null)
            {
                string errorMessage = $"Mission with isar mission Id {isarMissionId} was not found";
                logger.LogError("{Message}", errorMessage);
                throw new MissionRunNotFoundException(errorMessage);
            }

            missionRun.Status = missionStatus;

            missionRun = await UpdateMissionRunProperty(missionRun.Id, "Status", missionStatus);

            if (missionRun.Status == MissionStatus.Failed)
            {
                if (errorDescription is not null)
                {
                    missionRun = await UpdateMissionRunProperty(
                        missionRun.Id,
                        "StatusReason",
                        errorDescription
                    );
                }

                _ = signalRService.SendMessageAsync(
                    "Mission run failed",
                    missionRun?.InspectionArea.Installation,
                    missionRun != null ? new MissionRunResponse(missionRun) : null
                );
            }
            return missionRun!;
        }

        #endregion ISAR Specific methods


        public async Task<MissionRun> UpdateMissionRunProperty(
            string missionRunId,
            string propertyName,
            object? value
        )
        {
            var missionRun = await ReadById(missionRunId, readOnly: true);
            if (missionRun is null)
            {
                string errorMessage =
                    $"Mission with ID {missionRunId} was not found in the database";
                logger.LogError("{Message}", errorMessage);
                throw new MissionRunNotFoundException(errorMessage);
            }

            foreach (var property in typeof(MissionRun).GetProperties())
            {
                if (property.Name == propertyName)
                {
                    logger.LogDebug(
                        "Setting {missionRunName} field {propertyName} from {oldValue} to {NewValue}",
                        missionRun.Name,
                        propertyName,
                        property.GetValue(missionRun),
                        value
                    );
                    property.SetValue(missionRun, value);
                }
            }

            try
            {
                await Update(missionRun);
            }
            catch (InvalidOperationException e)
            {
                logger.LogError(e, "Failed to update {missionRunName}", missionRun.Name);
            }
            ;
            return missionRun;
        }

        public async Task UpdateCurrentRobotMissionToFailed(string robotId)
        {
            var robot =
                await robotService.ReadById(robotId, readOnly: true)
                ?? throw new RobotNotFoundException(
                    $"Robot with ID: {robotId} was not found in the database"
                );
            if (robot.CurrentMissionId != null)
            {
                try
                {
                    await SetMissionRunToFailed(
                        robot.CurrentMissionId,
                        "Lost connection to ISAR during mission"
                    );
                }
                catch (MissionRunNotFoundException)
                {
                    logger.LogError(
                        "Mission '{MissionId}' could not be set to failed as it no longer exists",
                        robot.CurrentMissionId
                    );
                }
                logger.LogWarning(
                    "Mission '{Id}' failed because ISAR could not be reached",
                    robot.CurrentMissionId
                );
            }
        }

        public async Task<MissionRun> SetMissionRunToFailed(
            string missionRunId,
            string failureDescription
        )
        {
            var missionRun =
                await ReadById(missionRunId, readOnly: true)
                ?? throw new MissionRunNotFoundException(
                    $"Could not find mission run with ID {missionRunId}"
                );

            missionRun.Status = MissionStatus.Failed;
            missionRun.StatusReason = failureDescription;
            foreach (var task in missionRun.Tasks.Where(task => !task.IsCompleted))
            {
                task.Status = Database.Models.TaskStatus.Failed;
            }

            _ = signalRService.SendMessageAsync(
                "Mission run failed",
                missionRun.InspectionArea.Installation,
                new MissionRunResponse(missionRun)
            );

            await Update(missionRun);
            return missionRun;
        }

        public void DetachTracking(FlotillaDbContext context, MissionRun missionRun)
        {
            context.Entry(missionRun).State = EntityState.Detached;
            foreach (var task in missionRun.Tasks)
            {
                if (context.Entry(task).State != EntityState.Detached)
                    missionTaskService.DetachTracking(context, task);
            }
            if (
                missionRun.InspectionArea != null
                && context.Entry(missionRun.InspectionArea).State != EntityState.Detached
            )
                inspectionAreaService.DetachTracking(context, missionRun.InspectionArea);
            if (
                missionRun.Robot != null
                && context.Entry(missionRun.Robot).State != EntityState.Detached
            )
                robotService.DetachTracking(context, missionRun.Robot);
        }

        public async Task<MissionRun> UpdateWithIsarInfo(
            string missionRunId,
            IsarMission isarMission
        )
        {
            var missionRun =
                await ReadById(missionRunId, readOnly: true)
                ?? throw new MissionRunNotFoundException(
                    $"Could not find mission run with ID {missionRunId}"
                );

            missionRun.IsarMissionId = isarMission.IsarMissionId;
            foreach (var isarTask in isarMission.Tasks)
            {
                var task = missionRun.GetTaskById(isarTask.IsarTaskId);
                task?.UpdateWithIsarInfo(isarTask);
            }
            await UpdateWithInspections(missionRun);
            return missionRun;
        }
    }
}
