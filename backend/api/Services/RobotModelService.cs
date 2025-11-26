using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IRobotModelService
    {
        public Task<IEnumerable<RobotModel>> ReadAll(bool readOnly = true);

        public Task<RobotModel?> ReadById(string id, bool readOnly = true);

        public Task<RobotModel?> ReadByRobotType(RobotType robotType, bool readOnly = true);

        public Task<RobotModel> Create(RobotModel newRobotModel);

        public Task<RobotModel> Update(RobotModel robotModel);

        public Task<RobotModel?> Delete(string id);

        public void DetachTracking(FlotillaDbContext context, RobotModel robotModel);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class RobotModelService(FlotillaDbContext context) : IRobotModelService
    {
        private readonly FlotillaDbContext _context = context;

        public async Task<IEnumerable<RobotModel>> ReadAll(bool readOnly = true)
        {
            return await GetRobotModels(readOnly: readOnly).ToListAsync();
        }

        private IQueryable<RobotModel> GetRobotModels(bool readOnly = true)
        {
            return readOnly
                ? _context.RobotModels.AsNoTracking()
                : _context.RobotModels.AsTracking();
        }

        public async Task<RobotModel?> ReadById(string id, bool readOnly = true)
        {
            return await GetRobotModels(readOnly: readOnly)
                .FirstOrDefaultAsync(robotModel => robotModel.Id.Equals(id));
        }

        public async Task<RobotModel?> ReadByRobotType(RobotType robotType, bool readOnly = true)
        {
            return await GetRobotModels(readOnly: readOnly)
                .FirstOrDefaultAsync(robotModel => robotModel.Type.Equals(robotType));
        }

        public async Task<RobotModel> Create(RobotModel newRobotModel)
        {
            await _context.RobotModels.AddAsync(newRobotModel);
            await _context.SaveChangesAsync();
            DetachTracking(_context, newRobotModel);
            return newRobotModel;
        }

        public async Task<RobotModel> Update(RobotModel robotModel)
        {
            var entry = _context.Update(robotModel);
            await _context.SaveChangesAsync();
            DetachTracking(_context, robotModel);
            return entry.Entity;
        }

        public async Task<RobotModel?> Delete(string id)
        {
            var robotModel = await GetRobotModels().FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (robotModel is null)
            {
                return null;
            }

            _context.RobotModels.Remove(robotModel);
            await _context.SaveChangesAsync();

            return robotModel;
        }

        public void DetachTracking(FlotillaDbContext context, RobotModel robotModel)
        {
            context.Entry(robotModel).State = EntityState.Detached;
        }
    }
}
