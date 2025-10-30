using System.Security.Claims;
using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public enum AccessMode
    {
        Read,
        Write,
    }

    public interface IAccessRoleService
    {
        public Task<List<string>> GetAllowedInstallationCodes(AccessMode accessMode);
        public Task<List<string>> GetAllowedInstallationCodes(
            ClaimsPrincipal user,
            AccessMode accessMode
        );
        public bool IsUserAdmin();
        public bool IsAuthenticationAvailable();
        public Task<AccessRole> Create(
            Installation installation,
            string roleName,
            RoleAccessLevel accessLevel
        );
        public Task<AccessRole?> ReadByInstallation(Installation installation);
        public Task<List<AccessRole>> ReadAll();
        public void DetachTracking(FlotillaDbContext context, AccessRole accessRole);
    }

    public class AccessRoleService(
        FlotillaDbContext context,
        IHttpContextAccessor httpContextAccessor
    ) : IAccessRoleService
    {
        private const string SUPER_ADMIN_ROLE_NAME = "Role.Admin";

        private IQueryable<AccessRole> GetAccessRoles(bool readOnly = true)
        {
            return readOnly ? context.AccessRoles.AsNoTracking() : context.AccessRoles.AsTracking();
        }

        public async Task<List<string>> GetAllowedInstallationCodes(AccessMode accessMode)
        {
            var user = httpContextAccessor.HttpContext?.User;

            return await GetAllowedInstallationCodes(user, accessMode);
        }

        public async Task<List<string>> GetAllowedInstallationCodes(
            ClaimsPrincipal? user,
            AccessMode accessMode
        )
        {
            if (user == null)
                return await context
                    .Installations.AsNoTracking()
                    .Select(i => i.InstallationCode.ToUpperInvariant())
                    .ToListAsync();

            if (user.IsInRole(SUPER_ADMIN_ROLE_NAME))
                return await context
                    .Installations.AsNoTracking()
                    .Select(i => i.InstallationCode.ToUpperInvariant())
                    .ToListAsync();

            var userRoles = user
                .Claims.Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            var allowedInstallationCodes = await GetAllowedInstallationCodes(userRoles, accessMode);

            return allowedInstallationCodes;
        }

        private async Task<List<string>> EnsureUserRolesExistInDatabase(List<string> roles)
        {
            var dbRoles = await GetAccessRoles(readOnly: true)
                .Include(r => r.Installation)
                .Select(r => r.RoleName)
                .ToListAsync();

            var intersection = roles.Intersect(dbRoles, StringComparer.OrdinalIgnoreCase).ToList();

            return intersection;
        }

        private static List<string> GetInstallationCodesByAccessMode(
            List<string> roles,
            AccessMode accessMode
        )
        {
            List<string> permittedInstallationCodes = [];

            foreach (var role in roles)
            {
                switch (accessMode)
                {
                    case AccessMode.Read:
                        if (role.StartsWith("Role.ReadOnly.") || role.StartsWith("Role.User."))
                        {
                            var installationCode = TryExtractInstallationCode(role);
                            if (installationCode is null)
                                continue;
                            permittedInstallationCodes.Add(installationCode);
                        }
                        break;

                    case AccessMode.Write:
                        if (role.StartsWith("Role.User."))
                        {
                            var installationCode = TryExtractInstallationCode(role);
                            if (installationCode is null)
                                continue;
                            permittedInstallationCodes.Add(installationCode);
                        }
                        break;
                }
            }

            return [.. permittedInstallationCodes.Distinct()];
        }

        private static string? TryExtractInstallationCode(string role)
        {
            var parts = role.Split('.');
            if (parts.Length != 3)
                return null;

            var code = parts[2];
            return code.Length == 3 ? code : null;
        }

        public async Task<List<string>> GetAllowedInstallationCodes(
            List<string> roles,
            AccessMode accessMode
        )
        {
            var rolesInUserAndDb = await EnsureUserRolesExistInDatabase(roles);

            var permittedInstallationCodes = GetInstallationCodesByAccessMode(
                rolesInUserAndDb,
                accessMode
            );

            return permittedInstallationCodes;
        }

        private void ThrowExceptionIfNotAdmin()
        {
            if (httpContextAccessor.HttpContext == null)
                throw new HttpRequestException(
                    "Access roles can only be created in authenticated HTTP requests"
                );

            if (!httpContextAccessor.HttpContext.User.IsInRole(SUPER_ADMIN_ROLE_NAME))
                throw new HttpRequestException(
                    "This user is not authorised to create a new access role"
                );
        }

        public async Task<AccessRole> Create(
            Installation installation,
            string roleName,
            RoleAccessLevel accessLevel
        )
        {
            if (accessLevel == RoleAccessLevel.ADMIN)
                throw new HttpRequestException("Cannot create admin roles using database services");
            ThrowExceptionIfNotAdmin();

            context.Entry(installation).State = EntityState.Unchanged;

            var newAccessRole = new AccessRole
            {
                Installation = installation,
                RoleName = roleName,
                AccessLevel = accessLevel,
            };

            await context.AccessRoles.AddAsync(newAccessRole);
            await context.SaveChangesAsync();
            DetachTracking(context, newAccessRole);
            return newAccessRole!;
        }

        public async Task<AccessRole?> ReadByInstallation(Installation installation)
        {
            ThrowExceptionIfNotAdmin();
            return await GetAccessRoles(readOnly: true)
                .Include(r => r.Installation)
                .Where(r => r.Installation.Id == installation.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<List<AccessRole>> ReadAll()
        {
            ThrowExceptionIfNotAdmin();
            return await GetAccessRoles(readOnly: true).Include(r => r.Installation).ToListAsync();
        }

        public bool IsUserAdmin()
        {
            return httpContextAccessor.HttpContext?.User?.IsInRole(SUPER_ADMIN_ROLE_NAME) ?? false;
        }

        public bool IsAuthenticationAvailable()
        {
            return httpContextAccessor.HttpContext != null;
        }

        public void DetachTracking(FlotillaDbContext context, AccessRole accessRole)
        {
            context.Entry(accessRole).State = EntityState.Detached;
        }
    }
}
