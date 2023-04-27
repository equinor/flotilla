using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IAssetDeckService
    {
        public abstract Task<IEnumerable<AssetDeck>> ReadAll();

        public abstract Task<AssetDeck?> ReadById(string id);

        public abstract Task<IEnumerable<AssetDeck>> ReadByAsset(string asset);

        public abstract Task<AssetDeck?> ReadByAssetAndDeck(string asset, string deck);

        public abstract Task<AssetDeck> Create(CreateAssetDeckQuery newAssetDeck);

        public abstract Task<AssetDeck> Create(CreateAssetDeckQuery newAssetDeck, List<Pose> safePositions);

        public abstract Task<AssetDeck> Update(AssetDeck assetDeck);

        public abstract Task<AssetDeck?> AddSafePosition(string asset, string deck, SafePosition safePosition);

        public abstract Task<AssetDeck?> Delete(string id);

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
    public class AssetDeckService : IAssetDeckService
    {
        private readonly FlotillaDbContext _context;

        public AssetDeckService(FlotillaDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AssetDeck>> ReadAll()
        {
            return await GetAssetDecks().ToListAsync();
        }

        private IQueryable<AssetDeck> GetAssetDecks()
        {
            return _context.AssetDecks.Include(a => a.SafePositions);
        }

        public async Task<AssetDeck?> ReadById(string id)
        {
            return await GetAssetDecks()
                .FirstOrDefaultAsync(assetDeck => assetDeck.Id.Equals(id));
        }

        public async Task<IEnumerable<AssetDeck>> ReadByAsset(string asset)
        {

            return await _context.AssetDecks.Where(a =>
                a.AssetCode.ToLower().Equals(asset.ToLower())).Include(a => a.SafePositions).ToListAsync();
        }

        public async Task<AssetDeck?> ReadByAssetAndDeck(string asset, string deck)
        {
            return await _context.AssetDecks.Where(a =>
                a.AssetCode.ToLower().Equals(asset.ToLower()) &&
                a.DeckName.ToLower().Equals(deck.ToLower())
            ).Include(a => a.SafePositions).FirstOrDefaultAsync();
        }

        public async Task<AssetDeck> Create(CreateAssetDeckQuery newAssetDeck, List<Pose> safePositions)
        {
            var sp = new List<SafePosition>();
            foreach (var p in safePositions)
            {
                sp.Add(new SafePosition(p));
            }
            var assetDeck = new AssetDeck
            {
                AssetCode = newAssetDeck.AssetCode,
                DeckName = newAssetDeck.DeckName,
                DefaultLocalizationPose = newAssetDeck.DefaultLocalizationPose,
                SafePositions = sp
            };
            await _context.AssetDecks.AddAsync(assetDeck);
            await _context.SaveChangesAsync();
            return assetDeck;
        }

        public async Task<AssetDeck> Create(CreateAssetDeckQuery newAssetDeck)
        {
            var assetDeck = await Create(newAssetDeck, new List<Pose>());
            return assetDeck;
        }

        public async Task<AssetDeck?> AddSafePosition(string asset, string deck, SafePosition safePosition)
        {
            var assetDeck = await ReadByAssetAndDeck(asset, deck);
            if (assetDeck is null)
            {
                return null;
            }
            assetDeck.SafePositions.Add(safePosition);
            _context.AssetDecks.Update(assetDeck);
            await _context.SaveChangesAsync();
            return assetDeck;
        }

        public async Task<AssetDeck> Update(AssetDeck assetDeck)
        {
            var entry = _context.Update(assetDeck);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<AssetDeck?> Delete(string id)
        {
            var assetDeck = await GetAssetDecks()
                .FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (assetDeck is null)
            {
                return null;
            }

            _context.AssetDecks.Remove(assetDeck);
            await _context.SaveChangesAsync();

            return assetDeck;
        }
    }
}
