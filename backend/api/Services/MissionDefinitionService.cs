using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;
namespace Api.Services
{
    public interface IMissionDefinitionService
    {
        public Task<MissionDefinition> Create(MissionDefinition missionDefinition);

        public Task<MissionDefinition?> ReadById(string id);

        public Task<PagedList<MissionDefinition>> ReadAll(MissionDefinitionQueryStringParameters parameters);

        public Task<List<MissionDefinition>> ReadByAreaId(string areaId);

        public Task<List<MissionDefinition>> ReadByDeckId(string deckId);

        public Task<List<MissionTask>?> GetTasksFromSource(Source source, string installationCodes);

        public Task<List<MissionDefinition>> ReadBySourceId(string sourceId);

        public Task<MissionDefinition> UpdateLastSuccessfulMissionRun(string missionRunId, string missionDefinitionId);

        public Task<MissionDefinition> Update(MissionDefinition missionDefinition);

        public Task<MissionDefinition?> Delete(string id);
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
    public class MissionDefinitionService(FlotillaDbContext context,
            IEchoService echoService,
            ISourceService sourceService,
            ISignalRService signalRService,
            IAccessRoleService accessRoleService,
            ILogger<IMissionDefinitionService> logger,
            IMissionRunService missionRunService) : IMissionDefinitionService
    {
        public async Task<MissionDefinition> Create(MissionDefinition missionDefinition)
        {
            if (missionDefinition.LastSuccessfulRun is not null) { context.Entry(missionDefinition.LastSuccessfulRun).State = EntityState.Unchanged; }
            if (missionDefinition.Area is not null) { context.Entry(missionDefinition.Area).State = EntityState.Unchanged; }
            if (missionDefinition.Source is not null) { context.Entry(missionDefinition.Source).State = EntityState.Unchanged; }

            await context.MissionDefinitions.AddAsync(missionDefinition);
            await ApplyDatabaseUpdate(missionDefinition.Area?.Installation);
            _ = signalRService.SendMessageAsync("Mission definition created", missionDefinition.Area?.Installation, new CondensedMissionDefinitionResponse(missionDefinition));
            return missionDefinition;
        }

        public async Task<MissionDefinition?> ReadById(string id)
        {
            return await GetMissionDefinitionsWithSubModels().Where(m => m.IsDeprecated == false)
                .FirstOrDefaultAsync(missionDefinition => missionDefinition.Id.Equals(id));
        }

        public async Task<PagedList<MissionDefinition>> ReadAll(MissionDefinitionQueryStringParameters parameters)
        {
            var query = GetMissionDefinitionsWithSubModels().Where(m => m.IsDeprecated == false);
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

        public async Task<List<MissionDefinition>> ReadByAreaId(string areaId)
        {
            return await GetMissionDefinitionsWithSubModels().Where(
                m => m.IsDeprecated == false && m.Area != null && m.Area.Id == areaId).ToListAsync();
        }

        public async Task<List<MissionDefinition>> ReadBySourceId(string sourceId)
        {
            return await GetMissionDefinitionsWithSubModels().Where(
                m => m.IsDeprecated == false && m.Source.SourceId != null && m.Source.SourceId == sourceId).ToListAsync();
        }

        public async Task<List<MissionDefinition>> ReadByDeckId(string deckId)
        {
            return await GetMissionDefinitionsWithSubModels().Where(
                m => m.IsDeprecated == false && m.Area != null && m.Area.Deck != null && m.Area.Deck.Id == deckId).ToListAsync();
        }

        public async Task<MissionDefinition> UpdateLastSuccessfulMissionRun(string missionRunId, string missionDefinitionId)
        {
            var missionRun = await missionRunService.ReadById(missionRunId, readOnly: true);
            if (missionRun is null)
            {
                string errorMessage = $"Mission run {missionRunId} was not found";
                logger.LogWarning("{Message}", errorMessage);
                throw new MissionNotFoundException(errorMessage);
            }
            var missionDefinition = await ReadById(missionDefinitionId);
            if (missionDefinition == null)
            {
                string errorMessage = $"Mission definition {missionDefinitionId} was not found";
                logger.LogWarning("{Message}", errorMessage);
                throw new MissionNotFoundException(errorMessage);
            }
            if (missionRun.Status == MissionStatus.Successful)
            {
                missionDefinition.LastSuccessfulRun = missionRun;
                logger.LogInformation($"Updated last successful mission run on mission definition {missionDefinitionId} to mission run {missionRunId}");
            }
            return await Update(missionDefinition);
        }

        public async Task<MissionDefinition> Update(MissionDefinition missionDefinition)
        {
            if (missionDefinition.LastSuccessfulRun is not null) { context.Entry(missionDefinition.LastSuccessfulRun).State = EntityState.Unchanged; }
            if (missionDefinition.Area is not null) { context.Entry(missionDefinition.Area).State = EntityState.Unchanged; }

            var entry = context.Update(missionDefinition);
            await ApplyDatabaseUpdate(missionDefinition.Area?.Installation);
            _ = signalRService.SendMessageAsync("Mission definition updated", missionDefinition?.Area?.Installation, missionDefinition != null ? new CondensedMissionDefinitionResponse(missionDefinition) : null);
            return entry.Entity;
        }

        public async Task<MissionDefinition?> Delete(string id)
        {
            // We do not delete the source here as more than one mission definition may be using it
            var missionDefinition = await ReadById(id);
            if (missionDefinition is null) { return null; }

            missionDefinition.IsDeprecated = true;
            await ApplyDatabaseUpdate(missionDefinition.Area?.Installation);

            return missionDefinition;
        }

        public async Task<List<MissionTask>?> GetTasksFromSource(Source source, string installationCode)
        {
            try
            {
                return source.Type switch
                {
                    MissionSourceType.Echo =>
                        // CultureInfo is not important here since we are not using decimal points
                        echoService.GetMissionById(
                                int.Parse(source.SourceId, new CultureInfo("en-US"))
                            ).Result.Tags
                            .Select(t => new MissionTask(t))
                            .ToList(),
                    MissionSourceType.Custom =>
                        await sourceService.GetMissionTasksFromSourceId(source.SourceId),
                    _ =>
                        throw new MissionSourceTypeException($"Mission type {source.Type} is not accounted for")
                };
            }
            catch (FormatException)
            {
                logger.LogError("Echo source ID was not formatted correctly");
                throw new FormatException("Echo source ID was not formatted correctly");
            }
        }

        private async Task ApplyDatabaseUpdate(Installation? installation)
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes();
            if (installation == null || accessibleInstallationCodes.Contains(installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)))
                await context.SaveChangesAsync();
            else
                throw new UnauthorizedAccessException($"User does not have permission to update mission definition in installation {installation.Name}");

        }

