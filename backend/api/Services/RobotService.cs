using Api.Context;
using Api.Models;
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
        public async Task<Robot> Create(Robot robot)
        {
            await _context.Robots.AddAsync(robot);
            await _context.SaveChangesAsync();
            return robot;
        }

        public async Task<IEnumerable<Robot>> ReadAll()
        {
            return await _context.Robots.ToListAsync();
        }

        public async Task<Robot?> Read(string id)
        {
            return await _context.Robots.FirstOrDefaultAsync(robot => robot.Id.Equals(id, StringComparison.Ordinal));
        }
    }
}
