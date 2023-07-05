using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IInstallationService
    {
        public abstract Task<IEnumerable<Installation>> ReadAll();

        public abstract Task<Installation?> ReadById(string id);

        public abstract Task<IEnumerable<Installation>> ReadByAsset(string assetCode);

        public abstract Task<Installation?> ReadByAssetAndName(Asset asset, string installationCode);

        public abstract Task<Installation?> ReadByAssetAndName(string assetCode, string installationCode);

        public abstract Task<Installation> Create(CreateInstallationQuery newInstallation);

        public abstract Task<Installation> Update(Installation installation);

        public abstract Task<Installation?> Delete(string id);

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
    public class InstallationService : IInstallationService
    {
        private readonly FlotillaDbContext _context;
        private readonly IAssetService _assetService;

        public InstallationService(FlotillaDbContext context, IAssetService assetService)
        {
            _context = context;
            _assetService = assetService;
        }

        public async Task<IEnumerable<Installation>> ReadAll()
        {
            return await GetInstallations().ToListAsync();
        }

        private IQueryable<Installation> GetInstallations()
        {
            return _context.Installations.Include(i => i.Asset);
        }

        public async Task<Installation?> ReadById(string id)
        {
            return await GetInstallations()
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<IEnumerable<Installation>> ReadByAsset(string assetCode)
        {
            var asset = await _assetService.ReadByName(assetCode);
            if (asset == null)
                return new List<Installation>();
            return await _context.Installations.Where(a =>
                a.Asset.Id.Equals(asset.Id)).ToListAsync();
        }

        public async Task<Installation?> ReadByAssetAndName(Asset asset, string installationCode)
        {
            return await _context.Installations.Where(a =>
                a.InstallationCode.ToLower().Equals(installationCode.ToLower()) &&
                a.Asset.Id.Equals(asset.Id)).FirstOrDefaultAsync();
        }

        public async Task<Installation?> ReadByAssetAndName(string assetCode, string installationCode)
        {
            var asset = await _assetService.ReadByName(assetCode);
            if (asset == null)
                return null;
            return await _context.Installations.Where(a =>
                a.Asset.Id.Equals(asset.Id) &&
                a.InstallationCode.ToLower().Equals(installationCode.ToLower())
            ).FirstOrDefaultAsync();
        }

        public async Task<Installation> Create(CreateInstallationQuery newInstallationQuery)
        {
            var asset = await _assetService.ReadByName(newInstallationQuery.AssetCode);
            if (asset == null)
            {
                throw new AssetNotFoundException($"No asset with name {newInstallationQuery.AssetCode} could be found");
            }
            var installation = await ReadByAssetAndName(asset, newInstallationQuery.InstallationCode);
            if (installation == null)
            {
                installation = new Installation
                {
                    Name = newInstallationQuery.Name,
                    InstallationCode = newInstallationQuery.InstallationCode,
                    Asset = asset,
                };
                await _context.Installations.AddAsync(installation);
                await _context.SaveChangesAsync();
            }
            return installation!;
        }

        public async Task<Installation> Update(Installation installation)
        {
            var entry = _context.Update(installation);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<Installation?> Delete(string id)
        {
            var installation = await GetInstallations()
                .FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (installation is null)
            {
                return null;
            }

            _context.Installations.Remove(installation);
            await _context.SaveChangesAsync();

            return installation;
        }
    }
}
