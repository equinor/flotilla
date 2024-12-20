using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;
namespace Api.Services
{
    public interface IAreaService
    {
        public Task<PagedList<Area>> ReadAll(AreaQueryStringParameters parameters, bool readOnly = true);

        public Task<Area?> ReadById(string id, bool readOnly = true);

        public Task<IEnumerable<Area?>> ReadByInspectionAreaId(string inspectionAreaId, bool readOnly = true);

        public Task<Area?> ReadByInstallationAndName(string installationCode, string areaName, bool readOnly = true);

        public Task<Area> Create(CreateAreaQuery newArea);

        public Task<Area> Update(Area area);

        public Task<Area?> Delete(string id);

        public void DetachTracking(Area area);
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
    public class AreaService(
            FlotillaDbContext context, IInstallationService installationService, IPlantService plantService, IInspectionAreaService inspectionAreaService,
            IDefaultLocalizationPoseService defaultLocalizationPoseService, IAccessRoleService accessRoleService) : IAreaService
    {
        public async Task<PagedList<Area>> ReadAll(AreaQueryStringParameters parameters, bool readOnly = true)
        {
            var query = GetAreasWithSubModels(readOnly: readOnly).OrderBy(a => a.Installation);
            var filter = ConstructFilter(parameters);

            query = (IOrderedQueryable<Area>)query.Where(filter);

            return await PagedList<Area>.ToPagedListAsync(
                query,
                parameters.PageNumber,
                parameters.PageSize
            );
        }

        public async Task<Area?> ReadById(string id, bool readOnly = true)
        {
            return await GetAreas(readOnly: readOnly)
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<IEnumerable<Area?>> ReadByInspectionAreaId(string inspectionAreaId, bool readOnly = true)
        {
            if (inspectionAreaId == null) { return []; }
            return await GetAreas(readOnly: readOnly).Where(a => a.InspectionArea != null && a.InspectionArea.Id.Equals(inspectionAreaId)).ToListAsync();
        }

        public async Task<Area?> ReadByInstallationAndName(string installationCode, string areaName, bool readOnly = true)
        {
            var installation = await installationService.ReadByInstallationCode(installationCode, readOnly: true);
            if (installation == null) { return null; }

            return await GetAreas(readOnly: readOnly).Where(a =>
                a.Installation.Id.Equals(installation.Id) && a.Name.ToLower().Equals(areaName.ToLower())).FirstOrDefaultAsync();
        }
        public async Task<IEnumerable<Area>> ReadByInstallation(string installationCode)
        {
            var installation = await installationService.ReadByInstallationCode(installationCode, readOnly: true);
            if (installation == null) { return []; }

            return await GetAreas().Where(a => a.Installation.Id.Equals(installation.Id)).ToListAsync();
        }
        public async Task<Area> Create(CreateAreaQuery newAreaQuery)
        {

            var installation = await installationService.ReadByInstallationCode(newAreaQuery.InstallationCode, readOnly: true) ??
                               throw new InstallationNotFoundException($"No installation with name {newAreaQuery.InstallationCode} could be found");

            var plant = await plantService.ReadByInstallationAndPlantCode(installation, newAreaQuery.PlantCode, readOnly: true) ??
                        throw new PlantNotFoundException($"No plant with name {newAreaQuery.PlantCode} could be found");

            var inspectionArea = await inspectionAreaService.ReadByInstallationAndPlantAndName(installation, plant, newAreaQuery.InspectionAreaName, readOnly: true) ??
                       throw new InspectionAreaNotFoundException($"No inspection area with name {newAreaQuery.InspectionAreaName} could be found");

            var existingArea = await ReadByInstallationAndPlantAndInspectionAreaAndName(
                installation, plant, inspectionArea, newAreaQuery.AreaName, readOnly: true);
            if (existingArea != null)
            {
                throw new AreaExistsException($"Area with name {newAreaQuery.AreaName} already exists");
            }

            DefaultLocalizationPose? defaultLocalizationPose = null;
            if (newAreaQuery.DefaultLocalizationPose != null)
            {
                defaultLocalizationPose = await defaultLocalizationPoseService.Create(new DefaultLocalizationPose(newAreaQuery.DefaultLocalizationPose));
            }

            var newArea = new Area
            {
                Name = newAreaQuery.AreaName,
                DefaultLocalizationPose = defaultLocalizationPose,
                MapMetadata = new MapMetadata(),
                InspectionArea = inspectionArea!,
                Plant = plant!,
                Installation = installation!
            };

            context.Entry(newArea.Installation).State = EntityState.Unchanged;
            context.Entry(newArea.Plant).State = EntityState.Unchanged;
            context.Entry(newArea.InspectionArea).State = EntityState.Unchanged;

            if (newArea.DefaultLocalizationPose is not null) { context.Entry(newArea.DefaultLocalizationPose).State = EntityState.Modified; }

            await context.Areas.AddAsync(newArea);
            await ApplyDatabaseUpdate(installation);

            DetachTracking(newArea);
            return newArea;
        }

        public async Task<Area> Update(Area area)
        {
            var entry = context.Update(area);
            await ApplyDatabaseUpdate(area.Installation);
            return entry.Entity;
        }

        public async Task<Area?> Delete(string id)
        {
            var area = await GetAreas()
                .FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (area is null)
            {
                return null;
            }

            context.Areas.Remove(area);
            await ApplyDatabaseUpdate(area.Installation);

            return area;
        }

        private async Task ApplyDatabaseUpdate(Installation? installation)
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes();
            if (installation == null || accessibleInstallationCodes.Contains(installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)))
                await context.SaveChangesAsync();
            else
                throw new UnauthorizedAccessException($"User does not have permission to update area in installation {installation.Name}");
        }

