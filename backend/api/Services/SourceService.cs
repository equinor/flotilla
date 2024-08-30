﻿using System.Text.Json;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface ISourceService
    {
        public abstract Task<Source> Create(Source source);

        public abstract Task<List<Source>> ReadAll();

        public abstract Task<Source?> ReadById(string id, bool readOnly = false);

        public abstract Task<Source?> CheckForExistingSource(string sourceId);

        public abstract Task<Source?> CheckForExistingSourceFromTasks(IList<MissionTask> tasks);

        public abstract Task<Source> CreateSourceIfDoesNotExist(List<MissionTask> tasks, bool readOnly = false);

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
        ILogger<SourceService> logger) : ISourceService
    {
        public async Task<Source> Create(Source source)
        {
            context.Sources.Add(source);
            await context.SaveChangesAsync();
            DetachTracking(source);
            return source;
        }

        public async Task<List<Source>> ReadAll()
        {
            var query = GetSources();

            return await query.ToListAsync();
        }

        private IQueryable<Source> GetSources(bool readOnly = false)
        {
            return readOnly ? context.Sources.AsNoTracking() : context.Sources.AsTracking();
        }

        public async Task<Source?> ReadById(string id, bool readOnly = false)
        {
            return await GetSources(readOnly: readOnly)
                .FirstOrDefaultAsync(s => s.Id.Equals(id));
        }

        public async Task<Source?> ReadBySourceId(string sourceId, bool readOnly = false)
        {
            return await GetSources(readOnly: readOnly)
                .FirstOrDefaultAsync(s => s.SourceId.Equals(sourceId));
        }

        public async Task<Source?> CheckForExistingSource(string sourceId)
        {
            return await ReadBySourceId(sourceId);
        }

        public async Task<Source?> CheckForExistingSourceFromTasks(IList<MissionTask> tasks)
        {
            string hash = MissionTask.CalculateHashFromTasks(tasks);
            return await ReadBySourceId(hash);
        }

        public async Task<List<MissionTask>?> GetMissionTasksFromSourceId(string id)
        {
            var existingSource = await ReadBySourceId(id, readOnly: true);
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

        public async Task<Source> CreateSourceIfDoesNotExist(List<MissionTask> tasks, bool readOnly = false)
        {
            string json = JsonSerializer.Serialize(tasks);
            string hash = MissionTask.CalculateHashFromTasks(tasks);

            var existingSource = await ReadById(hash, readOnly: readOnly);

            if (existingSource != null) return existingSource;

            var newSource = await Create(
                new Source
                {
                    SourceId = hash,
                    CustomMissionTasks = json
                }
            );

            return newSource;
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
    }
}