        private IQueryable<MissionDefinition> GetMissionDefinitionsWithSubModels()
        {
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            return context.MissionDefinitions
                .Include(missionDefinition => missionDefinition.Area != null ? missionDefinition.Area.Deck : null)
                .ThenInclude(deck => deck != null ? deck.Plant : null)
                .ThenInclude(plant => plant != null ? plant.Installation : null)
                .Include(missionDefinition => missionDefinition.Area)
                .Include(missionDefinition => missionDefinition.Source)
                .Include(missionDefinition => missionDefinition.LastSuccessfulRun)
                .ThenInclude(missionRun => missionRun != null ? missionRun.Tasks : null)!
                .ThenInclude(missionTask => missionTask.Inspections)
                .ThenInclude(inspection => inspection.InspectionFindings)
                .Include(missionDefinition => missionDefinition.Area != null ? missionDefinition.Area.Deck : null)
                .ThenInclude(deck => deck != null ? deck.DefaultLocalizationPose : null)
                .ThenInclude(defaultLocalizationPose => defaultLocalizationPose != null ? defaultLocalizationPose.Pose : null)
                .Where((m) => m.Area == null || accessibleInstallationCodes.Result.Contains(m.Area.Installation.InstallationCode.ToUpper()));
        }

        private static void SearchByName(ref IQueryable<MissionDefinition> missionDefinitions, string? name)
        {
            if (!missionDefinitions.Any() || string.IsNullOrWhiteSpace(name))
                return;

            missionDefinitions = missionDefinitions.Where(
                missionDefinition =>
                    missionDefinition.Name != null && missionDefinition.Name.Contains(name.Trim(), StringComparison.OrdinalIgnoreCase)
            );
        }

        /// <summary>
        ///     Filters by <see cref="MissionDefinitionQueryStringParameters.InstallationCode" />
        ///     and <see cref="MissionDefinitionQueryStringParameters.Area" />
        ///     and <see cref="MissionDefinitionQueryStringParameters.NameSearch" />
        ///     and <see cref="MissionDefinitionQueryStringParameters.SourceType" />
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
            Expression<Func<MissionDefinition, bool>> areaFilter = parameters.Area is null
                ? missionDefinition => true
                : missionDefinition =>
                    missionDefinition.Area != null && missionDefinition.Area.Name.ToLower().Equals(parameters.Area.Trim().ToLower());

            Expression<Func<MissionDefinition, bool>> installationFilter = parameters.InstallationCode is null
                ? missionDefinition => true
                : missionDefinition =>
                    missionDefinition.InstallationCode.ToLower().Equals(parameters.InstallationCode.Trim().ToLower());

            Expression<Func<MissionDefinition, bool>> missionTypeFilter = parameters.SourceType is null
                ? missionDefinition => true
                : missionDefinition =>
                    missionDefinition.Source.Type.Equals(parameters.SourceType);

            // The parameter of the filter expression
            var missionDefinitionExpression = Expression.Parameter(typeof(MissionDefinition));

            // Combining the body of the filters to create the combined filter, using invoke to force parameter substitution
            Expression body = Expression.AndAlso(
                Expression.Invoke(installationFilter, missionDefinitionExpression),
                Expression.AndAlso(
                    Expression.Invoke(areaFilter, missionDefinitionExpression),
                    Expression.Invoke(missionTypeFilter, missionDefinitionExpression)
                )
            );

            // Constructing the resulting lambda expression by combining parameter and body
            return Expression.Lambda<Func<MissionDefinition, bool>>(body, missionDefinitionExpression);
        }
    }
}
