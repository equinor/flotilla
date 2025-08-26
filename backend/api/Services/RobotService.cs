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
        public Task UpdateRobotBatteryLevel(string robotId, float batteryLevel);
        public Task UpdateRobotBatteryState(string robotId, BatteryState? batteryState);
        public Task UpdateRobotPressureLevel(string robotId, float? pressureLevel);
        public Task UpdateRobotPose(string robotId, Pose pose);
        public Task UpdateRobotIsarConnected(string robotId, bool isarConnected);
        public Task UpdateCurrentMissionId(string robotId, string? missionId);
        public Task UpdateCurrentInspectionAreaId(string robotId, string? inspectionAreaId);
        public Task UpdateDeprecated(string robotId, bool deprecated);
        public Task UpdateMissionQueueFrozen(string robotId, bool missionQueueFrozen);
        public Task UpdateFlotillaStatus(string robotId, RobotFlotillaStatus status);
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

        private async Task SendToSingalROnPropertyUpdate(
            string robotId,
            string propertyName,
            object? propertyValue,
            Installation installation
        )
        {
            await signalRService.SendMessageAsync(
                "Robot property updated",
                installation,
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

            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var robotQuery = context.Robots.Where(r =>
                r.Id == robotId
                && accessibleInstallationCodes.Result.Contains(
                    r.CurrentInstallation.InstallationCode.ToUpper()
                )
            );
            await robotQuery.ExecuteUpdateAsync(setters =>
                setters.SetProperty(r => r.Status, status)
            );
            var robot = await robotQuery.FirstOrDefaultAsync();
            var robotInstallation = robot?.CurrentInstallation;
            if (robotInstallation != null)
                await SendToSingalROnPropertyUpdate(robotId, "status", status, robotInstallation);
        }

        public async Task UpdateRobotBatteryLevel(string robotId, float batteryLevel)
        {
            logger.LogDebug(
                "Setting batteryLevel on robot with id {robotId} to {NewValue}",
                robotId,
                batteryLevel
            );

            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var robotQuery = context.Robots.Where(r =>
                r.Id == robotId
                && accessibleInstallationCodes.Result.Contains(
                    r.CurrentInstallation.InstallationCode.ToUpper()
                )
            );
            await robotQuery.ExecuteUpdateAsync(setters =>
                setters.SetProperty(r => r.BatteryLevel, batteryLevel)
            );
            var robot = await robotQuery.FirstOrDefaultAsync();
            var robotInstallation = robot?.CurrentInstallation;
            if (robotInstallation != null)
                await SendToSingalROnPropertyUpdate(
                    robotId,
                    "batteryLevel",
                    batteryLevel,
                    robotInstallation
                );
        }

        public async Task UpdateRobotBatteryState(string robotId, BatteryState? batteryState)
        {
            logger.LogInformation(
                "Setting batteryState on robot with id {robotId} to {NewValue}",
                robotId,
                batteryState
            );

            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var robotQuery = context.Robots.Where(r =>
                r.Id == robotId
                && accessibleInstallationCodes.Result.Contains(
                    r.CurrentInstallation.InstallationCode.ToUpper()
                )
            );
            await robotQuery.ExecuteUpdateAsync(setters =>
                setters.SetProperty(r => r.BatteryState, batteryState)
            );
            var robot = await robotQuery.FirstOrDefaultAsync();
            var robotInstallation = robot?.CurrentInstallation;
            if (robotInstallation != null)
                await SendToSingalROnPropertyUpdate(
                    robotId,
                    "batteryState",
                    batteryState,
                    robotInstallation
                );
        }

        public async Task UpdateRobotPressureLevel(string robotId, float? pressureLevel)
        {
            logger.LogDebug(
                "Setting pressureLevel on robot with id {robotId} to {NewValue}",
                robotId,
                pressureLevel
            );

            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            await context
                .Robots.Where(r =>
                    r.Id == robotId
                    && accessibleInstallationCodes.Result.Contains(
                        r.CurrentInstallation.InstallationCode.ToUpper()
                    )
                )
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(r => r.PressureLevel, pressureLevel)
                );
        }

        private void ThrowIfRobotIsNull(Robot? robot, string robotId)
        {
            if (robot is not null)
                return;

            string errorMessage = $"Robot with ID {robotId} was not found in the database";
            logger.LogError("{Message}", errorMessage);
            throw new RobotNotFoundException(errorMessage);
        }

        public async Task UpdateRobotPose(string robotId, Pose pose)
        {
            var robotQuery = GetRobotsWithSubModels(readOnly: true)
                .Where(robot => robot.Id == robotId);
            var robot = await robotQuery.FirstOrDefaultAsync();
            ThrowIfRobotIsNull(robot, robotId);

            await VerifyThatUserIsAuthorizedToUpdateDataForInstallation(robot!.CurrentInstallation);

            await robotQuery
                .Select(r => r.Pose)
                .ExecuteUpdateAsync(poses =>
                    poses
                        .SetProperty(p => p.Orientation.X, pose.Orientation.X)
                        .SetProperty(p => p.Orientation.Y, pose.Orientation.Y)
                        .SetProperty(p => p.Orientation.Z, pose.Orientation.Z)
                        .SetProperty(p => p.Orientation.W, pose.Orientation.W)
                        .SetProperty(p => p.Position.X, pose.Position.X)
                        .SetProperty(p => p.Position.Y, pose.Position.Y)
                        .SetProperty(p => p.Position.Z, pose.Position.Z)
                );

            robot = await robotQuery.FirstOrDefaultAsync();
            ThrowIfRobotIsNull(robot, robotId);
            NotifySignalROfUpdatedRobot(robot!, robot!.CurrentInstallation!);
        }

        public async Task UpdateRobotIsarConnected(string robotId, bool isarConnected)
        {
            logger.LogInformation(
                "Setting isarConnected on robot with id {robotId} to {NewValue}",
                robotId,
                isarConnected
            );
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var robotQuery = context.Robots.Where(r =>
                r.Id == robotId
                && accessibleInstallationCodes.Result.Contains(
                    r.CurrentInstallation.InstallationCode.ToUpper()
                )
            );
            await robotQuery.ExecuteUpdateAsync(setters =>
                setters.SetProperty(r => r.IsarConnected, isarConnected)
            );

            var robot = await robotQuery.FirstOrDefaultAsync();
            var robotInstallation = robot?.CurrentInstallation;
            if (robotInstallation != null)
                await SendToSingalROnPropertyUpdate(
                    robotId,
                    "isarConnected",
                    isarConnected,
                    robotInstallation
                );
        }

        public async Task UpdateCurrentMissionId(string robotId, string? currentMissionId)
        {
            logger.LogInformation(
                "Setting currentMissionId on robot with id {robotId} to {NewValue}",
                robotId,
                currentMissionId
            );

            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var robotQuery = context.Robots.Where(r =>
                r.Id == robotId
                && accessibleInstallationCodes.Result.Contains(
                    r.CurrentInstallation.InstallationCode.ToUpper()
                )
            );
            await robotQuery.ExecuteUpdateAsync(setters =>
                setters.SetProperty(r => r.CurrentMissionId, currentMissionId)
            );

            var robot = await robotQuery.FirstOrDefaultAsync();
            var robotInstallation = robot?.CurrentInstallation;
            if (robotInstallation != null)
                await SendToSingalROnPropertyUpdate(
                    robotId,
                    "currentMissionId",
                    currentMissionId,
                    robotInstallation
                );
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

            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var robotQuery = context.Robots.Where(r =>
                r.Id == robotId
                && accessibleInstallationCodes.Result.Contains(
                    r.CurrentInstallation.InstallationCode.ToUpper()
                )
            );
            await robotQuery.ExecuteUpdateAsync(setters =>
                setters.SetProperty(r => r.CurrentInspectionAreaId, inspectionAreaId)
            );

            var robot = await robotQuery.FirstOrDefaultAsync();
            var robotInstallation = robot?.CurrentInstallation;
            if (robotInstallation != null)
                await SendToSingalROnPropertyUpdate(
                    robotId,
                    "currentInspectionAreaId",
                    inspectionAreaId,
                    robotInstallation
                );
        }

        public async Task UpdateDeprecated(string robotId, bool deprecated)
        {
            logger.LogInformation(
                "Setting deprecated on robot with id {robotId} to {NewValue}",
                robotId,
                deprecated
            );

            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var robotQuery = context.Robots.Where(r =>
                r.Id == robotId
                && accessibleInstallationCodes.Result.Contains(
                    r.CurrentInstallation.InstallationCode.ToUpper()
                )
            );
            await robotQuery.ExecuteUpdateAsync(setters =>
                setters.SetProperty(r => r.Deprecated, deprecated)
            );

            var robot = await robotQuery.FirstOrDefaultAsync();
            var robotInstallation = robot?.CurrentInstallation;
            if (robotInstallation != null)
                await SendToSingalROnPropertyUpdate(
                    robotId,
                    "deprecated",
                    deprecated,
                    robotInstallation
                );
        }

        public async Task UpdateMissionQueueFrozen(string robotId, bool missionQueueFrozen)
        {
            logger.LogInformation(
                "Setting missionQueueFrozen on robot with id {robotId} to {NewValue}",
                robotId,
                missionQueueFrozen
            );

            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var robotQuery = context.Robots.Where(r =>
                r.Id == robotId
                && accessibleInstallationCodes.Result.Contains(
                    r.CurrentInstallation.InstallationCode.ToUpper()
                )
            );
            await robotQuery.ExecuteUpdateAsync(setters =>
                setters.SetProperty(r => r.MissionQueueFrozen, missionQueueFrozen)
            );

            var robot = await robotQuery.FirstOrDefaultAsync();
            var robotInstallation = robot?.CurrentInstallation;
            if (robotInstallation != null)
                await SendToSingalROnPropertyUpdate(
                    robotId,
                    "missionQueueFrozen",
                    missionQueueFrozen,
                    robotInstallation
                );
        }

        public async Task UpdateFlotillaStatus(string robotId, RobotFlotillaStatus status)
        {
            logger.LogInformation(
                "Setting flotillaStatus on robot with id {robotId} to {NewValue}",
                robotId,
                status
            );

            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
            var robotQuery = context.Robots.Where(r =>
                r.Id == robotId
                && accessibleInstallationCodes.Result.Contains(
                    r.CurrentInstallation.InstallationCode.ToUpper()
                )
            );
            await robotQuery.ExecuteUpdateAsync(setters =>
                setters.SetProperty(r => r.FlotillaStatus, status)
            );

            var robot = await robotQuery.FirstOrDefaultAsync();
            var robotInstallation = robot?.CurrentInstallation;
            if (robotInstallation != null)
                await SendToSingalROnPropertyUpdate(
                    robotId,
                    "flotillaStatus",
                    status,
                    robotInstallation
                );
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
            var accessibleInstallationCodes = accessRoleService.GetAllowedInstallationCodes();
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
                        || accessibleInstallationCodes.Result.Contains(
                            r.CurrentInstallation.InstallationCode.ToUpper()
                        )
                    )
                );
#pragma warning restore CA1304
            return readOnly ? query.AsNoTracking() : query.AsTracking();
        }

        private async Task ApplyDatabaseUpdate(Installation? installation)
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes();
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

        private async Task VerifyThatUserIsAuthorizedToUpdateDataForInstallation(
            Installation? installation
        )
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes();
            if (
                installation == null
                || accessibleInstallationCodes.Contains(
                    installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)
                )
            )
                return;

            throw new UnauthorizedAccessException(
                $"User does not have permission to update robot in installation {installation.Name}"
            );
        }

        private void NotifySignalROfUpdatedRobot(Robot robot, Installation installation)
        {
            _ = signalRService.SendMessageAsync(
                "Robot updated",
                installation,
                robot != null ? new RobotResponse(robot) : null
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
