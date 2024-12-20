using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;
namespace Api.Services
{
    public interface IInspectionAreaService
    {
        public Task<IEnumerable<InspectionArea>> ReadAll(bool readOnly = true);

        public Task<InspectionArea?> ReadById(string id, bool readOnly = true);

        public Task<IEnumerable<InspectionArea>> ReadByInstallation(string installationCode, bool readOnly = true);

        public Task<InspectionArea?> ReadByInstallationAndName(string installationCode, string inspectionAreaName, bool readOnly = true);

        public Task<InspectionArea?> ReadByInstallationAndPlantAndName(Installation installation, Plant plant, string inspectionAreaName, bool readOnly = true);

        public Task<InspectionArea> Create(CreateInspectionAreaQuery newInspectionArea);

        public Task<InspectionArea> Update(InspectionArea inspectionArea);

        public Task<InspectionArea?> Delete(string id);

        public void DetachTracking(InspectionArea inspectionArea);
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
    public class InspectionAreaService(
        FlotillaDbContext context,
        IDefaultLocalizationPoseService defaultLocalizationPoseService,
        IInstallationService installationService,
        IPlantService plantService,
        IAccessRoleService accessRoleService,
        ISignalRService signalRService) : IInspectionAreaService
    {
        public async Task<IEnumerable<InspectionArea>> ReadAll(bool readOnly = true)
        {
            return await GetInspectionAreas(readOnly: readOnly).ToListAsync();
        }

        public async Task<InspectionArea?> ReadById(string id, bool readOnly = true)
        {
            return await GetInspectionAreas(readOnly: readOnly)
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<IEnumerable<InspectionArea>> ReadByInstallation(string installationCode, bool readOnly = true)
        {
            var installation = await installationService.ReadByInstallationCode(installationCode, readOnly: true);
            if (installation == null) { return []; }
            return await GetInspectionAreas(readOnly: readOnly).Where(a =>
                a.Installation != null && a.Installation.Id.Equals(installation.Id)).ToListAsync();
        }

        public async Task<InspectionArea?> ReadByInstallationAndName(string installationCode, string inspectionAreaName, bool readOnly = true)
        {
            if (inspectionAreaName == null) { return null; }
            return await GetInspectionAreas(readOnly: readOnly).Where(a =>
                a.Installation != null && a.Installation.InstallationCode.ToLower().Equals(installationCode.ToLower()) && a.Name.ToLower().Equals(inspectionAreaName.ToLower())
            ).FirstOrDefaultAsync();
        }

        public async Task<InspectionArea?> ReadByInstallationAndPlantAndName(Installation installation, Plant plant, string name, bool readOnly = true)
        {
            return await GetInspectionAreas(readOnly: readOnly).Where(a =>
                a.Plant != null && a.Plant.Id.Equals(plant.Id) &&
                a.Installation != null && a.Installation.Id.Equals(installation.Id) &&
                a.Name.ToLower().Equals(name.ToLower())
            ).Include(d => d.Plant).Include(i => i.Installation).FirstOrDefaultAsync();
        }

        public async Task<InspectionArea> Create(CreateInspectionAreaQuery newInspectionAreaQuery)
        {
            var installation = await installationService.ReadByInstallationCode(newInspectionAreaQuery.InstallationCode, readOnly: true) ??
                               throw new InstallationNotFoundException($"No installation with name {newInspectionAreaQuery.InstallationCode} could be found");
            var plant = await plantService.ReadByInstallationAndPlantCode(installation, newInspectionAreaQuery.PlantCode, readOnly: true) ??
                        throw new PlantNotFoundException($"No plant with name {newInspectionAreaQuery.PlantCode} could be found");
            var existingInspectionArea = await ReadByInstallationAndPlantAndName(installation, plant, newInspectionAreaQuery.Name, readOnly: true);

            if (existingInspectionArea != null)
            {
                throw new InspectionAreaExistsException($"Inspection are with name {newInspectionAreaQuery.Name} already exists");
            }

            DefaultLocalizationPose? defaultLocalizationPose = null;
            if (newInspectionAreaQuery.DefaultLocalizationPose != null)
            {
                defaultLocalizationPose = await defaultLocalizationPoseService.Create(new DefaultLocalizationPose(newInspectionAreaQuery.DefaultLocalizationPose.Value.Pose, newInspectionAreaQuery.DefaultLocalizationPose.Value.IsDockingStation));
            }

            var inspectionArea = new InspectionArea
            {
                Name = newInspectionAreaQuery.Name,
                Installation = installation,
                Plant = plant,
                DefaultLocalizationPose = defaultLocalizationPose
            };

            context.Entry(inspectionArea.Installation).State = EntityState.Unchanged;
            context.Entry(inspectionArea.Plant).State = EntityState.Unchanged;
            if (inspectionArea.DefaultLocalizationPose is not null) { context.Entry(inspectionArea.DefaultLocalizationPose).State = EntityState.Modified; }

            await context.InspectionAreas.AddAsync(inspectionArea);
            await ApplyDatabaseUpdate(inspectionArea.Installation);
            _ = signalRService.SendMessageAsync("InspectionArea created", inspectionArea.Installation, new InspectionAreaResponse(inspectionArea));
            DetachTracking(inspectionArea);
            return inspectionArea!;
        }

        public async Task<InspectionArea> Update(InspectionArea inspectionArea)
        {
            var entry = context.Update(inspectionArea);
            await ApplyDatabaseUpdate(inspectionArea.Installation);
            _ = signalRService.SendMessageAsync("InspectionArea updated", inspectionArea.Installation, new InspectionAreaResponse(inspectionArea));
            return entry.Entity;
        }

        public async Task<InspectionArea?> Delete(string id)
        {
            var inspectionArea = await GetInspectionAreas()
                .FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (inspectionArea is null)
            {
                return null;
            }

            context.InspectionAreas.Remove(inspectionArea);
            await ApplyDatabaseUpdate(inspectionArea.Installation);
            _ = signalRService.SendMessageAsync("InspectionArea deleted", inspectionArea.Installation, new InspectionAreaResponse(inspectionArea));

            return inspectionArea;
        }

        private IQueryable<InspectionArea> GetInspectionAreas(bool readOnly = true)
        {
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var query = context.InspectionAreas.Include(p => p.Plant).ThenInclude(p => p.Installation).Include(i => i.Installation).Include(d => d.DefaultLocalizationPose)
                .Where((d) => accessibleInstallationCodes.Result.Contains(d.Installation.InstallationCode.ToUpper()));
            return readOnly ? query.AsNoTracking() : query.AsTracking();
        }

        private async Task ApplyDatabaseUpdate(Installation? installation)
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes();
            if (installation == null || accessibleInstallationCodes.Contains(installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)))
                await context.SaveChangesAsync();
            else
                throw new UnauthorizedAccessException($"User does not have permission to update inspection area in installation {installation.Name}");
        }

        public void DetachTracking(InspectionArea inspectionArea)
        {
            if (inspectionArea.Installation != null) installationService.DetachTracking(inspectionArea.Installation);
            if (inspectionArea.Plant != null) plantService.DetachTracking(inspectionArea.Plant);
            if (inspectionArea.DefaultLocalizationPose != null) defaultLocalizationPoseService.DetachTracking(inspectionArea.DefaultLocalizationPose);
            context.Entry(inspectionArea).State = EntityState.Detached;
        }
    }
}
