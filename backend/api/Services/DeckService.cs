﻿using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;
namespace Api.Services
{
    public interface IDeckService
    {
        public Task<IEnumerable<Deck>> ReadAll(bool readOnly = false);

        public Task<Deck?> ReadById(string id, bool readOnly = false);

        public Task<IEnumerable<Deck>> ReadByInstallation(string installationCode, bool readOnly = false);

        public Task<Deck?> ReadByName(string deckName, bool readOnly = false);

        public Task<Deck?> ReadByInstallationAndPlantAndName(Installation installation, Plant plant, string deckName, bool readOnly = false);

        public Task<Deck> Create(CreateDeckQuery newDeck);

        public Task<Deck> Update(Deck deck);

        public Task<Deck?> Delete(string id);
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
    public class DeckService(
        FlotillaDbContext context,
        IDefaultLocalizationPoseService defaultLocalizationPoseService,
        IInstallationService installationService,
        IPlantService plantService,
        IAccessRoleService accessRoleService,
        ISignalRService signalRService) : IDeckService
    {
        public async Task<IEnumerable<Deck>> ReadAll(bool readOnly = false)
        {
            return await GetDecks(readOnly: readOnly).ToListAsync();
        }

        public async Task<Deck?> ReadById(string id, bool readOnly = false)
        {
            return await GetDecks(readOnly: readOnly)
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<IEnumerable<Deck>> ReadByInstallation(string installationCode, bool readOnly = false)
        {
            var installation = await installationService.ReadByName(installationCode, readOnly: true);
            if (installation == null) { return new List<Deck>(); }
            return await GetDecks(readOnly: readOnly).Where(a =>
                a.Installation != null && a.Installation.Id.Equals(installation.Id)).ToListAsync();
        }

        public async Task<Deck?> ReadByName(string deckName, bool readOnly = false)
        {
            if (deckName == null) { return null; }
            return await GetDecks(readOnly: readOnly).Where(a =>
                a.Name.ToLower().Equals(deckName.ToLower())
            ).FirstOrDefaultAsync();
        }

        public async Task<Deck?> ReadByInstallationAndPlantAndName(Installation installation, Plant plant, string name, bool readOnly = false)
        {
            return await GetDecks(readOnly: readOnly).Where(a =>
                a.Plant != null && a.Plant.Id.Equals(plant.Id) &&
                a.Installation != null && a.Installation.Id.Equals(installation.Id) &&
                a.Name.ToLower().Equals(name.ToLower())
            ).Include(d => d.Plant).Include(i => i.Installation).FirstOrDefaultAsync();
        }

        public async Task<Deck> Create(CreateDeckQuery newDeckQuery)
        {
            var installation = await installationService.ReadByName(newDeckQuery.InstallationCode, readOnly: true) ??
                               throw new InstallationNotFoundException($"No installation with name {newDeckQuery.InstallationCode} could be found");
            var plant = await plantService.ReadByInstallationAndName(installation, newDeckQuery.PlantCode, readOnly: true) ??
                        throw new PlantNotFoundException($"No plant with name {newDeckQuery.PlantCode} could be found");
            var existingDeck = await ReadByInstallationAndPlantAndName(installation, plant, newDeckQuery.Name, readOnly: true);

            if (existingDeck != null)
            {
                throw new DeckExistsException($"Deck with name {newDeckQuery.Name} already exists");
            }

            DefaultLocalizationPose? defaultLocalizationPose = null;
            if (newDeckQuery.DefaultLocalizationPose != null)
            {
                defaultLocalizationPose = await defaultLocalizationPoseService.Create(new DefaultLocalizationPose(newDeckQuery.DefaultLocalizationPose.Value.Pose, newDeckQuery.DefaultLocalizationPose.Value.IsDockingStation));
            }

            var deck = new Deck
            {
                Name = newDeckQuery.Name,
                Installation = installation,
                Plant = plant,
                DefaultLocalizationPose = defaultLocalizationPose
            };

            context.Entry(deck.Installation).State = EntityState.Unchanged;
            context.Entry(deck.Plant).State = EntityState.Unchanged;
            if (deck.DefaultLocalizationPose is not null) { context.Entry(deck.DefaultLocalizationPose).State = EntityState.Modified; }

            await context.Decks.AddAsync(deck);
            await ApplyDatabaseUpdate(deck.Installation);
            _ = signalRService.SendMessageAsync("Deck created", deck.Installation, new DeckResponse(deck));
            return deck!;
        }

        public async Task<Deck> Update(Deck deck)
        {
            var entry = context.Update(deck);
            await ApplyDatabaseUpdate(deck.Installation);
            _ = signalRService.SendMessageAsync("Deck updated", deck.Installation, new DeckResponse(deck));
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

            context.Decks.Remove(deck);
            await ApplyDatabaseUpdate(deck.Installation);
            _ = signalRService.SendMessageAsync("Deck deleted", deck.Installation, new DeckResponse(deck));

            return deck;
        }

        private IQueryable<Deck> GetDecks(bool readOnly = false)
        {
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var query = context.Decks.Include(p => p.Plant).Include(i => i.Installation).Include(d => d.DefaultLocalizationPose)
                .Where((d) => accessibleInstallationCodes.Result.Contains(d.Installation.InstallationCode.ToUpper()));
            return readOnly ? query.AsNoTracking() : query.AsTracking();
        }

        private async Task ApplyDatabaseUpdate(Installation? installation)
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes();
            if (installation == null || accessibleInstallationCodes.Contains(installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)))
                await context.SaveChangesAsync();
            else
                throw new UnauthorizedAccessException($"User does not have permission to update deck in installation {installation.Name}");
        }
    }
}
