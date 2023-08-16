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

        public abstract Task<IEnumerable<Deck>> ReadByInstallation(string installationCode);

        public abstract Task<Deck?> ReadByName(string deckName);

        public abstract Task<Deck?> ReadByInstallationAndPlantAndName(Installation installation, Plant plant, string deckName);

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
        private readonly IInstallationService _installationService;
        private readonly IPlantService _plantService;

        public DeckService(FlotillaDbContext context, IInstallationService installationService, IPlantService plantService)
        {
            _context = context;
            _installationService = installationService;
            _plantService = plantService;
        }

        public async Task<IEnumerable<Deck>> ReadAll()
        {
            return await GetDecks().ToListAsync();
        }

        private IQueryable<Deck> GetDecks()
        {
            return _context.Decks.Include(p => p.Plant).Include(i => i.Installation);
        }

        public async Task<Deck?> ReadById(string id)
        {
            return await GetDecks()
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<IEnumerable<Deck>> ReadByInstallation(string installationCode)
        {
            var installation = await _installationService.ReadByName(installationCode);
            if (installation == null)
                return new List<Deck>();
            return await _context.Decks.Where(a =>
                a.Installation.Id.Equals(installation.Id)).ToListAsync();
        }

        public async Task<Deck?> ReadByName(string deckName)
        {
            if (deckName == null)
                return null;
            return await _context.Decks.Where(a =>
                a.Name.ToLower().Equals(deckName.ToLower())
            ).FirstOrDefaultAsync();
        }

        public async Task<Deck?> ReadByInstallationAndPlantAndName(Installation installation, Plant plant, string name)
        {
            return await _context.Decks.Where(a =>
                a.Plant.Id.Equals(plant.Id) &&
                a.Installation.Id.Equals(installation.Id) &&
                a.Name.ToLower().Equals(name.ToLower())
            ).Include(d => d.Plant).Include(i => i.Installation).FirstOrDefaultAsync();
        }

        public async Task<Deck> Create(CreateDeckQuery newDeckQuery)
        {
            var installation = await _installationService.ReadByName(newDeckQuery.InstallationCode);
            if (installation == null)
            {
                throw new InstallationNotFoundException($"No installation with name {newDeckQuery.InstallationCode} could be found");
            }
            var plant = await _plantService.ReadByInstallationAndName(installation, newDeckQuery.PlantCode);
            if (plant == null)
            {
                throw new PlantNotFoundException($"No plant with name {newDeckQuery.PlantCode} could be found");
            }
            var deck = await ReadByInstallationAndPlantAndName(installation, plant, newDeckQuery.Name);
            if (deck == null)
            {
                deck = new Deck
                {
                    Name = newDeckQuery.Name,
                    Installation = installation,
                    Plant = plant
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
