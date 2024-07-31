using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;
namespace Api.Services
{
    public interface IPlantService
    {
        public Task<IEnumerable<Plant>> ReadAll(bool readOnly = false);

        public Task<Plant?> ReadById(string id, bool readOnly = false);

        public Task<IEnumerable<Plant>> ReadByInstallation(string installationCode, bool readOnly = false);

        public Task<Plant?> ReadByInstallationAndName(Installation installation, string plantCode, bool readOnly = false);

        public Task<Plant?> ReadByInstallationAndName(string installationCode, string plantCode, bool readOnly = false);

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
    public class PlantService(FlotillaDbContext context, IInstallationService installationService, IAccessRoleService accessRoleService) : IPlantService
    {
        public async Task<IEnumerable<Plant>> ReadAll(bool readOnly = false)
        {
            return await GetPlants(readOnly: readOnly).ToListAsync();
        }

        public async Task<Plant?> ReadById(string id, bool readOnly = false)
        {
            return await GetPlants(readOnly: readOnly)
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<IEnumerable<Plant>> ReadByInstallation(string installationCode, bool readOnly = false)
        {
            var installation = await installationService.ReadByName(installationCode, readOnly: true);
            if (installation == null) { return new List<Plant>(); }
            return await GetPlants(readOnly: readOnly).Where(a =>
                a.Installation != null && a.Installation.Id.Equals(installation.Id)).ToListAsync();
        }

        public async Task<Plant?> ReadByInstallationAndName(Installation installation, string plantCode, bool readOnly = false)
        {
            return await GetPlants(readOnly: readOnly).Where(a =>
                a.PlantCode.ToLower().Equals(plantCode.ToLower()) &&
                a.Installation != null && a.Installation.Id.Equals(installation.Id)).FirstOrDefaultAsync();
        }

        public async Task<Plant?> ReadByInstallationAndName(string installationCode, string plantCode, bool readOnly = false)
        {
            var installation = await installationService.ReadByName(installationCode, readOnly: true);
            if (installation == null) { return null; }
            return await GetPlants(readOnly: readOnly).Where(a =>
                a.Installation != null && a.Installation.Id.Equals(installation.Id) &&
                a.PlantCode.ToLower().Equals(plantCode.ToLower())
            ).FirstOrDefaultAsync();
        }

        public async Task<Plant> Create(CreatePlantQuery newPlantQuery)
        {
            var installation = await installationService.ReadByName(newPlantQuery.InstallationCode) ??
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
                context.Entry(plant.Installation).State = EntityState.Unchanged;
                await context.Plants.AddAsync(plant);
                await ApplyDatabaseUpdate(plant.Installation);
            }
            return plant!;
        }

        public async Task<Plant> Update(Plant plant)
        {
            var entry = context.Update(plant);
            await ApplyDatabaseUpdate(plant.Installation);
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

            context.Plants.Remove(plant);
            await ApplyDatabaseUpdate(plant.Installation);

            return plant;
        }

        private IQueryable<Plant> GetPlants(bool readOnly = false)
        {
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var query = context.Plants.Include(i => i.Installation)
                .Where((p) => accessibleInstallationCodes.Result.Contains(p.Installation.InstallationCode.ToUpper()));
            return readOnly ? query.AsNoTracking() : query;
        }

        private async Task ApplyDatabaseUpdate(Installation? installation)
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes();
            if (installation == null || accessibleInstallationCodes.Contains(installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)))
                await context.SaveChangesAsync();
            else
                throw new UnauthorizedAccessException($"User does not have permission to update plant in installation {installation.Name}");
        }
    }
}
