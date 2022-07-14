using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IRobotService
    {
        public abstract Task<Robot> Create(Robot newRobot);
        public abstract Task<IEnumerable<Robot>> ReadAll();
        public abstract Task<Robot?> ReadById(string id);
        public abstract Task<Robot?> ReadByName(string name);
        public abstract Task<Robot> Update(Robot robot);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1309:Use ordinal StringComparison",
    Justification = "EF Core refrains from translating string comparison overloads to SQL")]
    public class RobotService : IRobotService
    {
        private readonly FlotillaDbContext _context;

        public RobotService(FlotillaDbContext context)
        {
            _context = context;
        }

        public async Task<Robot> Create(Robot newRobot)
        {
            await _context.Robots.AddAsync(newRobot);
            await _context.SaveChangesAsync();
            return newRobot;
        }

        public async Task<IEnumerable<Robot>> ReadAll()
        {
            return await _context.Robots.Include(r => r.VideoStreams).ToListAsync();
        }

        public async Task<Robot?> ReadById(string id)
        {
            return await _context.Robots
                .Include(r => r.VideoStreams)
                .FirstOrDefaultAsync(robot => robot.Id.Equals(id));
        }

        public async Task<Robot?> ReadByName(string name)
        {
            return await _context.Robots
                .Include(r => r.VideoStreams)
                .FirstOrDefaultAsync(robot => robot.Name.Equals(name));
        }

        public async Task<Robot> Update(Robot robot)
        {
            var entry = _context.Update(robot);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }
    }
}
