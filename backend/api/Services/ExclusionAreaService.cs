using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IExclusionAreaService
    {
        public Task<IEnumerable<ExclusionArea>> ReadAll(bool readOnly = true);

        public Task<ExclusionArea?> ReadById(string id, bool readOnly = true);

        public Task<IEnumerable<ExclusionArea>> ReadByInstallationCode(
            string installationCode,
            bool readOnly = true
        );

        public Task<ExclusionArea?> ReadByInstallationAndName(
            string installationCode,
            string exclusionAreaName,
            bool readOnly = true
        );

        public Task<ExclusionArea?> ReadByInstallationAndPlantAndName(
            Installation installation,
            Plant plant,
            string exclusionAreaName,
            bool readOnly = true
        );

        public Task<List<ExclusionArea>> ReadExclusionAreasByInstallationCode(
            string installationCode,
            bool readOnly = true
        );

        public Task<List<MissionTask>> FilterOutExcludedMissionTasks(
            IList<MissionTask> missionTasks,
            string installationCode
        );

        public Task<ExclusionArea> Create(CreateExclusionAreaQuery newExclusionArea);

        public Task<ExclusionArea> Update(ExclusionArea exclusionArea);

        public Task<ExclusionArea?> Delete(string id);

        public void DetachTracking(FlotillaDbContext context, ExclusionArea exclusionArea);
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
    public class ExclusionAreaService(
        FlotillaDbContext context,
        IInstallationService installationService,
        IPlantService plantService,
        IAccessRoleService accessRoleService,
        ISignalRService signalRService,
        IAreaPolygonService areaPolygonService
    ) : IExclusionAreaService
    {
        public async Task<IEnumerable<ExclusionArea>> ReadAll(bool readOnly = true)
        {
            return await GetExclusionAreas(readOnly: readOnly).ToListAsync();
        }

        public async Task<ExclusionArea?> ReadById(string id, bool readOnly = true)
        {
            return await GetExclusionAreas(readOnly: readOnly)
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<IEnumerable<ExclusionArea>> ReadByInstallationCode(
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
            return await GetExclusionAreas(readOnly: readOnly)
                .Where(a => a.Installation != null && a.Installation.Id.Equals(installation.Id))
                .ToListAsync();
        }

        public async Task<ExclusionArea?> ReadByInstallationAndName(
            string installationCode,
            string exclusionAreaName,
            bool readOnly = true
        )
        {
            if (exclusionAreaName == null)
            {
                return null;
            }
            return await GetExclusionAreas(readOnly: readOnly)
                .Where(a =>
                    a.Name != null
                    && a.Installation != null
                    && a.Installation.InstallationCode.ToLower().Equals(installationCode.ToLower())
                    && a.Name.ToLower().Equals(exclusionAreaName.ToLower())
                )
                .FirstOrDefaultAsync();
        }

        public async Task<ExclusionArea?> ReadByInstallationAndPlantAndName(
            Installation installation,
            Plant plant,
            string name,
            bool readOnly = true
        )
        {
            return await GetExclusionAreas(readOnly: readOnly)
                .Where(a =>
                    a.Name != null
                    && a.Plant != null
                    && a.Plant.Id.Equals(plant.Id)
                    && a.Installation != null
                    && a.Installation.Id.Equals(installation.Id)
                    && a.Name.ToLower().Equals(name.ToLower())
                )
                .Include(d => d.Plant)
                .Include(i => i.Installation)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ExclusionArea>> ReadExclusionAreasByInstallationCode(
            string installationCode,
            bool readOnly = true
        )
        {
            var exclusionAreas = await GetExclusionAreas(readOnly: readOnly)
                .Where(a =>
                    a.Installation != null
                    && a.Installation.InstallationCode.Equals(installationCode)
                )
                .ToListAsync();
            return exclusionAreas;
        }

        public async Task<List<MissionTask>> FilterOutExcludedMissionTasks(
            IList<MissionTask> missionTasks,
            string installationCode
        )
        {
            var exclusionAreas = await ReadByInstallationCode(installationCode);

            return
            [
                .. missionTasks.Where(t =>
                    !exclusionAreas.Any(e =>
                        areaPolygonService.IsPositionInsidePolygon(
                            e.AreaPolygon.Positions,
                            t.RobotPose.Position,
                            e.AreaPolygon.ZMin,
                            e.AreaPolygon.ZMax
                        )
                    )
                ),
            ];
        }

        public async Task<ExclusionArea> Create(CreateExclusionAreaQuery newExclusionAreaQuery)
        {
            var installation =
                await installationService.ReadByInstallationCode(
                    newExclusionAreaQuery.InstallationCode,
                    readOnly: true
                )
                ?? throw new InstallationNotFoundException(
                    $"No installation with name {newExclusionAreaQuery.InstallationCode} could be found"
                );
            var plant =
                await plantService.ReadByInstallationAndPlantCode(
                    installation,
                    newExclusionAreaQuery.PlantCode,
                    readOnly: true
                )
                ?? throw new PlantNotFoundException(
                    $"No plant with name {newExclusionAreaQuery.PlantCode} could be found"
                );

            if (newExclusionAreaQuery.Name != null)
            {
                var existingExclusionArea = await ReadByInstallationAndPlantAndName(
                    installation,
                    plant,
                    newExclusionAreaQuery.Name,
                    readOnly: true
                );

                if (existingExclusionArea != null)
                {
                    throw new ExclusionAreaExistsException(
                        $"Exclusion are with name {newExclusionAreaQuery.Name} already exists"
                    );
                }
            }

            var exclusionArea = new ExclusionArea
            {
                Name = newExclusionAreaQuery.Name,
                Installation = installation,
                Plant = plant,
                AreaPolygon = newExclusionAreaQuery.AreaPolygon,
            };

            context.Entry(exclusionArea.Installation).State = EntityState.Unchanged;
            context.Entry(exclusionArea.Plant).State = EntityState.Unchanged;

            await context.ExclusionAreas.AddAsync(exclusionArea);
            await ApplyDatabaseUpdate(exclusionArea.Installation);
            _ = signalRService.SendMessageAsync(
                "ExclusionArea created",
                exclusionArea.Installation,
                new ExclusionAreaResponse(exclusionArea)
            );
            DetachTracking(context, exclusionArea);
            return exclusionArea!;
        }

        public async Task<ExclusionArea> Update(ExclusionArea exclusionArea)
        {
            if (exclusionArea.Installation is not null)
            {
                context.Entry(exclusionArea.Installation).State = EntityState.Unchanged;
            }
            if (exclusionArea.Plant is not null)
            {
                context.Entry(exclusionArea.Plant).State = EntityState.Unchanged;
            }
            var entry = context.Update(exclusionArea);
            await ApplyDatabaseUpdate(exclusionArea.Installation);
            _ = signalRService.SendMessageAsync(
                "ExclusionArea updated",
                exclusionArea.Installation,
                new ExclusionAreaResponse(exclusionArea)
            );
            DetachTracking(context, exclusionArea);
            return entry.Entity;
        }

        public async Task<ExclusionArea?> Delete(string id)
        {
            var exclusionArea = await GetExclusionAreas()
                .FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (exclusionArea is null)
            {
                return null;
            }

            context.ExclusionAreas.Remove(exclusionArea);
            await ApplyDatabaseUpdate(exclusionArea.Installation);
            _ = signalRService.SendMessageAsync(
                "ExclusionArea deleted",
                exclusionArea.Installation,
                new ExclusionAreaResponse(exclusionArea)
            );

            return exclusionArea;
        }

        private IQueryable<ExclusionArea> GetExclusionAreas(bool readOnly = true)
        {
            var accessibleInstallationCodes = accessRoleService
                .GetAllowedInstallationCodes(AccessMode.Read)
                .Result;
            var query = context
                .ExclusionAreas.Include(p => p.Plant)
                .ThenInclude(p => p.Installation)
                .Include(i => i.Installation)
                .Where(d =>
                    accessibleInstallationCodes.Contains(d.Installation.InstallationCode.ToUpper())
                );
            return readOnly ? query.AsNoTracking() : query.AsTracking();
        }

        private async Task ApplyDatabaseUpdate(Installation? installation)
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes(
                AccessMode.Write
            );
            if (
                installation == null
                || accessibleInstallationCodes.Contains(
                    installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)
                )
            )
                await context.SaveChangesAsync();
            else
                throw new UnauthorizedAccessException(
                    $"User does not have permission to update exclusion area in installation {installation.Name}"
                );
        }

        public void DetachTracking(FlotillaDbContext context, ExclusionArea exclusionArea)
        {
            if (
                exclusionArea.Installation != null
                && context.Entry(exclusionArea.Installation).State != EntityState.Detached
            )
                installationService.DetachTracking(context, exclusionArea.Installation);
            if (
                exclusionArea.Plant != null
                && context.Entry(exclusionArea.Plant).State != EntityState.Detached
            )
                plantService.DetachTracking(context, exclusionArea.Plant);
            context.Entry(exclusionArea).State = EntityState.Detached;
        }
    }
}
