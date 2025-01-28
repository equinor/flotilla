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
        public abstract Task<IEnumerable<Installation>> ReadAll(bool readOnly = true);

        public abstract Task<Installation?> ReadById(string id, bool readOnly = true);

        public abstract Task<Installation?> ReadByInstallationCode(
            string installation,
            bool readOnly = true
        );

        public abstract Task<Installation> Create(CreateInstallationQuery newInstallation);

        public abstract Task<Installation> Update(Installation installation);

        public abstract Task<Installation?> Delete(string id);

        public void DetachTracking(FlotillaDbContext context, Installation installation);

        public void RobotIsOnSameInstallationAsMission(
            Robot robot,
            MissionDefinition missionDefinition
        );
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class InstallationService(
        FlotillaDbContext context,
        IAccessRoleService accessRoleService,
        ILogger<InstallationService> logger
    ) : IInstallationService
    {
        public async Task<IEnumerable<Installation>> ReadAll(bool readOnly = true)
        {
            return await GetInstallations(readOnly: readOnly).ToListAsync();
        }

        private IQueryable<Installation> GetInstallations(bool readOnly = true)
        {
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var query = context.Installations.Where(
                (i) => accessibleInstallationCodes.Result.Contains(i.InstallationCode)
            );
            return readOnly ? query.AsNoTracking() : query.AsTracking();
        }

        private async Task ApplyUnprotectedDatabaseUpdate()
        {
            await context.SaveChangesAsync();
        }

        private async Task ApplyDatabaseUpdate(Installation? installation)
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes();
            if (
                installation == null
                || accessibleInstallationCodes.Contains(installation.InstallationCode)
            )
                await context.SaveChangesAsync();
            else
                throw new UnauthorizedAccessException(
                    $"User does not have permission to update installation in installation {installation.Name}"
                );
        }

        public async Task<Installation?> ReadById(string id, bool readOnly = true)
        {
            return await GetInstallations(readOnly: readOnly)
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<Installation?> ReadByInstallationCode(
            string installationCode,
            bool readOnly = true
        )
        {
            if (installationCode == null)
                return null;

            var installations = await GetInstallations(readOnly: readOnly).ToListAsync();

            return installations
                .Where(a =>
                    a.InstallationCode.Equals(
                        installationCode,
                        StringComparison.InvariantCultureIgnoreCase
                    )
                )
                .FirstOrDefault();
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

        public async Task<Installation> Update(Installation installation)
        {
            var entry = context.Update(installation);
            await ApplyDatabaseUpdate(installation);
            DetachTracking(context, installation);
            return entry.Entity;
        }

        public async Task<Installation?> Delete(string id)
        {
            var installation = await GetInstallations().FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (installation is null)
            {
                return null;
            }

            context.Installations.Remove(installation);
            await ApplyDatabaseUpdate(installation);

            return installation;
        }

        public void DetachTracking(FlotillaDbContext context, Installation installation)
        {
            context.Entry(installation).State = EntityState.Detached;
        }

        public void RobotIsOnSameInstallationAsMission(
            Robot robot,
            MissionDefinition missionDefinition
        )
        {
            // TODO: MissionDefinition.Installation is required, this if should not be needed
            // var missionInstallation = await ReadByInstallationCode(
            //     missionDefinition.InstallationCode,
            //     readOnly: true
            // );

            // if (missionInstallation is null)
            // {
            //     string errorMessage =
            //         $"Could not find installation for installation code {missionDefinition.InstallationCode}";
            //     logger.LogError("{Message}", errorMessage);
            //     throw new InstallationNotFoundException(errorMessage);
            // }

            if (robot.CurrentInstallation.Id != missionDefinition.Installation.Id)
            {
                string errorMessage =
                    $"The robot {robot.Name} is on installation {robot.CurrentInstallation.Name} which is not the same as the mission installation {missionDefinition.Installation.Name}";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotInSameInstallationAsMissionException(errorMessage);
            }
        }
    }
}
