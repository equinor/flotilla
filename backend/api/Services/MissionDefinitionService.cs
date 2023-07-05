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

        public abstract Task<MissionDefinition> Update(MissionDefinition missionDefinition);

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

        public MissionDefinitionService(FlotillaDbContext context)
        {
            _context = context;
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
                .Include(missionDefinition => missionDefinition.Area)
                .ThenInclude(area => area.Deck)
                .ThenInclude(area => area.Installation)
                .ThenInclude(area => area.Asset)
                .Include(missionDefinition => missionDefinition.Source)
                .Include(missionDefinition => missionDefinition.LastRun);
        }

        public async Task<MissionDefinition?> ReadById(string id)
        {
            return await GetMissionDefinitionsWithSubModels().Where(m => m.Deprecated == false)
                .FirstOrDefaultAsync(missionDefinition => missionDefinition.Id.Equals(id));
        }

        public async Task<PagedList<MissionDefinition>> ReadAll(MissionDefinitionQueryStringParameters parameters)
        {
            var query = GetMissionDefinitionsWithSubModels().Where(m => m.Deprecated == false);
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

        public async Task<MissionDefinition> Update(MissionDefinition missionDefinition)
        {
            var entry = _context.Update(missionDefinition);
            await _context.SaveChangesAsync();
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

            missionDefinition.Deprecated = true;
            await _context.SaveChangesAsync();

            return missionDefinition;
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
        /// Filters by <see cref="MissionDefinitionQueryStringParameters.AssetCode"/> and <see cref="MissionDefinitionQueryStringParameters.Status"/>
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
                      missionDefinition.Area.Name.ToLower().Equals(parameters.Area.Trim().ToLower());

            Expression<Func<MissionDefinition, bool>> assetFilter = parameters.AssetCode is null
                ? missionDefinition => true
                : missionDefinition =>
                      missionDefinition.AssetCode.ToLower().Equals(parameters.AssetCode.Trim().ToLower());

            Expression<Func<MissionDefinition, bool>> missionTypeFilter = parameters.SourceType is null
                ? missionDefinition => true
                : missionDefinition =>
                      missionDefinition.Source.Type.Equals(parameters.SourceType);

            // The parameter of the filter expression
            var missionRunExpression = Expression.Parameter(typeof(MissionRun));

            // Combining the body of the filters to create the combined filter, using invoke to force parameter substitution
            Expression body = Expression.AndAlso(
                Expression.Invoke(assetFilter, missionRunExpression),
                Expression.AndAlso(
                    Expression.Invoke(areaFilter, missionRunExpression),
                    Expression.Invoke(missionTypeFilter, missionRunExpression)
                    )
                );

            // Constructing the resulting lambda expression by combining parameter and body
            return Expression.Lambda<Func<MissionDefinition, bool>>(body, missionRunExpression);
        }
    }
}
