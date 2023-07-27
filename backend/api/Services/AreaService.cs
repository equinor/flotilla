﻿using Api.Controllers.Models;
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

        public abstract Task<IEnumerable<Area>> ReadByInstallation(string installationCode);

        public abstract Task<Area?> ReadByInstallationAndName(string installationCode, string areaName);

        public abstract Task<Area> Create(CreateAreaQuery newArea);

        public abstract Task<Area> Create(CreateAreaQuery newArea, List<Pose> safePositions);

        public abstract Task<Area> Update(Area area);

        public abstract Task<Area?> AddSafePosition(string installationCode, string areaName, SafePosition safePosition);

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
        private readonly IInstallationService _installationService;
        private readonly IPlantService _plantService;
        private readonly IDeckService _deckService;

        public AreaService(
            FlotillaDbContext context, IInstallationService installationService, IPlantService plantService, IDeckService deckService)
        {
            _context = context;
            _installationService = installationService;
            _plantService = plantService;
            _deckService = deckService;
        }

        public async Task<IEnumerable<Area>> ReadAll()
        {
            return await GetAreas().ToListAsync();
        }

        private IQueryable<Area> GetAreas()
        {
            return _context.Areas.Include(a => a.SafePositions)
                .Include(a => a.Deck).Include(d => d.Plant).Include(i => i.Installation);
        }

        public async Task<Area?> ReadById(string id)
        {
            return await GetAreas()
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<Area?> ReadByInstallationAndName(Installation? installation, string areaName)
        {
            if (installation == null)
                return null;

            return await _context.Areas.Where(a =>
                a.Name.ToLower().Equals(areaName.ToLower()) &&
                a.Installation.Id.Equals(installation.Id)
            ).Include(a => a.SafePositions).Include(a => a.Installation)
                .Include(a => a.Plant).Include(a => a.Deck).FirstOrDefaultAsync();
        }

        public async Task<Area?> ReadByInstallationAndName(string installationCode, string areaName)
        {
            var installation = await _installationService.ReadByName(installationCode);
            if (installation == null)
                return null;

            return await _context.Areas.Where(a =>
                a.Installation.Id.Equals(installation.Id) &&
                a.Name.ToLower().Equals(areaName.ToLower())
            ).Include(a => a.SafePositions).Include(a => a.Installation)
                .Include(a => a.Plant).Include(a => a.Deck).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Area>> ReadByInstallation(string installationCode)
        {
            var installation = await _installationService.ReadByName(installationCode);
            if (installation == null)
                return new List<Area>();

            return await _context.Areas.Where(a =>
                a.Installation.Id.Equals(installation.Id)).Include(a => a.SafePositions).Include(a => a.Installation)
                .Include(a => a.Plant).Include(a => a.Deck).ToListAsync();
        }

        public async Task<Area?> ReadByInstallationAndPlantAndDeckAndName(Installation? installation, Plant? plant, Deck? deck, string areaName)
        {
            if (installation == null || plant == null || deck == null)
                return null;

            return await _context.Areas.Where(a =>
                a.Deck.Id.Equals(deck.Id) &&
                a.Plant.Id.Equals(plant.Id) &&
                a.Installation.Id.Equals(installation.Id) &&
                a.Name.ToLower().Equals(areaName.ToLower())
            ).Include(a => a.Deck).Include(d => d.Plant).Include(i => i.Installation)
                .Include(a => a.SafePositions).FirstOrDefaultAsync();
        }

        public async Task<Area> Create(CreateAreaQuery newAreaQuery, List<Pose> positions)
        {
            var safePositions = new List<SafePosition>();
            foreach (var pose in positions)
            {
                safePositions.Add(new SafePosition(pose));
            }

            var installation = await _installationService.ReadByName(newAreaQuery.InstallationCode);
            if (installation == null)
            {
                throw new InstallationNotFoundException($"No installation with name {newAreaQuery.InstallationCode} could be found");
            }

            var plant = await _plantService.ReadByInstallationAndName(installation, newAreaQuery.PlantCode);
            if (plant == null)
            {
                throw new PlantNotFoundException($"No plant with name {newAreaQuery.PlantCode} could be found");
            }

            var deck = await _deckService.ReadByInstallationAndPlantAndName(installation, plant, newAreaQuery.DeckName);
            if (deck == null)
            {
                throw new DeckNotFoundException($"No deck with name {newAreaQuery.DeckName} could be found");
            }

            var existingArea = await ReadByInstallationAndPlantAndDeckAndName(
                installation, plant, deck, newAreaQuery.AreaName);
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
                Plant = plant,
                Installation = installation
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

        public async Task<Area?> AddSafePosition(string installationCode, string areaName, SafePosition safePosition)
        {
            var area = await ReadByInstallationAndName(installationCode, areaName);
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
