using System.Globalization;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IInstallationService
    {
        public Task<IList<Installation>> ReadAll(bool readOnly = true);

        public Task<Installation?> ReadById(string id, bool readOnly = true);

        public Task<Installation?> ReadByInstallationCode(
            string installation,
            bool readOnly = true
        );

        public Task<Installation> Create(CreateInstallationQuery newInstallation);

        public Task<Installation?> Delete(string id);

        public Task AssertRobotIsOnSameInstallationAsMission(
            Robot robot,
            MissionDefinition missionDefinition
        );

        public void DetachTracking(FlotillaDbContext context, Installation installation);
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
    public class InstallationService(
        FlotillaDbContext context,
        IAccessRoleService accessRoleService,
        ILogger<InstallationService> logger
    ) : IInstallationService
    {
        public async Task<IList<Installation>> ReadAll(bool readOnly = true)
        {
            var query = await GetInstallations(readOnly: readOnly);
            return await query.ToListAsync();
        }

        private async Task<IQueryable<Installation>> GetInstallations(bool readOnly = true)
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes(
                AccessMode.Read
            );
            var query = context.Installations.Where(i =>
                accessibleInstallationCodes.Contains(i.InstallationCode.ToUpper())
            );
            return readOnly ? query.AsNoTracking() : query.AsTracking();
        }

        private async Task ApplyUnprotectedDatabaseUpdate()
        {
            await context.SaveChangesAsync();
        }

        private async Task ApplyDatabaseUpdate(Installation? installation)
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes(
                AccessMode.Write
            );
            if (
                installation == null
                || accessibleInstallationCodes.Contains(
                    installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)
                )
            )
                await context.SaveChangesAsync();
            else
                throw new UnauthorizedAccessException(
                    $"User does not have permission to update installation in installation {installation.Name}"
                );
        }

        public async Task<Installation?> ReadById(string id, bool readOnly = true)
        {
            var query = await GetInstallations(readOnly: readOnly);
            return await query.FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<Installation?> ReadByInstallationCode(
            string installationCode,
            bool readOnly = true
        )
        {
            if (installationCode == null)
                return null;
            var query = await GetInstallations(readOnly: readOnly);
            return await query
                .Where(a => a.InstallationCode.ToLower().Equals(installationCode.ToLower()))
                .FirstOrDefaultAsync();
        }

        public async Task<Installation> Create(CreateInstallationQuery newInstallationQuery)
        {
            var installation = await ReadByInstallationCode(
                newInstallationQuery.InstallationCode,
                readOnly: true
            );
            if (installation == null)
            {
                installation = new Installation
                {
                    Name = newInstallationQuery.Name,
                    InstallationCode = newInstallationQuery.InstallationCode,
                };
                await context.Installations.AddAsync(installation);
                await ApplyUnprotectedDatabaseUpdate();
                DetachTracking(context, installation);
            }

            return installation;
        }

        public async Task<Installation?> Delete(string id)
        {
            var query = await GetInstallations();
            var installation = await query.FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (installation is null)
            {
                return null;
            }

            context.Installations.Remove(installation);
            await ApplyDatabaseUpdate(installation);

            return installation;
        }

        public async Task AssertRobotIsOnSameInstallationAsMission(
            Robot robot,
            MissionDefinition missionDefinition
        )
        {
            var missionInstallation = await ReadByInstallationCode(
                missionDefinition.InstallationCode
            );

            if (missionInstallation is null)
            {
                string errorMessage =
                    $"Could not find installation for installation code {missionDefinition.InstallationCode}";
                logger.LogError("{Message}", errorMessage);
                throw new InstallationNotFoundException(errorMessage);
            }

            if (robot.CurrentInstallation.Id != missionInstallation.Id)
            {
                string errorMessage =
                    $"The robot {robot.Name} is on installation {robot.CurrentInstallation.Name} which is not the same as the mission installation {missionInstallation.Name}";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotInSameInstallationAsMissionException(errorMessage);
            }
        }

        public void DetachTracking(FlotillaDbContext context, Installation installation)
        {
            context.Entry(installation).State = EntityState.Detached;
        }
    }
}
