using System.Diagnostics.CodeAnalysis;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
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
        public Task<IList<Robot>> ReadLocalizedRobotsForInstallation(string installationCode);
        public Task<Robot> Update(Robot robot);
        public Task<Robot> UpdateRobotStatus(string robotId, RobotStatus status);
        public Task<Robot> UpdateRobotBatteryLevel(string robotId, float batteryLevel);
        public Task<Robot> UpdateRobotPressureLevel(string robotId, float? pressureLevel);
        public Task<Robot> UpdateRobotPose(string robotId, Pose pose);
        public Task<Robot> UpdateRobotEnabled(string robotId, bool enabled);
        public Task<Robot> UpdateCurrentMissionId(string robotId, string? missionId);
        public Task<Robot> UpdateCurrentArea(string robotId, Area? area);
        public Task<Robot> UpdateMissionQueueFrozen(string robotId, bool missionQueueFrozen);
        public Task<Robot?> Delete(string id);
    }

    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class RobotService : IRobotService, IDisposable
    {
        private readonly FlotillaDbContext _context;
        private readonly ILogger<RobotService> _logger;
        private readonly IRobotModelService _robotModelService;

        private readonly Semaphore _robotSemaphore = new(1, 1);
        private readonly ISignalRService _signalRService;

        public RobotService(FlotillaDbContext context, ILogger<RobotService> logger, IRobotModelService robotModelService, ISignalRService signalRService)
        {
            _context = context;
            _logger = logger;
            _robotModelService = robotModelService;
            _signalRService = signalRService;
        }

        public void Dispose()
        {
            _robotSemaphore.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task<Robot> Create(Robot newRobot)
        {
            if (newRobot.CurrentArea is not null) { _context.Entry(newRobot.CurrentArea).State = EntityState.Unchanged; }

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
                _ = _signalRService.SendMessageAsync("Robot list updated", GetEnabledRobotsWithSubModels().Select((r) => new RobotResponse(r)));
                return newRobot;
            }
            throw new DbUpdateException("Could not create new robot in database as robot model does not exist");
        }

        public async Task<Robot> UpdateRobotStatus(string robotId, RobotStatus status) { return await UpdateRobotProperty(robotId, "Status", status); }
        public async Task<Robot> UpdateRobotBatteryLevel(string robotId, float batteryLevel) { return await UpdateRobotProperty(robotId, "BatteryLevel", batteryLevel); }
        public async Task<Robot> UpdateRobotPressureLevel(string robotId, float? pressureLevel) { return await UpdateRobotProperty(robotId, "PressureLevel", pressureLevel); }
        public async Task<Robot> UpdateRobotPose(string robotId, Pose pose) { return await UpdateRobotProperty(robotId, "Pose", pose); }
        public async Task<Robot> UpdateRobotEnabled(string robotId, bool enabled) { return await UpdateRobotProperty(robotId, "Enabled", enabled); }
        public async Task<Robot> UpdateCurrentMissionId(string robotId, string? currentMissionId) { return await UpdateRobotProperty(robotId, "CurrentMissionId", currentMissionId); }
        public async Task<Robot> UpdateCurrentArea(string robotId, Area? area) { return await UpdateRobotProperty(robotId, "CurrentArea", area); }
        public async Task<Robot> UpdateMissionQueueFrozen(string robotId, bool missionQueueFrozen) { return await UpdateRobotProperty(robotId, "MissionQueueFrozen", missionQueueFrozen); }


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
            if (robot.CurrentArea is not null) { _context.Entry(robot.CurrentArea).State = EntityState.Unchanged; }

            var entry = _context.Update(robot);
            await _context.SaveChangesAsync();
            _ = _signalRService.SendMessageAsync("Robot list updated", GetEnabledRobotsWithSubModels().Select((r) => new RobotResponse(r)));
            return entry.Entity;
        }

        public async Task<Robot?> Delete(string id)
        {
            var robot = await GetRobotsWithSubModels().FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (robot is null) { return null; }

            _context.Robots.Remove(robot);
            await _context.SaveChangesAsync();
            _ = _signalRService.SendMessageAsync("Robot list updated", GetEnabledRobotsWithSubModels().Select((r) => new RobotResponse(r)));
            return robot;
        }

        public async Task<IList<Robot>> ReadLocalizedRobotsForInstallation(string installationCode)
        {
            return await GetRobotsWithSubModels()
                .Where(robot =>
#pragma warning disable CA1304
                    robot.CurrentInstallation.ToLower().Equals(installationCode.ToLower())
#pragma warning restore CA1304
                    && robot.CurrentArea != null)
                .ToListAsync();
        }

        private async Task<Robot> UpdateRobotProperty(string robotId, string propertyName, object? value)
        {
            _robotSemaphore.WaitOne();
            var robot = await ReadById(robotId);
            if (robot is null)
            {
                string errorMessage = $"Robot with ID {robotId} was not found in the database";
                _logger.LogError("{Message}", errorMessage);
                _robotSemaphore.Release();
                throw new RobotNotFoundException(errorMessage);
            }

            foreach (var property in typeof(Robot).GetProperties())
            {
                if (property.Name == propertyName) { property.SetValue(robot, value); }
            }

            robot = await Update(robot);
            _robotSemaphore.Release();
            return robot;
        }

        private IQueryable<Robot> GetRobotsWithSubModels()
        {
            return _context.Robots
                .Include(r => r.VideoStreams)
                .Include(r => r.Model)
                .Include(r => r.CurrentArea)
                .ThenInclude(area => area != null ? area.Deck : null)
                .Include(r => r.CurrentArea)
                .ThenInclude(area => area != null ? area.Plant : null)
                .Include(r => r.CurrentArea)
                .ThenInclude(area => area != null ? area.Installation : null)
                .Include(r => r.CurrentArea)
                .ThenInclude(area => area != null ? area.SafePositions : null);
        }

        private IQueryable<Robot> GetEnabledRobotsWithSubModels()
        {
            return GetRobotsWithSubModels().Where(r => r.Enabled && r.Status != RobotStatus.Deprecated);
        }
    }
}
