using System.Diagnostics.CodeAnalysis;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;
namespace Api.Services
{
    public interface IRobotService
    {
        public Task<Robot> Create(Robot newRobot);
        public Task<Robot> CreateFromQuery(CreateRobotQuery robotQuery);
        public Task<IEnumerable<Robot>> ReadAll();
        public Task<IEnumerable<string>> ReadAllActivePlants();
        public Task<Robot?> ReadById(string id);
        public Task<Robot?> ReadByIsarId(string isarId);
        public Task<Robot> Update(Robot robot);
        public Task<Robot?> Delete(string id);
    }

    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class RobotService : IRobotService
    {
        private readonly FlotillaDbContext _context;
        private readonly IRobotModelService _robotModelService;
        private readonly ISignalRService _signalRService;

        public RobotService(FlotillaDbContext context, IRobotModelService robotModelService, ISignalRService signalRService)
        {
            _context = context;
            _robotModelService = robotModelService;
            _signalRService = signalRService;
        }

        public async Task<Robot> Create(Robot newRobot)
        {
            await _context.Robots.AddAsync(newRobot);
            await _context.SaveChangesAsync();
            return newRobot;
        }

        public async Task<Robot> CreateFromQuery(CreateRobotQuery robotQuery)
        {
            var robotModel = await _robotModelService.ReadByRobotType(robotQuery.RobotType);
            if (robotModel != null)
            {
                var newRobot = new Robot(robotQuery)
                {
                    Model = robotModel
                };
                _context.Entry(robotModel).State = EntityState.Unchanged;
                await _context.Robots.AddAsync(newRobot);
                await _context.SaveChangesAsync();
                await _signalRService.SendMessageAsync("robot list updated", GetEnabledRobotsWithSubModels());
                return newRobot;
            }
            throw new DbUpdateException("Could not create new robot in database as robot model does not exist");
        }

        public async Task<IEnumerable<Robot>> ReadAll()
        {
            return await GetRobotsWithSubModels().ToListAsync();
        }

        public async Task<Robot?> ReadById(string id)
        {
            return await GetRobotsWithSubModels().FirstOrDefaultAsync(robot => robot.Id.Equals(id));
        }

        public async Task<Robot?> ReadByIsarId(string isarId)
        {
            return await GetRobotsWithSubModels()
                .FirstOrDefaultAsync(robot => robot.IsarId.Equals(isarId));
        }

        public async Task<IEnumerable<string>> ReadAllActivePlants()
        {
            return await _context.Robots.Where(r => r.Enabled).Select(r => r.CurrentInstallation).ToListAsync();
        }

        public async Task<Robot> Update(Robot robot)
        {
            var entry = _context.Update(robot);
            await _context.SaveChangesAsync();
            await _signalRService.SendMessageAsync("robot list updated", GetEnabledRobotsWithSubModels());
            return entry.Entity;
        }

        public async Task<Robot?> Delete(string id)
        {
            var robot = await GetRobotsWithSubModels().FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (robot is null) { return null; }

            _context.Robots.Remove(robot);
            await _context.SaveChangesAsync();
            await _signalRService.SendMessageAsync("robot list updated", GetEnabledRobotsWithSubModels());
            return robot;
        }

        private IQueryable<Robot> GetRobotsWithSubModels()
        {
            return _context.Robots
                .Include(r => r.VideoStreams)
                .Include(r => r.Model)
                .Include(r => r.CurrentArea)
                .ThenInclude(r => r != null ? r.SafePositions : null);
        }

        private IQueryable<Robot> GetEnabledRobotsWithSubModels()
        {
            return GetRobotsWithSubModels().Where((r) => r.Enabled && r.Status != RobotStatus.Deprecated);
        }
    }
}
