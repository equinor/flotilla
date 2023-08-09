using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface ILocalizationPoseService
    {
        public abstract Task<IEnumerable<LocalizationPose>> ReadAll();

        public abstract Task<LocalizationPose?> ReadById(string id);

        public abstract Task<LocalizationPose> Create(Pose newLocalizationPose);

        public abstract Task<LocalizationPose> Update(LocalizationPose localizationPose);

        public abstract Task<LocalizationPose?> Delete(string id);

    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class LocalizationPoseService : ILocalizationPoseService
    {
        private readonly FlotillaDbContext _context;

        public LocalizationPoseService(FlotillaDbContext context)
        {
            _context = context;

        }

        public async Task<IEnumerable<LocalizationPose>> ReadAll()
        {
            return await GetLocalizationPoses().ToListAsync();
        }

        private IQueryable<LocalizationPose> GetLocalizationPoses()
        {
            return _context.LocalizationPoses;
        }

        public async Task<LocalizationPose?> ReadById(string id)
        {
            return await GetLocalizationPoses()
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<LocalizationPose> Create(Pose newLocalizationPose)
        {

            var localizationPose = new LocalizationPose
            {
                Pose = newLocalizationPose
            };
            await _context.LocalizationPoses.AddAsync(localizationPose);
            await _context.SaveChangesAsync();
            return localizationPose!;
        }

        public async Task<LocalizationPose> Update(LocalizationPose localizationPose)
        {
            var entry = _context.Update(localizationPose);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<LocalizationPose?> Delete(string id)
        {
            var localizationPose = await GetLocalizationPoses()
                .FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (localizationPose is null)
            {
                return null;
            }

            _context.LocalizationPoses.Remove(localizationPose);
            await _context.SaveChangesAsync();

            return localizationPose;
        }
    }
}
