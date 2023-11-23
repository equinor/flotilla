using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IAccessRoleService
    {
        public Task<List<string>> GetAllowedInstallationCodes();
    }

    public class AccessRoleService(FlotillaDbContext context, IHttpContextAccessor httpContextAccessor, IInstallationService installationService) : IAccessRoleService
    {
        private const string SUPER_ADMIN_ROLE_NAME  = "Role.Admin";

        public async Task<List<string>> GetAllowedInstallationCodes()
        {
            var dbRoles = await context.AccessRoles.Include((r) => r.Installation).ToListAsync();
            var roles = httpContextAccessor.HttpContext!.GetRequestedRoleNames();

            if (roles.Contains(SUPER_ADMIN_ROLE_NAME))
            {
                return (await installationService.ReadAll()).Select((i) => i.InstallationCode).ToList();
            }
            else
            {
                return await context.AccessRoles.Include((r) => r.Installation)
                    .Where((r) => roles.Contains(r.RoleName)).Select((r) => r.Installation != null ? r.Installation.InstallationCode : "").ToListAsync();
            }
        }
    }
}
