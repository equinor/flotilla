using Api.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public class RobotService
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
                .FirstOrDefaultAsync(robot => robot.Id.Equals(id, StringComparison.Ordinal));
        }

        public async Task<Robot?> ReadByName(string name)
        {
            return await _context.Robots
                .Include(r => r.VideoStreams)
                .FirstOrDefaultAsync(robot => robot.Name.Equals(name, StringComparison.Ordinal));
        }

        public async Task<Robot> Update(Robot robot)
        {
            var entry = _context.Update(robot);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }
    }
}
