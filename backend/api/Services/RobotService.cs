using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public interface IRobotService
    {
        public Task<Robot> Create(Robot newRobot);
        public Task<Robot> CreateFromQuery(CreateRobotQuery robotQuery);
        public Task<Robot> GetRobotWithSchedulingPreCheck(string robotId, bool readOnly = true);
        public Task<IEnumerable<Robot>> ReadAll(bool readOnly = true);
        public Task<IEnumerable<string>> ReadAllActivePlants(bool readOnly = true);
        public Task<Robot?> ReadById(string id, bool readOnly = true);
        public Task<Robot?> ReadByIsarId(string isarId, bool readOnly = true);
        public Task<IList<Robot>> ReadRobotsForInstallation(
            string installationCode,
            bool readOnly = true
        );
        public Task Update(Robot robot);
        public Task UpdateRobotStatus(string robotId, RobotStatus status);
        public Task UpdateRobotIsarConnected(string robotId, bool isarConnected);
        public Task UpdateCurrentMissionId(string robotId, string? missionId);
        public Task UpdateCurrentInspectionAreaId(string robotId, string? inspectionAreaId);
        public Task UpdateDeprecated(string robotId, bool deprecated);

        public Task SendToSignalROnPropertyUpdate(
            string robotId,
            string propertyName,
            object? propertyValue
        );
        public Task<Robot?> Delete(string id);
        public void DetachTracking(FlotillaDbContext context, Robot robot);
    }

    [SuppressMessage(
        "Globalization",
        "CA1309:Use ordinal StringComparison",
        Justification = "EF Core refrains from translating string comparison overloads to SQL"
    )]
    public class RobotService(
        FlotillaDbContext context,
        ILogger<RobotService> logger,
        IRobotModelService robotModelService,
        ISignalRService signalRService,
        IAccessRoleService accessRoleService,
        IInstallationService installationService,
        IInspectionAreaService inspectionAreaService
    ) : IRobotService
    {
        public async Task<Robot> Create(Robot newRobot)
        {
            if (newRobot.CurrentInstallation != null)
                context.Entry(newRobot.CurrentInstallation).State = EntityState.Unchanged;
            if (newRobot.Model != null)
                context.Entry(newRobot.Model).State = EntityState.Unchanged;

            await context.Robots.AddAsync(newRobot);
            await ApplyDatabaseUpdate(newRobot.CurrentInstallation);
            DetachTracking(context, newRobot);
            return newRobot;
        }

        public async Task<Robot> CreateFromQuery(CreateRobotQuery robotQuery)
        {
            var robotModel = await robotModelService.ReadByRobotType(
                robotQuery.RobotType,
                readOnly: true
            );
            if (robotModel != null)
            {
                var installation = await installationService.ReadByInstallationCode(
                    robotQuery.CurrentInstallationCode,
                    readOnly: true
                );
                if (installation is null)
                {
                    logger.LogError(
                        "Installation {CurrentInstallation} does not exist",
                        robotQuery.CurrentInstallationCode
                    );
                    throw new DbUpdateException(
                        $"Could not create new robot in database as installation {robotQuery.CurrentInstallationCode} doesn't exist"
                    );
                }

                var newRobot = new Robot(robotQuery, installation, robotModel);

                if (newRobot.CurrentInstallation != null)
                    context.Entry(newRobot.CurrentInstallation).State = EntityState.Unchanged;
                if (newRobot.Model != null)
                    context.Entry(newRobot.Model).State = EntityState.Unchanged;

                await context.Robots.AddAsync(newRobot);
                await ApplyDatabaseUpdate(newRobot.CurrentInstallation);
                _ = signalRService.SendMessageAsync(
                    "Robot added",
                    newRobot!.CurrentInstallation,
                    new RobotResponse(newRobot!)
                );
                DetachTracking(context, newRobot);
                return newRobot!;
            }
            throw new DbUpdateException(
                "Could not create new robot in database as robot model does not exist"
            );
        }

        public async Task<Robot> GetRobotWithSchedulingPreCheck(
            string robotId,
            bool readOnly = true
        )
        {
            var robot = await ReadById(robotId, readOnly: readOnly);

            if (robot is null)
            {
                string errorMessage = $"The robot with ID {robotId} could not be found";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            if (robot.Status == RobotStatus.Offline)
            {
                string errorMessage = $"The robot with ID {robotId} is Offline";
                logger.LogError("{Message}", errorMessage);
                throw new RobotPreCheckFailedException(errorMessage);
            }

            if (robot.IsarConnected == false)
            {
                string errorMessage =
                    $"The robot with ID {robotId} has connection issues. Isar not connected.";
                logger.LogError("{Message}", errorMessage);
                throw new RobotPreCheckFailedException(errorMessage);
            }

            return robot;
        }

        public async Task SendToSignalROnPropertyUpdate(
            string robotId,
            string propertyName,
            object? propertyValue
        )
        {
            var robot = await ReadById(robotId);
            if (robot == null || robot.CurrentInstallation == null)
                return;
            await signalRService.SendMessageAsync(
                "Robot property updated",
                robot.CurrentInstallation,
                new UpdateRobotPropertyMessage
                {
                    RobotId = robotId,
                    PropertyName = propertyName,
                    PropertyValue = propertyValue,
                }
            );
        }

        public async Task UpdateRobotStatus(string robotId, RobotStatus status)
        {
            logger.LogInformation(
                "Setting status on robot with id {robotId} to {NewValue}",
                robotId,
                status
            );

            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes(
                AccessMode.Write
            );

            var robotQuery = context.Robots.Where(r =>
                r.Id == robotId
                && accessibleInstallationCodes.Contains(
                    r.CurrentInstallation.InstallationCode.ToUpper()
                )
            );
            await robotQuery.ExecuteUpdateAsync(setters =>
                setters.SetProperty(r => r.Status, status)
            );

            await SendToSignalROnPropertyUpdate(robotId, "status", status);
        }

        public async Task UpdateRobotIsarConnected(string robotId, bool isarConnected)
        {
            logger.LogInformation(
                "Setting isarConnected on robot with id {robotId} to {NewValue}",
                robotId,
                isarConnected
            );
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes(
                AccessMode.Write
            );
            var robotQuery = context.Robots.Where(r =>
                r.Id == robotId
                && accessibleInstallationCodes.Contains(
                    r.CurrentInstallation.InstallationCode.ToUpper()
                )
            );
            await robotQuery.ExecuteUpdateAsync(setters =>
                setters.SetProperty(r => r.IsarConnected, isarConnected)
            );

            await SendToSignalROnPropertyUpdate(robotId, "isarConnected", isarConnected);
        }

        public async Task UpdateCurrentMissionId(string robotId, string? currentMissionId)
        {
            logger.LogInformation(
                "Setting currentMissionId on robot with id {robotId} to {NewValue}",
                robotId,
                currentMissionId
            );

            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes(
                AccessMode.Write
            );
            var robotQuery = context.Robots.Where(r =>
                r.Id == robotId
                && accessibleInstallationCodes.Result.Contains(
                    r.CurrentInstallation.InstallationCode.ToUpper()
                )
            );
            await robotQuery.ExecuteUpdateAsync(setters =>
                setters.SetProperty(r => r.CurrentMissionId, currentMissionId)
            );

            await SendToSignalROnPropertyUpdate(robotId, "currentMissionId", currentMissionId);
        }

        public async Task UpdateCurrentInspectionAreaId(string robotId, string? inspectionAreaId)
        {
            logger.LogInformation(
                "Updating current inspection area for robot with Id {robotId} to inspection area with Id {areaId}",
                robotId,
                inspectionAreaId
            );

            if (inspectionAreaId is not null)
            {
                var inspectionArea = await inspectionAreaService.ReadById(
                    inspectionAreaId,
                    readOnly: true
                );
                if (inspectionArea is null)
                {
                    logger.LogError(
                        "Could not find inspection area with id '{InspectionAreaId}'. Setting inspection area to null for robot with id '{IsarId}'",
                        inspectionAreaId,
                        robotId
                    );
                    inspectionAreaId = null;
                }
            }

            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes(
                AccessMode.Write
            );
            var robotQuery = context.Robots.Where(r =>
                r.Id == robotId
                && accessibleInstallationCodes.Result.Contains(
                    r.CurrentInstallation.InstallationCode.ToUpper()
                )
            );
            await robotQuery.ExecuteUpdateAsync(setters =>
                setters.SetProperty(r => r.CurrentInspectionAreaId, inspectionAreaId)
            );

            await SendToSignalROnPropertyUpdate(
                robotId,
                "currentInspectionAreaId",
                inspectionAreaId
            );
        }

        public async Task UpdateDeprecated(string robotId, bool deprecated)
        {
            logger.LogInformation(
                "Setting deprecated on robot with id {robotId} to {NewValue}",
                robotId,
                deprecated
            );

            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes(
                AccessMode.Write
            );
            var robotQuery = context.Robots.Where(r =>
                r.Id == robotId
                && accessibleInstallationCodes.Result.Contains(
                    r.CurrentInstallation.InstallationCode.ToUpper()
                )
            );
            await robotQuery.ExecuteUpdateAsync(setters =>
                setters.SetProperty(r => r.Deprecated, deprecated)
            );

            await SendToSignalROnPropertyUpdate(robotId, "deprecated", deprecated);
        }

        public async Task<IEnumerable<Robot>> ReadAll(bool readOnly = true)
        {
            return await GetRobotsWithSubModels(readOnly: readOnly).ToListAsync();
        }

        public async Task<Robot?> ReadById(string id, bool readOnly = true)
        {
            return await GetRobotsWithSubModels(readOnly: readOnly)
                .FirstOrDefaultAsync(robot => robot.Id.Equals(id));
        }

        public async Task<Robot?> ReadByIsarId(string isarId, bool readOnly = true)
        {
            return await GetRobotsWithSubModels(readOnly: readOnly)
                .FirstOrDefaultAsync(robot => robot.IsarId.Equals(isarId));
        }

        public async Task<IEnumerable<string>> ReadAllActivePlants(bool readOnly = true)
        {
            return await GetRobotsWithSubModels(readOnly: readOnly)
                .Where(r => r.IsarConnected && r.CurrentInstallation != null)
                .Select(r => r.CurrentInstallation!.InstallationCode)
                .ToListAsync();
        }

        public async Task Update(Robot robot)
        {
            context.Entry(robot.Model).State = EntityState.Unchanged;

            context.Update(robot);
            await ApplyDatabaseUpdate(robot.CurrentInstallation);
            _ = signalRService.SendMessageAsync(
                "Robot updated",
                robot?.CurrentInstallation,
                robot != null ? new RobotResponse(robot) : null
            );
            DetachTracking(context, robot!);
        }

        public async Task<Robot?> Delete(string id)
        {
            var robot = await GetRobotsWithSubModels().FirstOrDefaultAsync(ev => ev.Id.Equals(id));
            if (robot is null)
                return null;

            context.Robots.Remove(robot);
            await ApplyDatabaseUpdate(robot.CurrentInstallation);
            _ = signalRService.SendMessageAsync(
                "Robot deleted",
                robot?.CurrentInstallation,
                robot != null ? new RobotResponse(robot) : null
            );
            return robot;
        }

        public async Task<IList<Robot>> ReadRobotsForInstallation(
            string installationCode,
            bool readOnly = true
        )
        {
            return await GetRobotsWithSubModels(readOnly: readOnly)
                .Where(robot =>
#pragma warning disable CA1304
                    robot.CurrentInstallation != null
                    && robot
                        .CurrentInstallation.InstallationCode.ToLower()
                        .Equals(installationCode.ToLower())
#pragma warning restore CA1304
                )
                .ToListAsync();
        }

        private IQueryable<Robot> GetRobotsWithSubModels(bool readOnly = true)
        {
            var accessibleInstallationCodes = accessRoleService
                .GetAllowedInstallationCodes(AccessMode.Read)
                .Result;

            var query = context
                .Robots.Include(r => r.Documentation)
                .Include(r => r.Model)
                .Include(r => r.CurrentInstallation)
#pragma warning disable CA1304
                .Where(r =>
                    !r.Deprecated
                    && (
                        r.CurrentInstallation == null
                        || r.CurrentInstallation.InstallationCode == null
                        || accessibleInstallationCodes.Contains(
                            r.CurrentInstallation.InstallationCode.ToUpper()
                        )
                    )
                );
#pragma warning restore CA1304
            return readOnly ? query.AsNoTracking() : query.AsTracking();
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
                    $"User does not have permission to update robot in installation {installation.Name}"
                );
        }

        public void DetachTracking(FlotillaDbContext context, Robot robot)
        {
            context.Entry(robot).State = EntityState.Detached;
            if (
                robot.CurrentInstallation != null
                && context.Entry(robot.CurrentInstallation).State != EntityState.Detached
            )
                installationService.DetachTracking(context, robot.CurrentInstallation);
            if (robot.Model != null && context.Entry(robot.Model).State != EntityState.Detached)
                robotModelService.DetachTracking(context, robot.Model);
        }
    }
}
