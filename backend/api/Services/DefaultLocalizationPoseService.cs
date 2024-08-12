using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IDefaultLocalizationPoseService
    {
        public abstract Task<IEnumerable<DefaultLocalizationPose>> ReadAll(bool readOnly = false);

        public abstract Task<DefaultLocalizationPose?> ReadById(string id, bool readOnly = false);

        public abstract Task<DefaultLocalizationPose> Create(DefaultLocalizationPose defaultLocalizationPose);

        public abstract Task<DefaultLocalizationPose> Update(DefaultLocalizationPose defaultLocalizationPose);

        public abstract Task<DefaultLocalizationPose?> Delete(string id);

        public void DetachTracking(DefaultLocalizationPose defaultLocalizationPose);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class DefaultLocalizationPoseService(FlotillaDbContext context) : IDefaultLocalizationPoseService
    {
        public async Task<IEnumerable<DefaultLocalizationPose>> ReadAll(bool readOnly = false)
        {
            return await GetDefaultLocalizationPoses(readOnly: readOnly).ToListAsync();
        }

        private IQueryable<DefaultLocalizationPose> GetDefaultLocalizationPoses(bool readOnly = false)
        {
            return readOnly ? context.DefaultLocalizationPoses.AsNoTracking() : context.DefaultLocalizationPoses.AsTracking();
        }

        public async Task<DefaultLocalizationPose?> ReadById(string id, bool readOnly = false)
        {
            return await GetDefaultLocalizationPoses(readOnly: readOnly)
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<DefaultLocalizationPose> Create(DefaultLocalizationPose defaultLocalizationPose)
        {

            await context.DefaultLocalizationPoses.AddAsync(defaultLocalizationPose);
            await context.SaveChangesAsync();

            DetachTracking(defaultLocalizationPose);
            return defaultLocalizationPose;
        }

        public async Task<DefaultLocalizationPose> Update(DefaultLocalizationPose defaultLocalizationPose)
        {
            var entry = context.Update(defaultLocalizationPose);
            await context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<DefaultLocalizationPose?> Delete(string id)
        {
            var defaultLocalizationPose = await GetDefaultLocalizationPoses()
                .FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (defaultLocalizationPose is null)
            {
                return null;
            }

            context.DefaultLocalizationPoses.Remove(defaultLocalizationPose);
            await context.SaveChangesAsync();

            return defaultLocalizationPose;
        }

        public void DetachTracking(DefaultLocalizationPose defaultLocalizationPose)
        {
            context.Entry(defaultLocalizationPose).State = EntityState.Detached;
        }
    }
}
