using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IAccessRoleService
    {
        public Task<List<string>> GetAllowedInstallationCodes();
        public Task<List<string>> GetAllowedInstallationCodes(List<string> roles);
        public bool IsUserAdmin();
        public bool IsAuthenticationAvailable();
        public Task<AccessRole> Create(Installation installation, string roleName, RoleAccessLevel accessLevel);
        public Task<AccessRole?> ReadByInstallation(Installation installation);
        public Task<List<AccessRole>> ReadAll();
    }

    public class AccessRoleService(FlotillaDbContext context, IHttpContextAccessor httpContextAccessor) : IAccessRoleService
    {
        private const string SUPER_ADMIN_ROLE_NAME = "Role.Admin";

        public async Task<List<string>> GetAllowedInstallationCodes()
        {
            if (httpContextAccessor.HttpContext == null)
                return await context.Installations.Select((i) => i.InstallationCode.ToUpperInvariant()).ToListAsync();

            var roles = httpContextAccessor.HttpContext.GetRequestedRoleNames();

            return await GetAllowedInstallationCodes(roles);
        }

        public async Task<List<string>> GetAllowedInstallationCodes(List<string> roles)
        {
            if (roles.Contains(SUPER_ADMIN_ROLE_NAME))
                return await context.Installations.Select((i) => i.InstallationCode.ToUpperInvariant()).ToListAsync();
            else
                return await context.AccessRoles.Include((r) => r.Installation)
                    .Where((r) => roles.Contains(r.RoleName)).Select((r) => r.Installation != null ? r.Installation.InstallationCode.ToUpperInvariant() : "").ToListAsync();
        }

        private void ThrowExceptionIfNotAdmin()
        {
            if (httpContextAccessor.HttpContext == null)
                throw new HttpRequestException("Access roles can only be created in authenticated HTTP requests");

            var roles = httpContextAccessor.HttpContext.GetRequestedRoleNames();
            if (!roles.Contains(SUPER_ADMIN_ROLE_NAME))
                throw new HttpRequestException("This user is not authorised to create a new access role");
        }

        public async Task<AccessRole> Create(Installation installation, string roleName, RoleAccessLevel accessLevel)
        {
            if (accessLevel == RoleAccessLevel.ADMIN)
                throw new HttpRequestException("Cannot create admin roles using database services");
            ThrowExceptionIfNotAdmin();

            context.Entry(installation).State = EntityState.Unchanged;

            var newAccessRole = new AccessRole()
            {
                Installation = installation,
                RoleName = roleName,
                AccessLevel = accessLevel
            };

            await context.AccessRoles.AddAsync(newAccessRole);
            await context.SaveChangesAsync();
            return newAccessRole!;
        }

        public async Task<AccessRole?> ReadByInstallation(Installation installation)
        {
            ThrowExceptionIfNotAdmin();
            return await context.AccessRoles.Include((r) => r.Installation).Where((r) => r.Installation.Id == installation.Id).FirstOrDefaultAsync();
        }

        public async Task<List<AccessRole>> ReadAll()
        {
            ThrowExceptionIfNotAdmin();
            return await context.AccessRoles.Include((r) => r.Installation).ToListAsync();
        }

        public bool IsUserAdmin()
        {
            if (!IsAuthenticationAvailable())
                return false;
            var roles = httpContextAccessor.HttpContext!.GetRequestedRoleNames();
            return roles.Contains(SUPER_ADMIN_ROLE_NAME);
        }

        public bool IsAuthenticationAvailable()
        {
            return httpContextAccessor.HttpContext != null;
        }
    }
}
