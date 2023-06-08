using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IMissionDefinitionService
    {
        public abstract Task<MissionDefinition> Create(MissionDefinition mission);

        public abstract Task<MissionDefinition?> ReadById(string id);

        public abstract Task<PagedList<MissionDefinition>> ReadAll(MissionDefinitionQueryStringParameters parameters);

        public abstract Task<MissionDefinition> Update(MissionDefinition mission);

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
        private readonly ILogger<MissionDefinitionService> _logger;

        public MissionDefinitionService(FlotillaDbContext context, ILogger<MissionDefinitionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MissionDefinition> Create(MissionDefinition mission)
        {
            await _context.MissionDefinitions.AddAsync(mission);
            await _context.SaveChangesAsync();

            return mission;
        }

        private IQueryable<MissionDefinition> GetMissionsWithSubModels()
        {
            return _context.MissionDefinitions
                .Include(mission => mission.Area)
                .ThenInclude(robot => robot.Deck)
                .ThenInclude(robot => robot.Installation)
                .ThenInclude(robot => robot.Asset)
                .Include(mission => mission.Source)
                .Include(mission => mission.LastRun)
                .ThenInclude(planTask => planTask == null ? null : planTask.StartTime);
        }

        public async Task<MissionDefinition?> ReadById(string id)
        {
            return await GetMissionsWithSubModels()
                .FirstOrDefaultAsync(mission => mission.Id.Equals(id));
        }

        public async Task<PagedList<MissionDefinition>> ReadAll(MissionDefinitionQueryStringParameters parameters)
        {
            var query = GetMissionsWithSubModels();
            var filter = ConstructFilter(parameters);

            query = query.Where(filter);

            SearchByName(ref query, parameters.NameSearch);

            ApplySort(ref query, parameters.OrderBy);

            return await PagedList<MissionDefinition>.ToPagedListAsync(
                query,
                parameters.PageNumber,
                parameters.PageSize
            );
        }

        public async Task<MissionDefinition> Update(MissionDefinition mission)
        {
            var entry = _context.Update(mission);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<MissionDefinition?> Delete(string id)
        {
            // We do not delete the source here as more than one mission definition may be using it
            var mission = await ReadById(id);
            if (mission is null)
            {
                return null;
            }

            _context.MissionDefinitions.Remove(mission);
            await _context.SaveChangesAsync();

            return mission;
        }

        private static void SearchByName(ref IQueryable<MissionDefinition> missions, string? name)
        {
            if (!missions.Any() || string.IsNullOrWhiteSpace(name))
                return;

            missions = missions.Where(
                mission =>
                    mission.Name != null && mission.Name.ToLower().Contains(name.Trim().ToLower())
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
                ? mission => true
                : mission =>
                      mission.Area.Name.ToLower().Equals(parameters.Area.Trim().ToLower());

            Expression<Func<MissionDefinition, bool>> assetFilter = parameters.AssetCode is null
                ? mission => true
                : mission =>
                      mission.AssetCode.ToLower().Equals(parameters.AssetCode.Trim().ToLower());

            Expression<Func<MissionDefinition, bool>> missionTypeFilter = parameters.SourceType is null
                ? mission => true
                : mission =>
                      mission.Source.Type.Equals(parameters.SourceType);

            // The parameter of the filter expression
            var mission = Expression.Parameter(typeof(MissionRun));

            // Combining the body of the filters to create the combined filter, using invoke to force parameter substitution
            Expression body = Expression.AndAlso(
                Expression.Invoke(assetFilter, mission),
                Expression.AndAlso(
                    Expression.Invoke(areaFilter, mission),
                    Expression.Invoke(missionTypeFilter, mission)
                    )
                );

            // Constructing the resulting lambda expression by combining parameter and body
            return Expression.Lambda<Func<MissionDefinition, bool>>(body, mission);
        }

        private static void ApplySort(ref IQueryable<MissionDefinition> missions, string orderByQueryString)
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

            var propertyInfos = typeof(MissionDefinition).GetProperties(
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
