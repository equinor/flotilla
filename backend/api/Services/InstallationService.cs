using System.Globalization;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IInstallationService
    {
        public abstract Task<IEnumerable<Installation>> ReadAll(bool readOnly = false);

        public abstract Task<Installation?> ReadById(string id, bool readOnly = false);

        public abstract Task<Installation?> ReadByName(string installation, bool readOnly = false);

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
        public async Task<IEnumerable<Installation>> ReadAll(bool readOnly = false)
        {
            return await GetInstallations(readOnly: readOnly).ToListAsync();
        }

        private IQueryable<Installation> GetInstallations(bool readOnly = false)
        {
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var query = context.Installations
                .Where((i) => accessibleInstallationCodes.Result.Contains(i.InstallationCode.ToUpper()));
            return readOnly ? query.AsNoTracking() : query;
        }

        private async Task ApplyUnprotectedDatabaseUpdate()
        {
            await context.SaveChangesAsync();
        }

        private async Task ApplyDatabaseUpdate(Installation? installation)
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes();
            if (installation == null || accessibleInstallationCodes.Contains(installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)))
                await context.SaveChangesAsync();
            else
                throw new UnauthorizedAccessException($"User does not have permission to update installation in installation {installation.Name}");
        }

        public async Task<Installation?> ReadById(string id, bool readOnly = false)
        {
            return await GetInstallations(readOnly: readOnly)
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<Installation?> ReadByName(string installationCode, bool readOnly = false)
        {
            if (installationCode == null)
                return null;
            return await GetInstallations(readOnly: readOnly).Where(a =>
                a.InstallationCode.ToLower().Equals(installationCode.ToLower())
            ).FirstOrDefaultAsync();
        }

        public async Task<Installation> Create(CreateInstallationQuery newInstallationQuery)
        {
            var installation = await ReadByName(newInstallationQuery.InstallationCode, readOnly: true);
            if (installation == null)
            {
                installation = new Installation
                {
                    Name = newInstallationQuery.Name,
                    InstallationCode = newInstallationQuery.InstallationCode
                };
                await context.Installations.AddAsync(installation);
                await ApplyUnprotectedDatabaseUpdate();
            }

            return installation;
        }

        public async Task<Installation> Update(Installation installation)
        {
            var entry = context.Update(installation);
            await ApplyDatabaseUpdate(installation);
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
            await ApplyDatabaseUpdate(installation);

            return installation;
        }
    }
}