        private IQueryable<Area> GetAreas(bool readOnly = true)
        {
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var query = context.Areas
                .Include(area => area.DefaultLocalizationPose)
                .Include(area => area.InspectionArea)
                .ThenInclude(inspectionArea => inspectionArea != null ? inspectionArea.DefaultLocalizationPose : null)
                .Include(area => area.InspectionArea)
                .ThenInclude(inspectionArea => inspectionArea.Plant)
                .ThenInclude(plant => plant.Installation)
                .Include(area => area.InspectionArea)
                .ThenInclude(inspectionArea => inspectionArea.Installation)
                .Include(area => area.Plant)
                .ThenInclude(plant => plant.Installation)
                .Include(area => area.Installation)
                .Where((area) => accessibleInstallationCodes.Result.Contains(area.Installation.InstallationCode.ToUpper()));
            return readOnly ? query.AsNoTracking() : query.AsTracking();
        }

        private IQueryable<Area> GetAreasWithSubModels(bool readOnly = true)
        {
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();

            // Include related entities using the Include method
            var query = context.Areas
             .Include(a => a.InspectionArea)
                 .ThenInclude(inspectionArea => inspectionArea != null ? inspectionArea.Plant : null)
                 .ThenInclude(plant => plant != null ? plant.Installation : null)
             .Include(a => a.Plant)
                 .ThenInclude(plant => plant != null ? plant.Installation : null)
             .Include(a => a.Installation)
             .Include(a => a.DefaultLocalizationPose)
             .Where(a => a.Installation != null && accessibleInstallationCodes.Result.Contains(a.Installation.InstallationCode.ToUpper()));
            return readOnly ? query.AsNoTracking() : query.AsTracking();
        }

        public async Task<Area?> ReadByInstallationAndPlantAndInspectionAreaAndName(Installation installation, Plant plant, InspectionArea inspectionArea, string areaName, bool readOnly = true)
        {
            return await GetAreas(readOnly: readOnly).Where(a =>
                    a.InspectionArea != null && a.InspectionArea.Id.Equals(inspectionArea.Id) &&
                    a.Plant.Id.Equals(plant.Id) &&
                    a.Installation.Id.Equals(installation.Id) &&
                    a.Name.ToLower().Equals(areaName.ToLower())
                ).FirstOrDefaultAsync();
        }

        private static Expression<Func<Area, bool>> ConstructFilter(
            AreaQueryStringParameters parameters
        )
        {
            Expression<Func<Area, bool>> installationFilter = string.IsNullOrEmpty(parameters.InstallationCode)
                ? area => true
                : area => area.InspectionArea != null &&
                    area.InspectionArea.Plant != null &&
                    area.InspectionArea.Plant.Installation != null &&
                    area.InspectionArea.Plant.Installation.InstallationCode.ToLower().Equals(parameters.InstallationCode.ToLower().Trim());

            Expression<Func<Area, bool>> inspectionAreaFilter = area => string.IsNullOrEmpty(parameters.InspectionArea) ||
                (area.InspectionArea != null &&
                    area.InspectionArea.Name != null &&
                    area.InspectionArea.Name.ToLower().Equals(parameters.InspectionArea.ToLower().Trim()));

            var area = Expression.Parameter(typeof(Area));

            Expression body = Expression.AndAlso(
                Expression.Invoke(installationFilter, area),
                Expression.Invoke(inspectionAreaFilter, area)
            );

            return Expression.Lambda<Func<Area, bool>>(body, area);
        }

        public void DetachTracking(Area area)
        {
            if (area.Installation != null) installationService.DetachTracking(area.Installation);
            if (area.Plant != null) plantService.DetachTracking(area.Plant);
            if (area.InspectionArea != null) inspectionAreaService.DetachTracking(area.InspectionArea);
            if (area.DefaultLocalizationPose != null) defaultLocalizationPoseService.DetachTracking(area.DefaultLocalizationPose);
            context.Entry(area).State = EntityState.Detached;
        }
    }
}
