using Api.Database.Context;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IAccessRoleService
    {
        public Task<List<string>> GetAllowedInstallationCodes();
        public Task<List<string>> GetAllowedInstallationCodes(List<string> roles);
    }

    public class AccessRoleService(FlotillaDbContext context, IHttpContextAccessor httpContextAccessor) : IAccessRoleService
    {
        private const string SUPER_ADMIN_ROLE_NAME = "Role.Admin";

        public async Task<List<string>> GetAllowedInstallationCodes()
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return await context.Installations.Select((i) => i.InstallationCode.ToUpperInvariant()).ToListAsync();
            }

            var roles = httpContextAccessor.HttpContext.GetRequestedRoleNames();

            return await GetAllowedInstallationCodes(roles);
        }

        public async Task<List<string>> GetAllowedInstallationCodes(List<string> roles)
        {
            var dbRoles = await context.AccessRoles.Include((r) => r.Installation).ToListAsync();

            if (roles.Contains(SUPER_ADMIN_ROLE_NAME))
            {
                return await context.Installations.Select((i) => i.InstallationCode.ToUpperInvariant()).ToListAsync();
            }
            else
            {
                return await context.AccessRoles.Include((r) => r.Installation)
                    .Where((r) => roles.Contains(r.RoleName)).Select((r) => r.Installation != null ? r.Installation.InstallationCode.ToUpperInvariant() : "").ToListAsync();
            }
        }
    }
}
