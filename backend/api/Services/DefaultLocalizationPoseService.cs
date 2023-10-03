using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IDefaultLocalizationPoseService
    {
        public abstract Task<IEnumerable<DefaultLocalizationPose>> ReadAll();

        public abstract Task<DefaultLocalizationPose?> ReadById(string id);

        public abstract Task<DefaultLocalizationPose> Create(DefaultLocalizationPose defaultLocalizationPose);

        public abstract Task<DefaultLocalizationPose> Update(DefaultLocalizationPose defaultLocalizationPose);

        public abstract Task<DefaultLocalizationPose?> Delete(string id);

    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class DefaultLocalizationPoseService : IDefaultLocalizationPoseService
    {
        private readonly FlotillaDbContext _context;

        public DefaultLocalizationPoseService(FlotillaDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DefaultLocalizationPose>> ReadAll()
        {
            return await GetDefaultLocalizationPoses().ToListAsync();
        }

        private IQueryable<DefaultLocalizationPose> GetDefaultLocalizationPoses()
        {
            return _context.DefaultLocalizationPoses;
        }

        public async Task<DefaultLocalizationPose?> ReadById(string id)
        {
            return await GetDefaultLocalizationPoses()
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<DefaultLocalizationPose> Create(DefaultLocalizationPose defaultLocalizationPose)
        {

            await _context.DefaultLocalizationPoses.AddAsync(defaultLocalizationPose);
            await _context.SaveChangesAsync();

            return defaultLocalizationPose;
        }

        public async Task<DefaultLocalizationPose> Update(DefaultLocalizationPose defaultLocalizationPose)
        {
            var entry = _context.Update(defaultLocalizationPose);
            await _context.SaveChangesAsync();
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

            _context.DefaultLocalizationPoses.Remove(defaultLocalizationPose);
            await _context.SaveChangesAsync();

            return defaultLocalizationPose;
        }
    }
}
