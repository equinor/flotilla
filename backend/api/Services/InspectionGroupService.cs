using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IInspectionGroupService
    {
        public Task<IEnumerable<InspectionGroup>> ReadAll(bool readOnly = true);

        public Task<InspectionGroup?> ReadById(string id, bool readOnly = true);

        public Task<IEnumerable<InspectionGroup>> ReadByInstallation(
            string installationCode,
            bool readOnly = true
        );

        public Task<InspectionGroup?> ReadByInstallationAndName(
            string installationCode,
            string inspectionGroupName,
            bool readOnly = true
        );

        public Task<InspectionGroup> Create(CreateInspectionGroupQuery newInspectionGroup);

        public Task<InspectionGroup> Update(InspectionGroup inspectionGroup);

        public Task<InspectionGroup?> Delete(string id);

        public void DetachTracking(FlotillaDbContext context, InspectionGroup inspectionGroup);
    }

    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class InspectionGroupService(
        FlotillaDbContext context,
        IDefaultLocalizationPoseService defaultLocalizationPoseService,
        IInstallationService installationService,
        IAccessRoleService accessRoleService,
        ISignalRService signalRService
    ) : IInspectionGroupService
    {
        public async Task<IEnumerable<InspectionGroup>> ReadAll(bool readOnly = true)
        {
            return await GetInspectionGroups(readOnly: readOnly).ToListAsync();
        }

        public async Task<InspectionGroup?> ReadById(string id, bool readOnly = true)
        {
            return await GetInspectionGroups(readOnly: readOnly)
                .FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<IEnumerable<InspectionGroup>> ReadByInstallation(
            string installationCode,
            bool readOnly = true
        )
        {
            var installation = await installationService.ReadByInstallationCode(
                installationCode,
                readOnly: true
            );
            if (installation == null)
            {
                return [];
            }
            return await GetInspectionGroups(readOnly: readOnly)
                .Where(a => a.Installation != null && a.Installation.Id.Equals(installation.Id))
                .ToListAsync();
        }

        public async Task<InspectionGroup?> ReadByInstallationAndName(
            string installationCode,
            string inspectionGroupName,
            bool readOnly = true
        )
        {
            if (string.IsNullOrEmpty(inspectionGroupName))
            {
                return null;
            }

            var inspectionGroups = await GetAccessibleInspectionGroups(readOnly: readOnly)
                .ToListAsync();

            return inspectionGroups
                .Where(a =>
                    a.Installation != null
                    && a.Installation.InstallationCode.Equals(
                        installationCode,
                        StringComparison.InvariantCultureIgnoreCase
                    )
                    && a.Name.Equals(
                        inspectionGroupName,
                        StringComparison.InvariantCultureIgnoreCase
                    )
                )
                .FirstOrDefault();
        }

        // public async Task<InspectionGroup?> ReadByInstallationAndName(
        //     Installation installation,
        //     string name,
        //     bool readOnly = true
        // )
        // {
        //     var query = await GetAccessibleInspectionGroups(readOnly: readOnly)
        //         .Where(ig =>
        //             ig.Installation != null
        //             && ig.Installation.Id.Equals(installation.Id)
        //             && ig.Name.ToLower().Equals(name.ToLower())
        //         )
        //         .FirstOrDefaultAsync();
        //     return query;
        // }

        public async Task<InspectionGroup> Create(
            CreateInspectionGroupQuery newInspectionGroupQuery
        )
        {
            var installation =
                await installationService.ReadByInstallationCode(
                    newInspectionGroupQuery.InstallationCode,
                    readOnly: true
                )
                ?? throw new InstallationNotFoundException(
                    $"No installation with name {newInspectionGroupQuery.InstallationCode} could be found"
                );
            var existingInspectionGroup = await ReadByInstallationAndName(
                installation.InstallationCode,
                newInspectionGroupQuery.Name,
                readOnly: true
            );

            if (existingInspectionGroup != null)
            {
                throw new InspectionGroupExistsException(
                    $"Inspection are with name {newInspectionGroupQuery.Name} already exists"
                );
            }

            DefaultLocalizationPose? defaultLocalizationPose = null;
            // var defaultLocalizationEntityStateChange = EntityState.Modified;
            if (newInspectionGroupQuery.DefaultLocalizationPose != null)
            {
                defaultLocalizationPose = await defaultLocalizationPoseService.Create(
                    new DefaultLocalizationPose(
                        newInspectionGroupQuery.DefaultLocalizationPose.Value.Pose,
                        newInspectionGroupQuery.DefaultLocalizationPose.Value.IsDockingStation
                    )
                );
                // defaultLocalizationEntityStateChange = EntityState.Added;
            }

            var inspectionGroup = new InspectionGroup
            {
                Name = newInspectionGroupQuery.Name,
                Installation = installation,
                DefaultLocalizationPose = defaultLocalizationPose,
            };

            context.Entry(inspectionGroup.Installation).State = EntityState.Unchanged;
            if (inspectionGroup.DefaultLocalizationPose is not null)
            {
                context.Entry(inspectionGroup.DefaultLocalizationPose).State = EntityState.Modified;
                // context.Entry(inspectionGroup.DefaultLocalizationPose.Pose).State =
                //     defaultLocalizationEntityStateChange;
            }

            await context.InspectionGroups.AddAsync(inspectionGroup);
            await ApplyDatabaseUpdate(inspectionGroup.Installation);
            _ = signalRService.SendMessageAsync(
                "InspectionGroup created",
                inspectionGroup.Installation,
                new InspectionGroupResponse(inspectionGroup)
            );
            DetachTracking(context, inspectionGroup);
            return inspectionGroup!;
        }

        public async Task<InspectionGroup> Update(InspectionGroup inspectionGroup)
        {
            var entry = context.Update(inspectionGroup);
            await ApplyDatabaseUpdate(inspectionGroup.Installation);
            _ = signalRService.SendMessageAsync(
                "InspectionGroup updated",
                inspectionGroup.Installation,
                new InspectionGroupResponse(inspectionGroup)
            );
            DetachTracking(context, inspectionGroup);
            return entry.Entity;
        }

        public async Task<InspectionGroup?> Delete(string id)
        {
            var inspectionGroup = await GetInspectionGroups()
                .FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (inspectionGroup is null)
            {
                return null;
            }

            context.InspectionGroups.Remove(inspectionGroup);
            await ApplyDatabaseUpdate(inspectionGroup.Installation);
            _ = signalRService.SendMessageAsync(
                "InspectionGroup deleted",
                inspectionGroup.Installation,
                new InspectionGroupResponse(inspectionGroup)
            );

            return inspectionGroup;
        }

        private IQueryable<InspectionGroup> GetInspectionGroups(bool readOnly = true)
        {
            var query = context
                .InspectionGroups.Include(ig => ig.Installation)
                .Include(ig => ig.DefaultLocalizationPose);
            return readOnly ? query.AsNoTracking() : query.AsTracking();
        }

        public IQueryable<InspectionGroup> GetAccessibleInspectionGroups(bool readOnly = true)
        {
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var inspectionGroups = GetInspectionGroups(readOnly: readOnly);
            return inspectionGroups.Where(
                (ig) =>
                    accessibleInstallationCodes.Result.Contains(ig.Installation.InstallationCode)
            );
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
                    $"User does not have permission to update inspection Group in installation {installation.Name}"
                );
        }

        public void DetachTracking(FlotillaDbContext context, InspectionGroup inspectionGroup)
        {
            if (
                inspectionGroup.Installation != null
                && context.Entry(inspectionGroup.Installation).State != EntityState.Detached
            )
                installationService.DetachTracking(context, inspectionGroup.Installation);
            if (
                inspectionGroup.Installation != null
                && context.Entry(inspectionGroup.Installation).State != EntityState.Detached
            )
                installationService.DetachTracking(context, inspectionGroup.Installation);
            if (
                inspectionGroup.DefaultLocalizationPose != null
                && context.Entry(inspectionGroup.DefaultLocalizationPose).State
                    != EntityState.Detached
            )
                defaultLocalizationPoseService.DetachTracking(
                    context,
                    inspectionGroup.DefaultLocalizationPose
                );
            context.Entry(inspectionGroup).State = EntityState.Detached;
        }
    }
}
