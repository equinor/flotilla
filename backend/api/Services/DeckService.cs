using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IDeckService
    {
        public abstract Task<IEnumerable<Deck>> ReadAll();

        public abstract Task<Deck?> ReadById(string id);

        public abstract Task<IEnumerable<Deck>> ReadByAsset(string assetCode);

        public abstract Task<Deck?> ReadByName(string deckName);

        public abstract Task<Deck?> ReadByAssetAndInstallationAndName(Asset asset, Installation installation, string deckName);

        public abstract Task<Deck> Create(CreateDeckQuery newDeck);

        public abstract Task<Deck> Update(Deck deck);

        public abstract Task<Deck?> Delete(string id);

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
    public class DeckService : IDeckService
    {
        private readonly FlotillaDbContext _context;
        private readonly IAssetService _assetService;
        private readonly IInstallationService _installationService;

        public DeckService(FlotillaDbContext context, IAssetService assetService, IInstallationService installationService)
        {
            _context = context;
            _assetService = assetService;
            _installationService = installationService;
        }

        public async Task<IEnumerable<Deck>> ReadAll()
        {
            return await GetDecks().ToListAsync();
        }

        private IQueryable<Deck> GetDecks()
        {
            return _context.Decks;
        }

        public async Task<Deck?> ReadById(string id)
        {
            return await GetDecks()
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<IEnumerable<Deck>> ReadByAsset(string assetCode)
        {
            var asset = await _assetService.ReadByName(assetCode);
            if (asset == null)
                return new List<Deck>();
            return await _context.Decks.Where(a =>
                a.Asset.Id.Equals(asset.Id)).ToListAsync();
        }

        public async Task<Deck?> ReadByName(string deckName)
        {
            if (deckName == null)
                return null;
            return await _context.Decks.Where(a =>
                a.Name.ToLower().Equals(deckName.ToLower())
            ).FirstOrDefaultAsync();
        }

        public async Task<Deck?> ReadByAssetAndInstallationAndName(Asset asset, Installation installation, string name)
        {
            return await _context.Decks.Where(a =>
                a.Installation.Id.Equals(installation.Id) &&
                a.Asset.Id.Equals(asset.Id) &&
                a.Name.ToLower().Equals(name.ToLower())
            ).Include(d => d.Installation).Include(i => i.Asset).FirstOrDefaultAsync();
        }

        public async Task<Deck> Create(CreateDeckQuery newDeckQuery)
        {
            var asset = await _assetService.ReadByName(newDeckQuery.AssetCode);
            if (asset == null)
            {
                throw new AssetNotFoundException($"No asset with name {newDeckQuery.AssetCode} could be found");
            }
            var installation = await _installationService.ReadByAssetAndName(asset, newDeckQuery.InstallationCode);
            if (installation == null)
            {
                throw new InstallationNotFoundException($"No installation with name {newDeckQuery.InstallationCode} could be found");
            }
            var deck = await ReadByAssetAndInstallationAndName(asset, installation, newDeckQuery.Name);
            if (deck == null)
            {
                deck = new Deck
                {
                    Name = newDeckQuery.Name,
                    Asset = asset,
                    Installation = installation
                };
                await _context.Decks.AddAsync(deck);
                await _context.SaveChangesAsync();
            }
            return deck!;
        }

        public async Task<Deck> Update(Deck deck)
        {
            var entry = _context.Update(deck);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<Deck?> Delete(string id)
        {
            var deck = await GetDecks()
                .FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (deck is null)
            {
                return null;
            }

            _context.Decks.Remove(deck);
            await _context.SaveChangesAsync();

            return deck;
        }
    }
}
