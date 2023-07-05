using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IAreaService
    {
        public abstract Task<IEnumerable<Area>> ReadAll();

        public abstract Task<Area?> ReadById(string id);

        public abstract Task<IEnumerable<Area>> ReadByAsset(string assetCode);

        public abstract Task<Area?> ReadByAssetAndName(string assetCode, string areaName);

        public abstract Task<Area> Create(CreateAreaQuery newArea);

        public abstract Task<Area> Create(CreateAreaQuery newArea, List<Pose> safePositions);

        public abstract Task<Area> Update(Area area);

        public abstract Task<Area?> AddSafePosition(string assetCode, string areaName, SafePosition safePosition);

        public abstract Task<Area?> Delete(string id);

    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1304:Specify CultureInfo",
        Justification = "Entity framework does not support translating culture info to SQL calls"
    )]
    public class AreaService : IAreaService
    {
        private readonly FlotillaDbContext _context;
        private readonly IAssetService _assetService;
        private readonly IInstallationService _installationService;
        private readonly IDeckService _deckService;

        public AreaService(
            FlotillaDbContext context, IAssetService assetService, IInstallationService installationService, IDeckService deckService)
        {
            _context = context;
            _assetService = assetService;
            _installationService = installationService;
            _deckService = deckService;
        }

        public async Task<IEnumerable<Area>> ReadAll()
        {
            return await GetAreas().ToListAsync();
        }

        private IQueryable<Area> GetAreas()
        {
            return _context.Areas.Include(a => a.SafePositions)
                .Include(a => a.Deck).Include(d => d.Installation).Include(i => i.Asset);
        }

        public async Task<Area?> ReadById(string id)
        {
            return await GetAreas()
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<Area?> ReadByAssetAndName(Asset? asset, string areaName)
        {
            if (asset == null)
                return null;

            return await _context.Areas.Where(a =>
                a.Name.ToLower().Equals(areaName.ToLower()) &&
                a.Asset.Id.Equals(asset.Id)
            ).Include(a => a.SafePositions).Include(a => a.Asset)
                .Include(a => a.Installation).Include(a => a.Deck).FirstOrDefaultAsync();
        }

        public async Task<Area?> ReadByAssetAndName(string assetCode, string areaName)
        {
            var asset = await _assetService.ReadByName(assetCode);
            if (asset == null)
                return null;

            return await _context.Areas.Where(a =>
                a.Asset.Id.Equals(asset.Id) &&
                a.Name.ToLower().Equals(areaName.ToLower())
            ).Include(a => a.SafePositions).Include(a => a.Asset)
                .Include(a => a.Installation).Include(a => a.Deck).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Area>> ReadByAsset(string assetCode)
        {
            var asset = await _assetService.ReadByName(assetCode);
            if (asset == null)
                return new List<Area>();

            return await _context.Areas.Where(a =>
                a.Asset.Id.Equals(asset.Id)).Include(a => a.SafePositions).Include(a => a.Asset)
                .Include(a => a.Installation).Include(a => a.Deck).ToListAsync();
        }

        public async Task<Area?> ReadByAssetAndInstallationAndDeckAndName(Asset? asset, Installation? installation, Deck? deck, string areaName)
        {
            if (asset == null || installation == null || deck == null)
                return null;

            return await _context.Areas.Where(a =>
                a.Deck.Id.Equals(deck.Id) &&
                a.Installation.Id.Equals(installation.Id) &&
                a.Asset.Id.Equals(asset.Id) &&
                a.Name.ToLower().Equals(areaName.ToLower())
            ).Include(a => a.Deck).Include(d => d.Installation).Include(i => i.Asset)
                .Include(a => a.SafePositions).FirstOrDefaultAsync();
        }

        public async Task<Area> Create(CreateAreaQuery newAreaQuery, List<Pose> positions)
        {
            var safePositions = new List<SafePosition>();
            foreach (var pose in positions)
            {
                safePositions.Add(new SafePosition(pose));
            }

            var asset = await _assetService.ReadByName(newAreaQuery.AssetCode);
            if (asset == null)
            {
                throw new AssetNotFoundException($"No asset with name {newAreaQuery.AssetCode} could be found");
            }

            var installation = await _installationService.ReadByAssetAndName(asset, newAreaQuery.InstallationCode);
            if (installation == null)
            {
                throw new InstallationNotFoundException($"No installation with name {newAreaQuery.InstallationCode} could be found");
            }

            var deck = await _deckService.ReadByAssetAndInstallationAndName(asset, installation, newAreaQuery.DeckName);
            if (deck == null)
            {
                throw new DeckNotFoundException($"No deck with name {newAreaQuery.DeckName} could be found");
            }

            var existingArea = await ReadByAssetAndInstallationAndDeckAndName(
                asset, installation, deck, newAreaQuery.AreaName);
            if (existingArea != null)
            {
                throw new AreaNotFoundException($"No area with name {newAreaQuery.AreaName} could be found");
            }

            var newArea = new Area
            {
                Name = newAreaQuery.AreaName,
                DefaultLocalizationPose = newAreaQuery.DefaultLocalizationPose,
                SafePositions = safePositions,
                MapMetadata = new MapMetadata(),
                Deck = deck,
                Installation = installation,
                Asset = asset
            };

            await _context.Areas.AddAsync(newArea);
            await _context.SaveChangesAsync();
            return newArea;
        }

        public async Task<Area> Create(CreateAreaQuery newArea)
        {
            var area = await Create(newArea, new List<Pose>());
            return area;
        }

        public async Task<Area?> AddSafePosition(string assetCode, string areaName, SafePosition safePosition)
        {
            var area = await ReadByAssetAndName(assetCode, areaName);
            if (area is null)
            {
                return null;
            }

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
    }
}
