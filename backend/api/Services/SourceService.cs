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
    public interface ISourceService
    {
        public abstract Task<PagedList<Source>> ReadAll(SourceQueryStringParameters? parameters);

        public abstract Task<SourceResponse?> ReadByIdAndInstallationWithTasks(string id, string installationCode);

        public abstract Task<SourceResponse?> ReadByIdWithTasks(string id);

        public abstract Task<Source?> ReadById(string id);

        public abstract Task<Source?> CheckForExistingEchoSource(int echoId);

        public abstract Task<Source?> CheckForExistingCustomSource(IList<MissionTask> tasks);

        public abstract Task<Source> Update(Source source);

        public abstract Task<Source?> Delete(string id);

    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class SourceService : ISourceService
    {
        private readonly FlotillaDbContext _context;
        private readonly ICustomMissionService _customMissionService;
        private readonly IEchoService _echoService;
        private readonly IStidService _stidService;

        public SourceService(
            FlotillaDbContext context,
            ICustomMissionService customMissionService,
            IEchoService echoService,
            IStidService stidService
        )
        {
            _context = context;
            _customMissionService = customMissionService;
            _echoService = echoService;
            _stidService = stidService;
        }

        public async Task<PagedList<Source>> ReadAll(SourceQueryStringParameters? parameters)
        {
            var query = GetSources();
            parameters ??= new SourceQueryStringParameters { };
            var filter = ConstructFilter(parameters);

            query = query.Where(filter);

            return await PagedList<Source>.ToPagedListAsync(
                query,
                parameters.PageNumber,
                parameters.PageSize
            );
        }

        private IQueryable<Source> GetSources()
        {
            return _context.Sources;
        }

        public async Task<Source?> ReadById(string id)
        {
            return await GetSources()
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<Source?> ReadBySourceId(string sourceId)
        {
            return await GetSources()
                .FirstOrDefaultAsync(a => a.SourceId.Equals(sourceId));
        }

        public async Task<SourceResponse?> ReadByIdAndInstallationWithTasks(string id, string installationCode)
        {
            var source = await GetSources()
                .FirstOrDefaultAsync(s => s.Id.Equals(id));
            if (source == null) return null;

            switch (source.Type)
            {
                case MissionSourceType.Custom:
                    throw new ArgumentException("Source is not of type Echo");
                case MissionSourceType.Echo:
                    var mission = await _echoService.GetMissionById(int.Parse(source.SourceId, new CultureInfo("en-US")));
                    var tasks = mission.Tags.Select(t =>
                    {
                        var tagPosition = _stidService
                            .GetTagPosition(t.TagId, installationCode)
                            .Result;
                        return new MissionTask(t, tagPosition);
                    }).ToList();
                    return new SourceResponse(source, tasks);
                default:
                    return null;
            }
        }

        public async Task<SourceResponse?> ReadByIdWithTasks(string id)
        {
            var source = await GetSources()
                .FirstOrDefaultAsync(s => s.Id.Equals(id));
            if (source == null) return null;

            switch (source.Type)
            {
                case MissionSourceType.Custom:
                    var tasks = await _customMissionService.GetMissionTasksFromSourceId(source.SourceId);
                    if (tasks == null) return null;
                    return new SourceResponse(source, tasks);
                case MissionSourceType.Echo:
                    throw new ArgumentException("Source is not of type Custom");
                default:
                    return null;
            }
        }

        public async Task<Source?> CheckForExistingEchoSource(int echoId)
        {
            return await ReadBySourceId(echoId.ToString());
        }

        public async Task<Source?> CheckForExistingCustomSource(IList<MissionTask> tasks)
        {
            string hash = _customMissionService.CalculateHashFromTasks(tasks);
            return await ReadBySourceId(hash);
        }

        public async Task<Source> Update(Source source)
        {
            var entry = _context.Update(source);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<Source?> Delete(string id)
        {
            var source = await GetSources()
                .FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (source is null)
            {
                return null;
            }

            _context.Sources.Remove(source);
            await _context.SaveChangesAsync();

            return source;
        }

        private static Expression<Func<Source, bool>> ConstructFilter(
            SourceQueryStringParameters parameters
        )
        {
            Expression<Func<Source, bool>> missionTypeFilter = parameters.Type is null
                ? source => true
                : source =>
                    source.Type == parameters.Type;

            // The parameter of the filter expression
            var sourceExpression = Expression.Parameter(typeof(Source));

            // Combining the body of the filters to create the combined filter, using invoke to force parameter substitution
            Expression body = Expression.Invoke(missionTypeFilter, sourceExpression);

            // Constructing the resulting lambda expression by combining parameter and body
            return Expression.Lambda<Func<Source, bool>>(body, sourceExpression);
        }
    }
}
