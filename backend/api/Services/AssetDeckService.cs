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

        public abstract Task<AssetDeck> Create(CreateAssetDeckQuery newAssetDeck);

        public abstract Task<AssetDeck> Update(AssetDeck assetDeck);

        public abstract Task<AssetDeck?> Delete(string id);

    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
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
            return _context.AssetDecks;
        }

        public async Task<AssetDeck?> ReadById(string id)
        {
            return await GetAssetDecks()
                .FirstOrDefaultAsync(assetDeck => assetDeck.Id.Equals(id));
        }

        public async Task<AssetDeck> Create(CreateAssetDeckQuery newAssetDeck)
        {
            var assetDeck = new AssetDeck
            {
                AssetCode = newAssetDeck.AssetCode,
                DeckName = newAssetDeck.DeckName,
                DefaultLocalizationPose = newAssetDeck.DefaultLocalizationPose
            };

            await _context.AssetDecks.AddAsync(assetDeck);
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
