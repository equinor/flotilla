using System.Text.Json;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface ISourceService
    {
        public Task<Source> Create(Source source);

        public Task<List<Source>> ReadAll(bool readOnly = true);

        public Task<Source?> ReadById(string id, bool readOnly = true);

        public Task<Source?> CheckForExistingSource(string sourceId);

        public Task<Source?> CheckForExistingSourceFromTasks(IList<MissionTask> tasks);

        public Task<Source> CreateSourceIfDoesNotExist(
            List<MissionTask> tasks,
            bool readOnly = true
        );

        public Task<List<MissionTask>?> GetMissionTasksFromSourceId(string id);

        public Task<Source?> Delete(string id);

        public void DetachTracking(FlotillaDbContext context, Source source);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class SourceService(FlotillaDbContext context, ILogger<SourceService> logger)
        : ISourceService
    {
        public async Task<Source> Create(Source source)
        {
            context.Sources.Add(source);
            await context.SaveChangesAsync();
            DetachTracking(context, source);
            return source;
        }

        public async Task<List<Source>> ReadAll(bool readOnly = true)
        {
            var query = GetSources(readOnly: readOnly);

            return await query.ToListAsync();
        }

        private IQueryable<Source> GetSources(bool readOnly = true)
        {
            return readOnly ? context.Sources.AsNoTracking() : context.Sources.AsTracking();
        }

        public async Task<Source?> ReadById(string id, bool readOnly = true)
        {
            return await GetSources(readOnly: readOnly).FirstOrDefaultAsync(s => s.Id.Equals(id));
        }

        public async Task<Source?> ReadBySourceId(string sourceId, bool readOnly = true)
        {
            return await GetSources(readOnly: readOnly)
                .FirstOrDefaultAsync(s => s.SourceId.Equals(sourceId));
        }

        public async Task<Source?> CheckForExistingSource(string sourceId)
        {
            return await ReadBySourceId(sourceId, readOnly: true);
        }

        public async Task<Source?> CheckForExistingSourceFromTasks(IList<MissionTask> tasks)
        {
            string hash = MissionTask.CalculateHashFromTasks(tasks);
            return await ReadBySourceId(hash, readOnly: true);
        }

        public async Task<List<MissionTask>?> GetMissionTasksFromSourceId(string id)
        {
            var existingSource = await ReadBySourceId(id, readOnly: true);
            if (existingSource == null || existingSource.CustomMissionTasks == null)
                return null;

            try
            {
                var content = JsonSerializer.Deserialize<List<MissionTask>>(
                    existingSource.CustomMissionTasks
                );

                if (content == null)
                    return null;

                foreach (var task in content)
                {
                    task.Id = Guid.NewGuid().ToString(); // This is needed as tasks are owned by mission runs and to update the tasks for the correct mission run
                }
                return content;
            }
            catch (Exception e)
            {
                logger.LogWarning(
                    "Unable to deserialize custom mission tasks with ID {Id}. {ErrorMessage}",
                    id,
                    e
                );
                return null;
            }
        }

        public async Task<Source> CreateSourceIfDoesNotExist(
            List<MissionTask> tasks,
            bool readOnly = true
        )
        {
            string json = JsonSerializer.Serialize(tasks);
            string hash = MissionTask.CalculateHashFromTasks(tasks);

            var existingSource = await ReadById(hash, readOnly: readOnly);

            if (existingSource != null)
                return existingSource;

            var newSource = await Create(new Source { SourceId = hash, CustomMissionTasks = json });

            DetachTracking(context, newSource);
            return newSource;
        }

        public async Task<Source?> Delete(string id)
        {
            var source = await GetSources().FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (source is null)
            {
                return null;
            }

            context.Sources.Remove(source);
            await context.SaveChangesAsync();

            return source;
        }

        public void DetachTracking(FlotillaDbContext context, Source source)
        {
            context.Entry(source).State = EntityState.Detached;
        }
    }
}
