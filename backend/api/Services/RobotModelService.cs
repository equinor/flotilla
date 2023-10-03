﻿using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services
{
    public interface IRobotModelService
    {
        public abstract Task<IEnumerable<RobotModel>> ReadAll();

        public abstract Task<RobotModel?> ReadById(string id);

        public abstract Task<RobotModel?> ReadByRobotType(RobotType robotType);

        public abstract Task<RobotModel> Create(RobotModel newRobotModel);

        public abstract Task<RobotModel> Update(RobotModel robotModel);

        public abstract Task<RobotModel?> Delete(string id);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class RobotModelService : IRobotModelService
    {
        private readonly FlotillaDbContext _context;

        public RobotModelService(FlotillaDbContext context)
        {
            _context = context;

            if (ReadAll().Result.IsNullOrEmpty())
            {
                // If no models in database, add default ones
                // Robot models are essentially database enums and should just be added to all databases
                // They can then be modified later with other values if needed
                InitDb.AddRobotModelsToDatabase(context);
            }
        }

        public async Task<IEnumerable<RobotModel>> ReadAll()
        {
            return await GetRobotModels().ToListAsync();
        }

        private IQueryable<RobotModel> GetRobotModels()
        {
            return _context.RobotModels;
        }

        public async Task<RobotModel?> ReadById(string id)
        {
            return await GetRobotModels()
                .FirstOrDefaultAsync(robotModel => robotModel.Id.Equals(id));
        }

        public async Task<RobotModel?> ReadByRobotType(RobotType robotType)
        {
            return await GetRobotModels()
                .FirstOrDefaultAsync(robotModel => robotModel.Type.Equals(robotType));
        }

        public async Task<RobotModel> Create(RobotModel newRobotModel)
        {
            await _context.RobotModels.AddAsync(newRobotModel);
            await _context.SaveChangesAsync();
            return newRobotModel;
        }

        public async Task<RobotModel> Update(RobotModel robotModel)
        {
            var entry = _context.Update(robotModel);
            await _context.SaveChangesAsync();
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
    }
}
