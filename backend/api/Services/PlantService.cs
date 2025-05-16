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
        public Task<IEnumerable<Plant>> ReadAll(bool readOnly = true);

        public Task<Plant?> ReadById(string id, bool readOnly = true);

        public Task<IEnumerable<Plant>> ReadByInstallation(
            string installationCode,
            bool readOnly = true
        );

        public Task<Plant?> ReadByPlantCode(string plantCode, bool readOnly = true);

        public Task<Plant?> ReadByInstallationAndPlantCode(
            Installation installation,
            string plantCode,
            bool readOnly = true
        );

        public Task<Plant?> ReadByInstallationAndPlantCode(
            string installationCode,
            string plantCode,
            bool readOnly = true
        );

        public Task<Plant> Create(CreatePlantQuery newPlant);

        public Task<Plant> Update(Plant plant);

        public Task<Plant?> Delete(string id);

        public void DetachTracking(FlotillaDbContext context, Plant plant);
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
    public class PlantService(
        FlotillaDbContext context,
        IInstallationService installationService,
        IAccessRoleService accessRoleService
    ) : IPlantService
    {
        public async Task<IEnumerable<Plant>> ReadAll(bool readOnly = true)
        {
            return await GetPlants(readOnly: readOnly).ToListAsync();
        }

        public async Task<Plant?> ReadById(string id, bool readOnly = true)
        {
            return await GetPlants(readOnly: readOnly).FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<IEnumerable<Plant>> ReadByInstallation(
            string installationCode,
            bool readOnly = true
        )
        {
            var installation = await installationService.ReadByInstallationCode(
                installationCode,
                readOnly: true
            );
            if (installation == null)
            {
                return [];
            }
            return await GetPlants(readOnly: readOnly)
                .Where(a => a.Installation != null && a.Installation.Id.Equals(installation.Id))
                .ToListAsync();
        }

        public async Task<Plant?> ReadByPlantCode(string plantCode, bool readOnly = true)
        {
            return await GetPlants(readOnly: readOnly)
                .Where(a => a.PlantCode.ToLower().Equals(plantCode.ToLower()))
                .FirstOrDefaultAsync();
        }

        public async Task<Plant?> ReadByInstallationAndPlantCode(
            Installation installation,
            string plantCode,
            bool readOnly = true
        )
        {
            return await GetPlants(readOnly: readOnly)
                .Where(a =>
                    a.PlantCode.ToLower().Equals(plantCode.ToLower())
                    && a.Installation != null
                    && a.Installation.Id.Equals(installation.Id)
                )
                .FirstOrDefaultAsync();
        }

        public async Task<Plant?> ReadByInstallationAndPlantCode(
            string installationCode,
            string plantCode,
            bool readOnly = true
        )
        {
            var installation = await installationService.ReadByInstallationCode(
                installationCode,
                readOnly: true
            );
            if (installation == null)
            {
                return null;
            }
            return await GetPlants(readOnly: readOnly)
                .Where(a =>
                    a.Installation != null
                    && a.Installation.Id.Equals(installation.Id)
                    && a.PlantCode.ToLower().Equals(plantCode.ToLower())
                )
                .FirstOrDefaultAsync();
        }

        public async Task<Plant> Create(CreatePlantQuery newPlantQuery)
        {
            var installation =
                await installationService.ReadByInstallationCode(
                    newPlantQuery.InstallationCode,
                    readOnly: true
                )
                ?? throw new InstallationNotFoundException(
                    $"No installation with name {newPlantQuery.InstallationCode} could be found"
                );

            var plant = await ReadByInstallationAndPlantCode(
                installation,
                newPlantQuery.PlantCode,
                readOnly: true
            );
            if (plant == null)
            {
                plant = new Plant
                {
                    Name = newPlantQuery.Name,
                    PlantCode = newPlantQuery.PlantCode,
                    Installation = installation,
                };
                context.Entry(plant.Installation).State = EntityState.Unchanged;
                await context.Plants.AddAsync(plant);
                await ApplyDatabaseUpdate(plant.Installation);
                DetachTracking(context, plant);
            }
            return plant!;
        }

        public async Task<Plant> Update(Plant plant)
        {
            var entry = context.Update(plant);
            await ApplyDatabaseUpdate(plant.Installation);
            DetachTracking(context, plant);
            return entry.Entity;
        }

        public async Task<Plant?> Delete(string id)
        {
            var plant = await GetPlants().FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (plant is null)
            {
                return null;
            }

            context.Plants.Remove(plant);
            await ApplyDatabaseUpdate(plant.Installation);

            return plant;
        }

        private IQueryable<Plant> GetPlants(bool readOnly = true)
        {
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var query = context
                .Plants.Include(i => i.Installation)
                .Where(p =>
                    accessibleInstallationCodes.Result.Contains(
                        p.Installation.InstallationCode.ToUpper()
                    )
                );
            return readOnly ? query.AsNoTracking() : query.AsTracking();
        }

        private async Task ApplyDatabaseUpdate(Installation? installation)
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes();
            if (
                installation == null
                || accessibleInstallationCodes.Contains(
                    installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)
                )
            )
                await context.SaveChangesAsync();
            else
                throw new UnauthorizedAccessException(
                    $"User does not have permission to update plant in installation {installation.Name}"
                );
        }

        public void DetachTracking(FlotillaDbContext context, Plant plant)
        {
            if (plant.Installation != null)
                installationService.DetachTracking(context, plant.Installation);
            context.Entry(plant).State = EntityState.Detached;
        }
    }
}
