using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IAssetService
    {
        public abstract Task<IEnumerable<Asset>> ReadAll();

        public abstract Task<Asset?> ReadById(string id);

        public abstract Task<Asset?> ReadByName(string asset);

        public abstract Task<Asset> Create(CreateAssetQuery newAsset);

        public abstract Task<Asset> Update(Asset asset);

        public abstract Task<Asset?> Delete(string id);

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
    public class AssetService : IAssetService
    {
        private readonly FlotillaDbContext _context;

        public AssetService(FlotillaDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Asset>> ReadAll()
        {
            return await GetAssets().ToListAsync();
        }

        private IQueryable<Asset> GetAssets()
        {
            return _context.Assets;
        }

        public async Task<Asset?> ReadById(string id)
        {
            return await GetAssets()
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<Asset?> ReadByName(string assetCode)
        {
            if (assetCode == null)
                return null;
            return await _context.Assets.Where(a =>
                a.AssetCode.ToLower().Equals(assetCode.ToLower())
            ).FirstOrDefaultAsync();
        }

        public async Task<Asset> Create(CreateAssetQuery newAssetQuery)
        {
            var asset = await ReadByName(newAssetQuery.AssetCode);
            if (asset == null)
            {
                asset = new Asset
                {
                    Name = newAssetQuery.Name,
                    AssetCode = newAssetQuery.AssetCode
                };
                await _context.Assets.AddAsync(asset);
                await _context.SaveChangesAsync();
            }

            return asset!;
        }

        public async Task<Asset> Update(Asset asset)
        {
            var entry = _context.Update(asset);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<Asset?> Delete(string id)
        {
            var asset = await GetAssets()
                .FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (asset is null)
            {
                return null;
            }

            _context.Assets.Remove(asset);
            await _context.SaveChangesAsync();

            return asset;
        }
    }
}
