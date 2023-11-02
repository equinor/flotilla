using System.Diagnostics.CodeAnalysis;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;
namespace Api.Services
{
    public interface IPlantService
    {
        public Task<IEnumerable<Plant>> ReadAll();

        public Task<Plant?> ReadById(string id);

        public Task<IEnumerable<Plant>> ReadByInstallation(string installationCode);

        public Task<Plant?> ReadByInstallationAndName(Installation installation, string plantCode);

        public Task<Plant?> ReadByInstallationAndName(string installationCode, string plantCode);

        public Task<Plant> Create(CreatePlantQuery newPlant);

        public Task<Plant> Update(Plant plant);

        public Task<Plant?> Delete(string id);
    }

    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    [SuppressMessage(
        "Globalization",
        "CA1304:Specify CultureInfo",
        Justification = "Entity framework does not support translating culture info to SQL calls"
    )]
    public class PlantService : IPlantService
    {
        private readonly FlotillaDbContext _context;
        private readonly IInstallationService _installationService;

        public PlantService(FlotillaDbContext context, IInstallationService installationService)
        {
            _context = context;
            _installationService = installationService;
        }

        public async Task<IEnumerable<Plant>> ReadAll()
        {
            return await GetPlants().ToListAsync();
        }

        public async Task<Plant?> ReadById(string id)
        {
            return await GetPlants()
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<IEnumerable<Plant>> ReadByInstallation(string installationCode)
        {
            var installation = await _installationService.ReadByName(installationCode);
            if (installation == null) { return new List<Plant>(); }
            return await _context.Plants.Where(a =>
                a.Installation != null && a.Installation.Id.Equals(installation.Id)).ToListAsync();
        }

        public async Task<Plant?> ReadByInstallationAndName(Installation installation, string plantCode)
        {
            return await _context.Plants.Where(a =>
                a.PlantCode.ToLower().Equals(plantCode.ToLower()) &&
                a.Installation != null && a.Installation.Id.Equals(installation.Id)).FirstOrDefaultAsync();
        }

        public async Task<Plant?> ReadByInstallationAndName(string installationCode, string plantCode)
        {
            var installation = await _installationService.ReadByName(installationCode);
            if (installation == null) { return null; }
            return await _context.Plants.Where(a =>
                a.Installation != null && a.Installation.Id.Equals(installation.Id) &&
                a.PlantCode.ToLower().Equals(plantCode.ToLower())
            ).FirstOrDefaultAsync();
        }

        public async Task<Plant> Create(CreatePlantQuery newPlantQuery)
        {
            var installation = await _installationService.ReadByName(newPlantQuery.InstallationCode) ??
                               throw new InstallationNotFoundException($"No installation with name {newPlantQuery.InstallationCode} could be found");

            var plant = await ReadByInstallationAndName(installation, newPlantQuery.PlantCode);
            if (plant == null)
            {
                plant = new Plant
                {
                    Name = newPlantQuery.Name,
                    PlantCode = newPlantQuery.PlantCode,
                    Installation = installation
                };
                _context.Entry(plant.Installation).State = EntityState.Unchanged;
                await _context.Plants.AddAsync(plant);
                await _context.SaveChangesAsync();
            }
            return plant!;
        }

        public async Task<Plant> Update(Plant plant)
        {
            var entry = _context.Update(plant);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<Plant?> Delete(string id)
        {
            var plant = await GetPlants()
                .FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (plant is null)
            {
                return null;
            }

            _context.Plants.Remove(plant);
            await _context.SaveChangesAsync();

            return plant;
        }

        private IQueryable<Plant> GetPlants()
        {
            return _context.Plants.Include(i => i.Installation);
        }
    }
}
