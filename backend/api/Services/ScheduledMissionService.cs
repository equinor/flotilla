using Api.Context;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public class ScheduledMissionService
    {
        private readonly FlotillaDbContext _context;

        public ScheduledMissionService(FlotillaDbContext context)
        {
            _context = context;
        }

        public async Task<ScheduledMission> Create(ScheduledMission scheduledMission)
        {
            await _context.ScheduledMissions.AddAsync(scheduledMission);
            await _context.SaveChangesAsync();
            return scheduledMission;
        }

        public async Task<IEnumerable<ScheduledMission>> ReadAll()
        {
            return await _context.ScheduledMissions.ToListAsync();
        }

        public async Task<ScheduledMission?> Read(string id)
        {
            return await _context.ScheduledMissions.FirstOrDefaultAsync(
                ev => ev.Id.Equals(id, StringComparison.Ordinal)
            );
        }

        public async Task<ScheduledMission?> Delete(string id)
        {
            var scheduledMission = await _context.ScheduledMissions.FirstOrDefaultAsync(
                ev => ev.Id.Equals(id, StringComparison.Ordinal)
            );
            if (scheduledMission is null)
            {
                return null;
            }
            _context.ScheduledMissions.Remove(scheduledMission);
            await _context.SaveChangesAsync();

            return scheduledMission;
        }
    }
}
