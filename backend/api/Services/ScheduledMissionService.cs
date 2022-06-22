using Api.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public class ScheduledMissionService
    {
        private readonly FlotillaDbContext _context;
        public static event EventHandler? ScheduledMissionUpdated;

        public ScheduledMissionService(FlotillaDbContext context)
        {
            _context = context;
        }

        public async Task<ScheduledMission> Create(ScheduledMission scheduledMission)
        {
            await _context.ScheduledMissions.AddAsync(scheduledMission);
            await _context.SaveChangesAsync();
            RaiseScheduledMissionUpdatedEvent();
            return scheduledMission;
        }

        public async Task<IEnumerable<ScheduledMission>> ReadAll()
        {
            return await _context.ScheduledMissions.Include(sm => sm.Robot).ToListAsync();
        }

        public async Task<ScheduledMission?> Read(string id)
        {
            return await _context.ScheduledMissions.Include(sm => sm.Robot).FirstOrDefaultAsync(
                ev => ev.Id.Equals(id, StringComparison.Ordinal)
            );
        }

        public void Update(ScheduledMission scheduledMission)
        {
            _context.ScheduledMissions.Update(scheduledMission);
            _context.SaveChanges();
            RaiseScheduledMissionUpdatedEvent();
        }

        public async Task<List<ScheduledMission>> GetUpcomingScheduledMissions()
        {
            return await _context.ScheduledMissions
                .Include(sm => sm.Robot)
                .Where(sm => sm.Status.Equals(ScheduledMissionStatus.Pending))
                .OrderBy(sm => sm.StartTime)
                .ToListAsync();
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

            RaiseScheduledMissionUpdatedEvent();

            return scheduledMission;
        }

        public void RaiseScheduledMissionUpdatedEvent()
        {
            if (ScheduledMissionUpdated is not null)
            {
                ScheduledMissionUpdated(this, new EventArgs());
            }
        }
    }
}
