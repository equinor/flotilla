using System.Diagnostics.CodeAnalysis;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;
namespace Api.Services
{
    public interface IAreaService
    {
        public Task<IEnumerable<Area>> ReadAll();

        public Task<Area?> ReadById(string id);

        public Task<IEnumerable<Area?>> ReadByDeckId(string deckId);

        public Task<IEnumerable<Area>> ReadByInstallation(string installationCode);

        public Task<Area?> ReadByInstallationAndName(string installationCode, string areaName);

        public Task<Area> Create(CreateAreaQuery newArea);

        public Task<Area> Create(CreateAreaQuery newArea, List<Pose> safePositions);

        public Task<Area> Update(Area area);

        public Task<Area?> AddSafePosition(string installationCode, string areaName, SafePosition safePosition);

        public Task<Area?> Delete(string id);
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
    public class AreaService : IAreaService
    {
        private readonly FlotillaDbContext _context;
        private readonly IDeckService _deckService;
        private readonly IDefaultLocalizationPoseService _defaultLocalizationPoseService;
        private readonly IInstallationService _installationService;
        private readonly IPlantService _plantService;

        public AreaService(
            FlotillaDbContext context, IInstallationService installationService, IPlantService plantService, IDeckService deckService,
            IDefaultLocalizationPoseService defaultLocalizationPoseService)
        {
            _context = context;
            _installationService = installationService;
            _plantService = plantService;
            _deckService = deckService;
            _defaultLocalizationPoseService = defaultLocalizationPoseService;
        }

        public async Task<IEnumerable<Area>> ReadAll()
        {
            return await GetAreas().ToListAsync();
        }

        public async Task<Area?> ReadById(string id)
        {
            return await GetAreas()
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<IEnumerable<Area?>> ReadByDeckId(string deckId)
        {
            if (deckId == null) { return new List<Area>(); }

            return await _context.Areas.Where(a =>
                    a.Deck != null && a.Deck.Id.Equals(deckId)
                ).Include(a => a.SafePositions).Include(a => a.Installation)
                .Include(a => a.Plant).Include(a => a.Deck).ToListAsync();
        }

        public async Task<Area?> ReadByInstallationAndName(string installationCode, string areaName)
        {
            var installation = await _installationService.ReadByName(installationCode);
            if (installation == null) { return null; }

            return await _context.Areas.Where(a =>
                    a.Installation != null && a.Installation.Id.Equals(installation.Id) &&
                    a.Name.ToLower().Equals(areaName.ToLower())
                ).Include(a => a.SafePositions).Include(a => a.Installation)
                .Include(a => a.Plant).Include(a => a.Deck).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Area>> ReadByInstallation(string installationCode)
        {
            var installation = await _installationService.ReadByName(installationCode);
            if (installation == null) { return new List<Area>(); }

            return await _context.Areas.Where(a =>
                    a.Installation != null && a.Installation.Id.Equals(installation.Id)).Include(a => a.SafePositions).Include(a => a.Installation)
                .Include(a => a.Plant).Include(a => a.Deck).ToListAsync();
        }

        public async Task<Area> Create(CreateAreaQuery newAreaQuery, List<Pose> positions)
        {
            var safePositions = new List<SafePosition>();
            foreach (var pose in positions)
            {
                safePositions.Add(new SafePosition(pose));
            }

            var installation = await _installationService.ReadByName(newAreaQuery.InstallationCode) ??
                               throw new InstallationNotFoundException($"No installation with name {newAreaQuery.InstallationCode} could be found");

            var plant = await _plantService.ReadByInstallationAndName(installation, newAreaQuery.PlantCode) ??
                        throw new PlantNotFoundException($"No plant with name {newAreaQuery.PlantCode} could be found");

            var deck = await _deckService.ReadByInstallationAndPlantAndName(installation, plant, newAreaQuery.DeckName) ??
                       throw new DeckNotFoundException($"No deck with name {newAreaQuery.DeckName} could be found");

            var existingArea = await ReadByInstallationAndPlantAndDeckAndName(
                installation, plant, deck, newAreaQuery.AreaName);
            if (existingArea != null)
            {
                throw new AreaExistsException($"Area with name {newAreaQuery.AreaName} already exists");
            }

            DefaultLocalizationPose? defaultLocalizationPose = null;
            if (newAreaQuery.DefaultLocalizationPose != null)
            {
                defaultLocalizationPose = await _defaultLocalizationPoseService.Create(new DefaultLocalizationPose(newAreaQuery.DefaultLocalizationPose));
            }

            var newArea = new Area
            {
                Name = newAreaQuery.AreaName,
                DefaultLocalizationPose = defaultLocalizationPose,
                SafePositions = safePositions,
                MapMetadata = new MapMetadata(),
                Deck = deck!,
                Plant = plant!,
                Installation = installation!
            };

            _context.Entry(newArea.Installation).State = EntityState.Unchanged;
            _context.Entry(newArea.Plant).State = EntityState.Unchanged;
            _context.Entry(newArea.Deck).State = EntityState.Unchanged;

            if (newArea.DefaultLocalizationPose is not null) { _context.Entry(newArea.DefaultLocalizationPose).State = EntityState.Modified; }

            await _context.Areas.AddAsync(newArea);
            await _context.SaveChangesAsync();
            return newArea;
        }

        public async Task<Area> Create(CreateAreaQuery newArea)
        {
            var area = await Create(newArea, new List<Pose>());
            return area;
        }

        public async Task<Area?> AddSafePosition(string installationCode, string areaName, SafePosition safePosition)
        {
            var area = await ReadByInstallationAndName(installationCode, areaName);
            if (area is null) { return null; }

            area.SafePositions.Add(safePosition);

            _context.Areas.Update(area);
            await _context.SaveChangesAsync();
            return area;
        }

        public async Task<Area> Update(Area area)
        {
            var entry = _context.Update(area);
            await _context.SaveChangesAsync();
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

            _context.Areas.Remove(area);
            await _context.SaveChangesAsync();

            return area;
        }

        private IQueryable<Area> GetAreas()
        {
            return _context.Areas.Include(a => a.SafePositions)
                .Include(a => a.Deck).Include(d => d.Plant).Include(i => i.Installation).Include(d => d.DefaultLocalizationPose);
        }

        public async Task<Area?> ReadByInstallationAndName(Installation? installation, string areaName)
        {
            if (installation == null) { return null; }

            return await _context.Areas.Where(a =>
                    a.Name.ToLower().Equals(areaName.ToLower()) &&
                    a.Installation.InstallationCode.Equals(installation.InstallationCode)
                ).Include(a => a.SafePositions).Include(a => a.Installation)
                .Include(a => a.Plant).Include(a => a.Deck).FirstOrDefaultAsync();
        }

        public async Task<Area?> ReadByInstallationAndPlantAndDeckAndName(Installation installation, Plant plant, Deck deck, string areaName)
        {
            return await _context.Areas.Where(a =>
                    a.Deck != null && a.Deck.Id.Equals(deck.Id) &&
                    a.Plant != null && a.Plant.Id.Equals(plant.Id) &&
                    a.Installation != null && a.Installation.Id.Equals(installation.Id) &&
                    a.Name.ToLower().Equals(areaName.ToLower())
                ).Include(a => a.Deck).Include(d => d.Plant).Include(i => i.Installation)
                .Include(a => a.SafePositions).FirstOrDefaultAsync();
        }
    }
}
