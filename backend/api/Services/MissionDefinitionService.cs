using System.Globalization;
using System.Linq.Dynamic.Core;
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
        public abstract Task<MissionDefinition> Create(MissionDefinition missionDefinition);

        public abstract Task<MissionDefinition?> ReadById(string id);

        public abstract Task<PagedList<MissionDefinition>> ReadAll(MissionDefinitionQueryStringParameters parameters);
        public abstract Task<List<MissionDefinition>> ReadMissionDefinitionsBySourceId(string sourceId);

        public abstract Task<List<MissionDefinition>> ReadByAreaId(string areaId);

        public abstract Task<List<MissionDefinition>> ReadByDeckId(string deckId);

        public abstract Task<List<MissionTask>?> GetTasksFromSource(Source source, string installationCodes);

        public abstract Task<List<MissionDefinition>> ReadBySourceId(string sourceId);

        public abstract Task<MissionDefinition> Update(MissionDefinition missionDefinition);

        public abstract Task<MissionDefinition?> UpdateLastRun(string missionId, MissionRun missionRun);

        public abstract Task<MissionDefinition?> Delete(string id);
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
    public class MissionDefinitionService : IMissionDefinitionService
    {
        private readonly FlotillaDbContext _context;
        private readonly IEchoService _echoService;
        private readonly IStidService _stidService;
        private readonly ICustomMissionService _customMissionService;
        private readonly ILogger<IMissionDefinitionService> _logger;

        public MissionDefinitionService(
            FlotillaDbContext context,
            IEchoService echoService,
            IStidService stidService,
            ICustomMissionService customMissionService,
            ILogger<IMissionDefinitionService> logger)
        {
            _context = context;
            _echoService = echoService;
            _stidService = stidService;
            _customMissionService = customMissionService;
            _logger = logger;
        }

        public async Task<MissionDefinition> Create(MissionDefinition missionDefinition)
        {
            await _context.MissionDefinitions.AddAsync(missionDefinition);
            await _context.SaveChangesAsync();

            return missionDefinition;
        }

        private IQueryable<MissionDefinition> GetMissionDefinitionsWithSubModels()
        {
            return _context.MissionDefinitions
                .Include(missionDefinition => missionDefinition.Area != null ? missionDefinition.Area.Deck : null)
                .ThenInclude(deck => deck != null ? deck.Plant : null)
                .ThenInclude(plant => plant != null ? plant.Installation : null)
                .Include(missionDefinition => missionDefinition.Source)
                .Include(missionDefinition => missionDefinition.LastRun);
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

        public async Task<MissionDefinition> Update(MissionDefinition missionDefinition)
        {
            var entry = _context.Update(missionDefinition);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<MissionDefinition?> UpdateLastRun(string missionId, MissionRun missionRun)
        {
            var missionDefinition = await ReadById(missionId);
            if (missionDefinition == null)
            {
                return null;
            }
            missionDefinition.LastRun = missionRun;
            return await Update(missionDefinition);
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
            await _context.SaveChangesAsync();

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
                        _echoService.GetMissionById(
                                int.Parse(source.SourceId, new CultureInfo("en-US"))
                            ).Result.Tags
                            .Select(
                                t =>
                                {
                                    var tagPosition = _stidService
                                        .GetTagPosition(t.TagId, installationCode)
                                        .Result;
                                    return new MissionTask(t, tagPosition);
                                }
                            )
                            .ToList(),
                    MissionSourceType.Custom =>
                        await _customMissionService.GetMissionTasksFromSourceId(source.SourceId),
                    _ =>
                        throw new MissionSourceTypeException($"Mission type {source.Type} is not accounted for")
                };
            }
            catch (FormatException e)
            {
                _logger.LogError("Echo source ID was not formatted correctly.", e);
                throw new FormatException("Echo source ID was not formatted correctly");
            }
        }

        private static void SearchByName(ref IQueryable<MissionDefinition> missionDefinitions, string? name)
        {
            if (!missionDefinitions.Any() || string.IsNullOrWhiteSpace(name))
                return;

            missionDefinitions = missionDefinitions.Where(
                missionDefinition =>
                    missionDefinition.Name != null && missionDefinition.Name.ToLower().Contains(name.Trim().ToLower())
            );
        }

        /// <summary>
        /// Filters by <see cref="MissionDefinitionQueryStringParameters.InstallationCode"/>
        /// and <see cref="MissionDefinitionQueryStringParameters.Area"/>
        /// and <see cref="MissionDefinitionQueryStringParameters.NameSearch" />
        /// and <see cref="MissionDefinitionQueryStringParameters.SourceType" />
        ///
        /// <para>Uses LINQ Expression trees (see <seealso href="https://docs.microsoft.com/en-us/dotnet/csharp/expression-trees"/>)</para>
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
