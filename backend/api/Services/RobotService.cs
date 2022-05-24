using Api.Context;
using Api.Controllers.Models;
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

        public async Task<Robot> Create(CreateRobotQuery robot)
        {
            var newRobot = new Robot()
            {
                Name = robot.Name,
                Model = robot.Model,
                SerialNumber = robot.SerialNumber,
                Logs = "",
                Host = robot.Host,
                Port = robot.Port,
                Enabled = robot.Enabled,
                Status = robot.Status
            };

            await _context.Robots.AddAsync(newRobot);
            await _context.SaveChangesAsync();
            return newRobot;
        }

        public async Task<IEnumerable<Robot>> ReadAll()
        {
            return await _context.Robots.ToListAsync();
        }

        public async Task<Robot?> Read(string id)
        {
            return await _context.Robots.FirstOrDefaultAsync(
                robot => robot.Id.Equals(id, StringComparison.Ordinal)
            );
        }
    }
}
