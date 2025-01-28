using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services.MissionLoaders;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IMissionDefinitionService
    {
        public Task<MissionDefinition> Create(MissionDefinition missionDefinition);

        public Task<MissionDefinition?> ReadById(string id, bool readOnly = true);

        public Task<PagedList<MissionDefinition>> ReadAll(
            MissionDefinitionQueryStringParameters parameters,
            bool readOnly = true
        );

        public Task<List<MissionDefinition>> ReadByInspectionGroupId(
            string inspectionGroupId,
            bool readOnly = true
        );

        public Task<List<MissionTask>?> GetTasksFromSource(Source source);

        public Task<List<MissionDefinition>> ReadBySourceId(string sourceId, bool readOnly = true);

        public Task<MissionDefinition> UpdateLastSuccessfulMissionRun(
            string missionRunId,
            string missionDefinitionId
        );

        public Task<MissionDefinition> Update(MissionDefinition missionDefinition);

        public Task<MissionDefinition?> Delete(string id);

        public void DetachTracking(FlotillaDbContext context, MissionDefinition missionDefinition);
    }

    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class MissionDefinitionService(
        FlotillaDbContext context,
        IMissionLoader missionLoader,
        ISignalRService signalRService,
        IAccessRoleService accessRoleService,
        ILogger<IMissionDefinitionService> logger,
        IMissionRunService missionRunService,
        ISourceService sourceService
    ) : IMissionDefinitionService
    {
        public async Task<MissionDefinition> Create(MissionDefinition missionDefinition)
        {
            if (missionDefinition.LastSuccessfulRun is not null)
            {
                context.Entry(missionDefinition.LastSuccessfulRun).State = EntityState.Unchanged;
            }
            if (missionDefinition.InspectionGroups is not null)
            {
                foreach (var inspectionGroup in missionDefinition.InspectionGroups)
                {
                    context.Entry(inspectionGroup).State = EntityState.Unchanged;
                    // context.Entry(inspectionGroup.Installation).State = EntityState.Unchanged;
                }
            }
            if (missionDefinition.Source is not null)
            {
                context.Entry(missionDefinition.Source).State = EntityState.Unchanged;
            }

            await context.MissionDefinitions.AddAsync(missionDefinition);
            await ApplyDatabaseUpdate(missionDefinition.Installation);
            _ = signalRService.SendMessageAsync(
                "Mission definition created",
                missionDefinition.Installation,
                new MissionDefinitionResponse(missionDefinition)
            );
            DetachTracking(context, missionDefinition);
            return missionDefinition;
        }

        public async Task<MissionDefinition?> ReadById(string id, bool readOnly = true)
        {
            return await GetAccessibleMissionDefinitions(readOnly: readOnly)
                .Where(m => m.IsDeprecated == false)
                .FirstOrDefaultAsync(missionDefinition => missionDefinition.Id.Equals(id));
        }

        public async Task<PagedList<MissionDefinition>> ReadAll(
            MissionDefinitionQueryStringParameters parameters,
            bool readOnly = true
        )
        {
            var query = GetAccessibleMissionDefinitions(readOnly: readOnly)
                .Where(m => m.IsDeprecated == false);
            var filter = ConstructFilter(parameters);

            query = query.Where(filter);

            SearchByName(ref query, parameters.NameSearch);

            SortingService.ApplySort(ref query, parameters.OrderBy);

            return await PagedList<MissionDefinition>.ToPagedListAsync(
                query,
                parameters.PageNumber,
                parameters.PageSize
            );
        }

        public async Task<List<MissionDefinition>> ReadByInspectionGroupId(
            string inspectionGroupId,
            bool readOnly = true
        )
        {
            return await GetAccessibleMissionDefinitions(readOnly: readOnly)
                .Where(m =>
                    m.IsDeprecated == false
                    && m.InspectionGroups.Any(ig => ig.Id == inspectionGroupId)
                )
                .ToListAsync();
        }

        public async Task<List<MissionDefinition>> ReadBySourceId(
            string sourceId,
            bool readOnly = true
        )
        {
            return await GetAccessibleMissionDefinitions(readOnly: readOnly)
                .Where(m =>
                    m.IsDeprecated == false
                    && m.Source.SourceId != null
                    && m.Source.SourceId == sourceId
                )
                .ToListAsync();
        }

        public async Task<MissionDefinition> UpdateLastSuccessfulMissionRun(
            string missionRunId,
            string missionDefinitionId
        )
        {
            var missionRun = await missionRunService.ReadById(missionRunId, readOnly: true);
            if (missionRun is null)
            {
                string errorMessage = $"Mission run {missionRunId} was not found";
                logger.LogWarning("{Message}", errorMessage);
                throw new MissionNotFoundException(errorMessage);
            }
            var missionDefinition = await ReadById(missionDefinitionId, readOnly: true);
            if (missionDefinition == null)
            {
                string errorMessage = $"Mission definition {missionDefinitionId} was not found";
                logger.LogWarning("{Message}", errorMessage);
                throw new MissionNotFoundException(errorMessage);
            }
            if (missionRun.Status == MissionStatus.Successful)
            {
                missionDefinition.LastSuccessfulRun = missionRun;
                logger.LogInformation(
                    $"Updated last successful mission run on mission definition {missionDefinitionId} to mission run {missionRunId}"
                );
            }
            return await Update(missionDefinition);
        }

        public async Task<MissionDefinition> Update(MissionDefinition missionDefinition)
        {
            if (missionDefinition.LastSuccessfulRun is not null)
            {
                context.Entry(missionDefinition.LastSuccessfulRun).State = EntityState.Unchanged;
            }
            if (missionDefinition.Installation is not null)
            {
                context.Entry(missionDefinition.Installation).State = EntityState.Unchanged;
            }
            if (missionDefinition.InspectionGroups.Any())
            {
                // TODO Fix this to apply to lists
                foreach (var inspectionGroup in missionDefinition.InspectionGroups)
                {
                    context.Entry(inspectionGroup).State = EntityState.Unchanged;
                    // context.Entry(inspectionGroup.Installation).State = EntityState.Unchanged;
                }
            }

            var entry = context.Update(missionDefinition);
            await ApplyDatabaseUpdate(missionDefinition.Installation);
            _ = signalRService.SendMessageAsync(
                "Mission definition updated",
                missionDefinition?.Installation,
                missionDefinition != null ? new MissionDefinitionResponse(missionDefinition) : null
            );
            DetachTracking(context, missionDefinition!);
            return entry.Entity;
        }

        public async Task<MissionDefinition?> Delete(string id)
        {
            // We do not delete the source here as more than one mission definition may be using it
            var missionDefinition = await ReadById(id);
            if (missionDefinition is null)
            {
                return null;
            }

            missionDefinition.IsDeprecated = true;
            await ApplyDatabaseUpdate(missionDefinition.Installation);

            return missionDefinition;
        }

        public async Task<List<MissionTask>?> GetTasksFromSource(Source source)
        {
            return await missionLoader.GetTasksForMission(source.SourceId);
        }

        private async Task ApplyDatabaseUpdate(Installation? installation)
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes();
            if (
                installation == null
                || accessibleInstallationCodes.Contains(installation.InstallationCode)
            )
                await context.SaveChangesAsync();
            else
                throw new UnauthorizedAccessException(
                    $"User does not have permission to update mission definition in installation {installation.Name}"
                );
        }

        private IQueryable<MissionDefinition> GetMissionDefinitionsWithSubModels(
            bool readOnly = true
        )
        {
            var query = context
                .MissionDefinitions.Include(missionDefinition => missionDefinition.InspectionGroups)
                .ThenInclude(inspectionGroup => inspectionGroup!)
                .ThenInclude(plant => plant.Installation)
                .Include(missionDefinition => missionDefinition.InspectionGroups)
                .ThenInclude(Group => Group!.Installation)
                .Include(missionDefinition => missionDefinition.Source)
                .Include(missionDefinition => missionDefinition.LastSuccessfulRun)
                .ThenInclude(missionRun => missionRun != null ? missionRun.Tasks : null)!
                .ThenInclude(missionTask => missionTask.Inspection)
                .ThenInclude(inspection =>
                    inspection != null ? inspection.InspectionFindings : null
                )
                .Include(missionDefinition => missionDefinition.InspectionGroups)
                .ThenInclude(inspectionGroup => inspectionGroup!.DefaultLocalizationPose)
                .ThenInclude(defaultLocalizationPose =>
                    defaultLocalizationPose != null ? defaultLocalizationPose.Pose : null
                )
                .Include(missionDefinition => missionDefinition.Map);
            return readOnly ? query.AsNoTracking() : query.AsTracking();
        }

        private IQueryable<MissionDefinition> GetAccessibleMissionDefinitions(bool readOnly = true)
        {
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var query = GetMissionDefinitionsWithSubModels(readOnly);
            query = query.Where(missionDefinition =>
                missionDefinition.Installation != null
                && accessibleInstallationCodes.Result.Contains(
                    missionDefinition.Installation.InstallationCode
                )
            );
            return readOnly ? query.AsNoTracking() : query.AsTracking();
        }

        private static void SearchByName(
            ref IQueryable<MissionDefinition> missionDefinitions,
            string? name
        )
        {
            if (!missionDefinitions.Any() || string.IsNullOrWhiteSpace(name))
                return;

#pragma warning disable CA1862
            missionDefinitions = missionDefinitions.Where(missionDefinition =>
                missionDefinition.Name != null && missionDefinition.Name.Contains(name.Trim())
            );
#pragma warning restore CA1862
        }

        /// <summary>
        ///     Filters by <see cref="MissionDefinitionQueryStringParameters.InstallationCode" />
        ///     and <see cref="MissionDefinitionQueryStringParameters.InspectionGroups" />
        ///     and <see cref="MissionDefinitionQueryStringParameters.NameSearch" />
        ///     <para>
        ///         Uses LINQ Expression trees (see
        ///         <seealso href="https://docs.microsoft.com/en-us/dotnet/csharp/expression-trees" />)
        ///     </para>
        /// </summary>
        /// <param name="parameters"> The variable containing the filter params </param>
        private static Expression<Func<MissionDefinition, bool>> ConstructFilter(
            MissionDefinitionQueryStringParameters parameters
        )
        {
            Expression<Func<MissionDefinition, bool>> inspectionGroupFilter =
                parameters.InspectionGroups is null
                    ? missionDefinition => true
                    : missionDefinition =>
                        missionDefinition.InspectionGroups != null
                        && missionDefinition
                            .InspectionGroups.Select(group => group.Name.Trim().ToLowerInvariant())
                            .Any(groupName =>
                                parameters
                                    .InspectionGroups.Select(paramGroup =>
                                        paramGroup.Trim().ToLowerInvariant()
                                    )
                                    .Contains(groupName)
                            );

            Expression<Func<MissionDefinition, bool>> installationFilter =
                parameters.InstallationCode is null
                    ? missionDefinition => true
                    : missionDefinition =>
                        missionDefinition.Installation.InstallationCode.Equals(
                            parameters.InstallationCode.Trim(),
                            StringComparison.InvariantCultureIgnoreCase
                        );

            // The parameter of the filter expression
            var missionDefinitionExpression = Expression.Parameter(typeof(MissionDefinition));

            // Combining the body of the filters to create the combined filter, using invoke to force parameter substitution
            Expression body = Expression.AndAlso(
                Expression.Invoke(installationFilter, missionDefinitionExpression),
                Expression.Invoke(inspectionGroupFilter, missionDefinitionExpression)
            );

            // Constructing the resulting lambda expression by combining parameter and body
            return Expression.Lambda<Func<MissionDefinition, bool>>(
                body,
                missionDefinitionExpression
            );
        }

        public void DetachTracking(FlotillaDbContext context, MissionDefinition missionDefinition)
        {
            if (missionDefinition.LastSuccessfulRun != null)
                missionRunService.DetachTracking(context, missionDefinition.LastSuccessfulRun);
            if (missionDefinition.Source != null)
                sourceService.DetachTracking(context, missionDefinition.Source);
            context.Entry(missionDefinition).State = EntityState.Detached;
        }
    }
}
