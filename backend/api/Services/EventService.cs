using Api.Context;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public class EventService
    {
        private readonly FlotillaDbContext _context;

        public EventService(FlotillaDbContext context)
        {
            _context = context;
        }
        public async Task<Event> Create(Event evnt)
        {
            await _context.Events.AddAsync(evnt);
            await _context.SaveChangesAsync();
            return evnt;
        }

        public async Task<IEnumerable<Event>> ReadAll()
        {
            return await _context.Events.ToListAsync();
        }

        public async Task<Event?> Read(string id)
        {
            return await _context.Events.FirstOrDefaultAsync(ev => ev.Id.Equals(id, StringComparison.Ordinal));
        }

        public async Task<Event?> Delete(string id)
        {
            var evnt = await _context.Events.FirstOrDefaultAsync(ev => ev.Id.Equals(id, StringComparison.Ordinal));
            if (evnt == null)
            {
                return null;
            }
            _context.Events.Remove(evnt);
            await _context.SaveChangesAsync();

            return evnt;
        }
    }
}
