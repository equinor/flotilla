using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface ISourceService
    {
        public abstract Task<Source> Create(Source source);

        public abstract Task<PagedList<Source>> ReadAll(SourceQueryStringParameters? parameters);

        public abstract Task<SourceResponse?> ReadByIdAndInstallationWithTasks(string id, string installationCode);

        public abstract Task<SourceResponse?> ReadByIdWithTasks(string id);

        public abstract Task<Source?> ReadById(string id);

        public abstract Task<Source?> CheckForExistingEchoSource(int echoId);

        public abstract Task<Source?> CheckForExistingCustomSource(IList<MissionTask> tasks);

        public abstract Task<List<MissionTask>?> GetMissionTasksFromSourceId(string id);

        public abstract Task<Source> CreateSourceIfDoesNotExist(List<MissionTask> tasks);

        public abstract string CalculateHashFromTasks(IList<MissionTask> tasks);

        public abstract Task<Source> Update(Source source);

        public abstract Task<Source?> Delete(string id);

    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class SourceService(
        FlotillaDbContext context,
        IEchoService echoService,
        ILogger<SourceService> logger) : ISourceService
    {
        public async Task<Source> Create(Source source)
        {
            context.Sources.Add(source);
            await context.SaveChangesAsync();
            return source;
        }

        public async Task<PagedList<Source>> ReadAll(SourceQueryStringParameters? parameters)
        {
            var query = GetSources();
            parameters ??= new SourceQueryStringParameters { };
            var filter = ConstructFilter(parameters);

            var filteredQuery = query.Where(filter);

            return await PagedList<Source>.ToPagedListAsync(
                filteredQuery,
                parameters.PageNumber,
                parameters.PageSize
            );
        }

        private DbSet<Source> GetSources()
        {
            return context.Sources;
        }

        public async Task<Source?> ReadById(string id)
        {
            return await GetSources()
                .FirstOrDefaultAsync(s => s.Id.Equals(id));
        }

        public async Task<Source?> ReadBySourceId(string sourceId)
        {
            return await GetSources()
                .FirstOrDefaultAsync(s => s.SourceId.Equals(sourceId));
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
                    var mission = await echoService.GetMissionById(int.Parse(source.SourceId, new CultureInfo("en-US")));
                    var tasks = mission.Tags.Select(t =>
                    {
                        return new MissionTask(t);
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
                    var tasks = await GetMissionTasksFromSourceId(source.SourceId);
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
            return await ReadBySourceId(echoId.ToString(CultureInfo.CurrentCulture));
        }

        public async Task<Source?> CheckForExistingCustomSource(IList<MissionTask> tasks)
        {
            string hash = CalculateHashFromTasks(tasks);
            return await ReadBySourceId(hash);
        }

        public async Task<List<MissionTask>?> GetMissionTasksFromSourceId(string id)
        {
            var existingSource = await ReadBySourceId(id);
            if (existingSource == null || existingSource.CustomMissionTasks == null) return null;

            try
            {
                var content = JsonSerializer.Deserialize<List<MissionTask>>(existingSource.CustomMissionTasks);

                if (content == null) return null;

                foreach (var task in content)
                {
                    task.Id = Guid.NewGuid().ToString(); // This is needed as tasks are owned by mission runs
                    task.IsarTaskId = Guid.NewGuid().ToString(); // This is needed to update the tasks for the correct mission run
                }
                return content;
            }
            catch (Exception e)
            {
                logger.LogWarning("Unable to deserialize custom mission tasks with ID {Id}. {ErrorMessage}", id, e);
                return null;
            }
        }

        public async Task<Source> CreateSourceIfDoesNotExist(List<MissionTask> tasks)
        {
            string json = JsonSerializer.Serialize(tasks);
            string hash = CalculateHashFromTasks(tasks);

            var existingSource = await ReadById(hash);

            if (existingSource != null) return existingSource;

            var newSource = await Create(
                new Source
                {
                    SourceId = hash,
                    Type = MissionSourceType.Custom,
                    CustomMissionTasks = json
                }
            );

            return newSource;
        }

        public string CalculateHashFromTasks(IList<MissionTask> tasks)
        {
            var genericTasks = new List<MissionTask>();
            foreach (var task in tasks)
            {
                var taskCopy = new MissionTask(task)
                {
                    Id = "",
                    IsarTaskId = ""
                };
                taskCopy.Inspections = taskCopy.Inspections.Select(i => new Inspection(i, useEmptyIDs: true)).ToList();
                genericTasks.Add(taskCopy);
            }

            string json = JsonSerializer.Serialize(genericTasks);
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            return BitConverter.ToString(hash).Replace("-", "", StringComparison.CurrentCulture).ToUpperInvariant();
        }

        public async Task<Source> Update(Source source)
        {
            var entry = context.Update(source);
            await context.SaveChangesAsync();
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

            context.Sources.Remove(source);
            await context.SaveChangesAsync();

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
