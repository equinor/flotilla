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

        private Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Api.Database.Models.Robot, Api.Database.Models.Pose> getRobotObject()
        {
            return _context.Robots
                .Include(r => r.VideoStreams)
                .Include(r => r.Pose)
                .Include(r => r.Pose.Frame)
                .Include(r => r.Pose.Orientation)
                .Include(r => r.Pose.Orientation.Frame)
                .Include(r => r.Pose.Position)
                .Include(r => r.Pose.Position.Frame)
                .Include(r => r.Pose);
        }

        public async Task<Robot> Create(Robot newRobot)
        {
            await _context.Robots.AddAsync(newRobot);
            await _context.SaveChangesAsync();
            return newRobot;
        }

        public async Task<IEnumerable<Robot>> ReadAll()
        {
            return await getRobotObject().ToListAsync();
        }

        public async Task<Robot?> ReadById(string id)
        {
            return await getRobotObject().FirstOrDefaultAsync(robot => robot.Id.Equals(id));
        }

        public async Task<Robot?> ReadByName(string name)
        {
            return await getRobotObject().FirstOrDefaultAsync(robot => robot.Name.Equals(name));
        }

        public async Task<Robot> Update(Robot robot)
        {
            var entry = _context.Update(robot);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }
    }
}
