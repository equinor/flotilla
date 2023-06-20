using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IAreaService
    {
        public abstract Task<IEnumerable<Area>> ReadAll();

        public abstract Task<Area?> ReadById(string id);

        public abstract Task<IEnumerable<Area>> ReadByAsset(string asset);

        public abstract Task<Area?> ReadByAssetAndName(string assetName, string name);

        public abstract Task<Area> Create(CreateAreaQuery newArea);

        public abstract Task<Area> Create(CreateAreaQuery newArea, List<Pose> safePositions);

        public abstract Task<Area> Update(Area area);

        public abstract Task<Area?> AddSafePosition(string asset, string name, SafePosition safePosition);

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

        public AreaService(FlotillaDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Area>> ReadAll()
        {
            return await GetAreas().ToListAsync();
        }

        private IQueryable<Area> GetAreas()
        {
            return _context.Areas.Include(a => a.SafePositions)
                .Include(a => a.Deck).ThenInclude(d => d.Installation).ThenInclude(i => i.Asset);
        }

        public async Task<Area?> ReadById(string id)
        {
            return await GetAreas()
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<Area?> ReadByAssetAndName(Asset? asset, string name)
        {
            if (asset == null)
                return null;
            return await _context.Areas.Where(a =>
                a.Name.ToLower().Equals(name.ToLower()) &&
                a.Asset.Equals(asset)
            ).Include(a => a.SafePositions).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Area>> ReadByAsset(string assetName)
        {
            var asset = await ReadAssetByName(assetName);
            if (asset == null)
                return new List<Area>();
            return await _context.Areas.Where(a =>
                a.Asset.Equals(asset)).Include(a => a.SafePositions).ToListAsync();
        }

        public async Task<Area?> ReadByAssetAndName(string assetName, string name)
        {
            // TODO: can we assume that this combination will be unique? Are area names specific enough?
            var asset = await ReadAssetByName(assetName);
            if (asset == null)
                return null;
            return await _context.Areas.Where(a =>
                a.Asset.Equals(asset) &&
                a.Name.ToLower().Equals(name.ToLower())
            ).Include(a => a.SafePositions).FirstOrDefaultAsync();
        }

        public async Task<Area?> ReadAreaByAssetAndInstallationAndDeckAndName(Asset? asset, Installation? installation, Deck? deck, string name)
        {
            if (asset == null || installation == null || deck == null)
                return null;
            return await _context.Areas.Where(a =>
                a.Deck.Equals(deck) &&
                a.Installation.Equals(installation) &&
                a.Asset.Equals(asset) &&
                a.Name.ToLower().Equals(name.ToLower())
            ).Include(a => a.Deck).ThenInclude(d => d.Installation).ThenInclude(i => i.Asset)
                .Include(a => a.SafePositions).FirstOrDefaultAsync();
        }

        public async Task<Deck?> ReadDeckByAssetAndInstallationAndName(Asset? asset, Installation? installation, string name)
        {
            if (asset == null || installation == null)
                return null;
            return await _context.Decks.Where(a =>
                a.Installation.Equals(installation) &&
                a.Asset.Equals(asset) &&
                a.Name.ToLower().Equals(name.ToLower())
            ).Include(d => d.Installation).Include(i => i.Asset).FirstOrDefaultAsync();
        }

        public async Task<Installation?> ReadInstallationByAssetAndName(Asset? asset, string name)
        {
            if (asset == null)
                return null;
            return await _context.Installations.Where(a =>
                a.Asset.Equals(asset) &&
                a.Name.ToLower().Equals(name.ToLower())
            ).Include(i => i.Asset).FirstOrDefaultAsync();
        }

        public async Task<Asset?> ReadAssetByName(string asset)
        {
            return await _context.Assets.Where(a =>
                a.ShortName.ToLower().Equals(asset.ToLower())
            ).FirstOrDefaultAsync();
        }

        public async Task<Area> Create(CreateAreaQuery newAreaQuery, List<Pose> safePositions)
        {
            var sp = new List<SafePosition>();
            foreach (var p in safePositions)
            {
                sp.Add(new SafePosition(p));
            }

            var asset = await ReadAssetByName(newAreaQuery.AssetCode);
            if (asset == null)
            {
                asset = new Asset
                {
                    Name = "", // TODO:
                    ShortName = newAreaQuery.AssetCode
                };
                await _context.Assets.AddAsync(asset);
                await _context.SaveChangesAsync();
            }

            var installation = await ReadInstallationByAssetAndName(asset, newAreaQuery.InstallationName);
            if (installation == null)
            {
                installation = new Installation
                {
                    Asset = asset,
                    Name = "", // TODO:
                    ShortName = newAreaQuery.InstallationName
                };
                await _context.Installations.AddAsync(installation);
                await _context.SaveChangesAsync();
            }

            var deck = await ReadDeckByAssetAndInstallationAndName(asset, installation, newAreaQuery.DeckName);
            if (deck == null)
            {
                deck = new Deck
                {
                    Installation = installation,
                    Asset = asset,
                    Name = newAreaQuery.DeckName
                };
                await _context.Decks.AddAsync(deck);
                await _context.SaveChangesAsync();
            }

            var existingArea = await ReadAreaByAssetAndInstallationAndDeckAndName(
                asset, installation, deck, newAreaQuery.AreaName);
            if (existingArea != null)
            {
                // TODO: maybe just append safe positions, or return an error
            }

            var newArea = new Area
            {
                Name = newAreaQuery.AreaName,
                DefaultLocalizationPose = newAreaQuery.DefaultLocalizationPose,
                SafePositions = sp,
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

        public async Task<Area?> AddSafePosition(string asset, string name, SafePosition safePosition)
        {
            var area = await ReadByAssetAndName(asset, name);
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
