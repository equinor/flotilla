using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IScheduledMissionService
    {
        public abstract Task<ScheduledMission> Create(ScheduledMission scheduledMission);

        public abstract Task<IEnumerable<ScheduledMission>> ReadAll();

        public abstract Task<ScheduledMission?> ReadById(string id);

        public abstract Task<List<ScheduledMission>> ReadByStatus(ScheduledMissionStatus status);

        public abstract void Update(ScheduledMission scheduledMission);

        public abstract Task<ScheduledMission?> Delete(string id);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class ScheduledMissionService : IScheduledMissionService
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

        public async Task<ScheduledMission?> ReadById(string id)
        {
            return await _context.ScheduledMissions
                .Include(sm => sm.Robot)
                .FirstOrDefaultAsync(ev => ev.Id.Equals(id));
        }

        public async Task<List<ScheduledMission>> ReadByStatus(ScheduledMissionStatus status)
        {
            // EF Core cannot translate DateTimeOffset ordering to SQL,
            // so we need to do this on the client side (After getting list from database)
            var list = await _context.ScheduledMissions
                .Include(sm => sm.Robot)
                .Where(sm => sm.Status.Equals(status))
                .ToListAsync();
            return list.OrderBy(sm => sm.StartTime).ToList();
        }

        public void Update(ScheduledMission scheduledMission)
        {
            _context.ScheduledMissions.Update(scheduledMission);
            _context.SaveChanges();
            RaiseScheduledMissionUpdatedEvent();
        }

        public async Task<ScheduledMission?> Delete(string id)
        {
            var scheduledMission = await _context.ScheduledMissions.FirstOrDefaultAsync(
                ev => ev.Id.Equals(id)
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

        private void RaiseScheduledMissionUpdatedEvent()
        {
            if (ScheduledMissionUpdated is not null)
            {
                ScheduledMissionUpdated(this, new EventArgs());
            }
        }
    }
}
