using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IInstallationService
    {
        public abstract Task<IEnumerable<Installation>> ReadAll();

        public abstract Task<Installation?> ReadById(string id);

        public abstract Task<Installation?> ReadByName(string installation);

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
    public class InstallationService(FlotillaDbContext context, IAccessRoleService accessRoleService) : IInstallationService
    {
        public async Task<IEnumerable<Installation>> ReadAll()
        {
            return await GetInstallations().ToListAsync();
        }

        private IQueryable<Installation> GetInstallations()
        {
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            return context.Installations
                .Where((i) => accessibleInstallationCodes.Result.Contains(i.InstallationCode));
        }

        public async Task<Installation?> ReadById(string id)
        {
            return await GetInstallations()
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<Installation?> ReadByName(string installationCode)
        {
            if (installationCode == null)
                return null;
            return await context.Installations.Where(a =>
                a.InstallationCode.ToLower().Equals(installationCode.ToLower())
            ).FirstOrDefaultAsync();
        }

        public async Task<Installation> Create(CreateInstallationQuery newInstallationQuery)
        {
            var installation = await ReadByName(newInstallationQuery.InstallationCode);
            if (installation == null)
            {
                installation = new Installation
                {
                    Name = newInstallationQuery.Name,
                    InstallationCode = newInstallationQuery.InstallationCode
                };
                await context.Installations.AddAsync(installation);
                await context.SaveChangesAsync();
            }

            return installation!;
        }

        public async Task<Installation> Update(Installation installation)
        {
            var entry = context.Update(installation);
            await context.SaveChangesAsync();
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

            context.Installations.Remove(installation);
            await context.SaveChangesAsync();

            return installation;
        }
    }
}
