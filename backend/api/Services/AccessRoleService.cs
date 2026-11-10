using System.Security.Claims;
using Api.Database.Context;
using Api.Services.Models;
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
        public Task<UserRoles> GetUserRoles();
    }

    public class AccessRoleService(
        FlotillaDbContext context,
        IHttpContextAccessor httpContextAccessor
    ) : IAccessRoleService
    {
        private const string SUPER_ADMIN_ROLE_NAME = "Role.Admin";

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

            var allowedInstallationCodes = GetInstallationCodesByAccessMode(userRoles, accessMode);

            return allowedInstallationCodes;
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

        private static List<InstallationRole> GetInstallationRoles(List<string> roles)
        {
            List<InstallationRole> accesses = [];

            foreach (var role in roles)
            {
                var installationCode = TryExtractInstallationCode(role);
                if (installationCode is null)
                    continue;

                var existingRole = accesses.FirstOrDefault(
                    (r) => r.InstallationCode == installationCode
                );

                if (
                    role.StartsWith("Role.ReadOnly.")
                    && existingRole.Equals(default(InstallationRole))
                )
                {
                    accesses.Add(
                        new InstallationRole
                        {
                            InstallationCode = installationCode,
                            HasWriteAccess = false,
                        }
                    );
                }

                if (role.StartsWith("Role.User."))
                {
                    if (existingRole.Equals(default(InstallationRole)))
                        accesses.Add(
                            new InstallationRole
                            {
                                InstallationCode = installationCode,
                                HasWriteAccess = true,
                            }
                        );
                    else
                        existingRole.HasWriteAccess = true;
                }
            }

            return accesses;
        }

        private static string? TryExtractInstallationCode(string role)
        {
            var parts = role.Split('.');
            if (parts.Length != 3)
                return null;

            var code = parts[2];
            return code.Length == 3 ? code : null;
        }

        public async Task<UserRoles> GetUserRoles()
        {
            var user =
                (httpContextAccessor.HttpContext?.User)
                ?? throw new HttpRequestException(
                    HttpRequestError.UserAuthenticationError,
                    "No user credentials available"
                );

            var userRoles = user
                .Claims.Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            if (user.IsInRole(SUPER_ADMIN_ROLE_NAME))
            {
                return new UserRoles
                {
                    IsAdmin = true,
                    InstallationRoles = GetInstallationRoles(userRoles),
                };
            }

            return new UserRoles
            {
                IsAdmin = false,
                InstallationRoles = GetInstallationRoles(userRoles),
            };
        }
    }
}
