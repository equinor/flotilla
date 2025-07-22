using Api.Controllers.Models;
using Api.Database.Models;
using Api.Mqtt;
using Api.Mqtt.Events;
using Api.Mqtt.MessageModels;
using Api.Services;
using Api.Services.ActionServices;
using Api.Services.Events;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.EventHandlers
{
    /// <summary>
    ///     A background service which listens to events and performs callback functions.
    /// </summary>
    public class MqttEventHandler : EventHandlerBase
    {
        private readonly ILogger<MqttEventHandler> _logger;

        private readonly IServiceScopeFactory _scopeFactory;

        private readonly Semaphore _updateRobotSemaphore = new(1, 1);

        public MqttEventHandler(ILogger<MqttEventHandler> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            // Reason for using factory: https://www.thecodebuzz.com/using-dbcontext-instance-in-ihostedservice/
            _scopeFactory = scopeFactory;

            Subscribe();
        }

        private IBatteryLevelService BatteryLevelService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IBatteryLevelService>();
        private IInspectionService InspectionService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IInspectionService>();
        private IInstallationService InstallationService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IInstallationService>();
        private ILastMissionRunService LastMissionRunService =>
            _scopeFactory
                .CreateScope()
                .ServiceProvider.GetRequiredService<ILastMissionRunService>();
        private IMissionRunService MissionRunService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionRunService>();
        private IMissionSchedulingService MissionScheduling =>
            _scopeFactory
                .CreateScope()
                .ServiceProvider.GetRequiredService<IMissionSchedulingService>();
        private IMissionTaskService MissionTaskService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionTaskService>();
        private IPressureLevelService PressureLevelService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IPressureLevelService>();
        private IRobotService RobotService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IRobotService>();
        private IRobotPoseService RobotPoseService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IRobotPoseService>();
        private ISignalRService SignalRService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ISignalRService>();
        private ITaskDurationService TaskDurationService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ITaskDurationService>();
        private ITeamsMessageService TeamsMessageService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ITeamsMessageService>();
        private IEmergencyActionService EmergencyActionService =>
            _scopeFactory
                .CreateScope()
                .ServiceProvider.GetRequiredService<IEmergencyActionService>();

        public override void Subscribe()
        {
            MqttService.MqttIsarStatusReceived += OnIsarStatus;
            MqttService.MqttIsarRobotInfoReceived += OnIsarRobotInfo;
            MqttService.MqttIsarMissionReceived += OnIsarMissionUpdate;
            MqttService.MqttIsarTaskReceived += OnIsarTaskUpdate;
            MqttService.MqttIsarBatteryReceived += OnIsarBatteryUpdate;
            MqttService.MqttIsarPressureReceived += OnIsarPressureUpdate;
            MqttService.MqttIsarPoseReceived += OnIsarPoseUpdate;
            MqttService.MqttIsarCloudHealthReceived += OnIsarCloudHealthUpdate;
            MqttService.MqttIsarStartupReceived += OnIsarStartup;
            MqttService.MqttSaraInspectionResultReceived += OnSaraInspectionResultUpdate;
        }

        public override void Unsubscribe()
        {
            MqttService.MqttIsarStatusReceived -= OnIsarStatus;
            MqttService.MqttIsarRobotInfoReceived -= OnIsarRobotInfo;
            MqttService.MqttIsarMissionReceived -= OnIsarMissionUpdate;
            MqttService.MqttIsarTaskReceived -= OnIsarTaskUpdate;
            MqttService.MqttIsarBatteryReceived -= OnIsarBatteryUpdate;
            MqttService.MqttIsarPressureReceived -= OnIsarPressureUpdate;
            MqttService.MqttIsarPoseReceived -= OnIsarPoseUpdate;
            MqttService.MqttIsarCloudHealthReceived -= OnIsarCloudHealthUpdate;
            MqttService.MqttIsarStartupReceived -= OnIsarStartup;
            MqttService.MqttSaraInspectionResultReceived -= OnSaraInspectionResultUpdate;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await stoppingToken;
        }

        private async void OnIsarStatus(object? sender, MqttReceivedArgs mqttArgs)
        {
            var isarStatus = (IsarStatusMessage)mqttArgs.Message;

            var robot = await RobotService.ReadByIsarId(isarStatus.IsarId, readOnly: true);

            if (robot == null)
            {
                _logger.LogInformation(
                    "Received message from unknown ISAR instance {Id} with robot name {Name}",
                    isarStatus.IsarId,
                    isarStatus.RobotName
                );
                return;
            }

            if (robot.Status == isarStatus.Status)
            {
                return;
            }

            _logger.LogInformation(
                "OnIsarStatus: Robot {robotName} has status {robotStatus} and current inspection area id {areaId}",
                robot.Name,
                robot.Status,
                robot.CurrentInspectionAreaId
            );

            _updateRobotSemaphore.WaitOne();
            _logger.LogDebug("Semaphore acquired for updating robot status");

            await RobotService.UpdateRobotStatus(robot.Id, isarStatus.Status);
            robot.Status = isarStatus.Status;

            _updateRobotSemaphore.Release();
            _logger.LogDebug("Semaphore released after updating robot status");

            _logger.LogInformation(
                "Updated status for robot {Name} to {Status}",
                robot.Name,
                robot.Status
            );

            _logger.LogInformation(
                "OnIsarStatus: Robot {robotName} has status {robotStatus} and current inspection area id {areaId}",
                robot.Name,
                robot.Status,
                robot.CurrentInspectionAreaId
            );

            if (robot.IsRobotReadyToStartMissions())
            {
                MissionScheduling.TriggerRobotReadyForMissions(
                    new RobotReadyForMissionsEventArgs(robot)
                );
            }
        }

        private async void CreateRobot(
            IsarRobotInfoMessage isarRobotInfo,
            Installation installation
        )
        {
            _logger.LogInformation(
                "Received message from new ISAR instance '{Id}' with robot name '{Name}'. Adding new robot to database",
                isarRobotInfo.IsarId,
                isarRobotInfo.RobotName
            );

            var robotQuery = new CreateRobotQuery
            {
                IsarId = isarRobotInfo.IsarId,
                Name = isarRobotInfo.RobotName,
                RobotType = isarRobotInfo.RobotType,
                SerialNumber = isarRobotInfo.SerialNumber,
                CurrentInstallationCode = installation.InstallationCode,
                Documentation = isarRobotInfo.DocumentationQueries,
                Host = isarRobotInfo.Host,
                Port = isarRobotInfo.Port,
                RobotCapabilities = isarRobotInfo.Capabilities,
                Status = RobotStatus.Available,
            };

            try
            {
                var newRobot = await RobotService.CreateFromQuery(robotQuery);
                _logger.LogInformation(
                    "Added robot '{RobotName}' with ISAR id '{IsarId}' to database",
                    newRobot.Name,
                    newRobot.IsarId
                );
            }
            catch (DbUpdateException)
            {
                _logger.LogError(
                    "Failed to add robot {robotQueryName} with to the database",
                    robotQuery.Name
                );
            }
        }

        private async void OnIsarRobotInfo(object? sender, MqttReceivedArgs mqttArgs)
        {
            var isarRobotInfo = (IsarRobotInfoMessage)mqttArgs.Message;

            var installation = await InstallationService.ReadByInstallationCode(
                isarRobotInfo.CurrentInstallation,
                readOnly: true
            );

            if (installation is null)
            {
                _logger.LogError(
                    new InstallationNotFoundException(
                        $"No installation with code {isarRobotInfo.CurrentInstallation} found"
                    ),
                    "Could not create new robot due to missing installation"
                );
                return;
            }

            try
            {
                var robot = await RobotService.ReadByIsarId(isarRobotInfo.IsarId, readOnly: true);

                if (robot == null)
                {
                    CreateRobot(isarRobotInfo, installation);
                    return;
                }

                try
                {
                    _updateRobotSemaphore.WaitOne();
                    _logger.LogDebug("Semaphore acquired for updating robot");

                    List<string> updatedFields = [];

                    if (isarRobotInfo.Host is not null)
                        UpdateHostIfChanged(isarRobotInfo.Host, ref robot, ref updatedFields);

                    UpdatePortIfChanged(isarRobotInfo.Port, ref robot, ref updatedFields);

                    if (isarRobotInfo.CurrentInstallation is not null)
                        UpdateCurrentInstallationIfChanged(
                            installation,
                            ref robot,
                            ref updatedFields
                        );
                    if (isarRobotInfo.Capabilities is not null)
                        UpdateRobotCapabilitiesIfChanged(
                            isarRobotInfo.Capabilities,
                            ref robot,
                            ref updatedFields
                        );
                    if (updatedFields.Count < 1)
                        return;

                    await RobotService.Update(robot);
                    _logger.LogInformation(
                        "Updated robot '{Id}' ('{RobotName}') in database: {Updates}",
                        robot.Id,
                        robot.Name,
                        updatedFields
                    );
                }
                finally
                {
                    _updateRobotSemaphore.Release();
                    _logger.LogDebug("Semaphore released after updating robot");
                }
            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e, "Could not add robot to db");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not update robot in db");
            }
        }

        private static void UpdateHostIfChanged(
            string host,
            ref Robot robot,
            ref List<string> updatedFields
        )
        {
            if (host.Equals(robot.Host, StringComparison.Ordinal))
                return;

            updatedFields.Add($"\nHost ({robot.Host} -> {host})\n");
            robot.Host = host;
        }

        private static void UpdatePortIfChanged(
            int port,
            ref Robot robot,
            ref List<string> updatedFields
        )
        {
            if (port.Equals(robot.Port))
                return;

            updatedFields.Add($"\nPort ({robot.Port} -> {port})\n");
            robot.Port = port;
        }

        private static void UpdateCurrentInstallationIfChanged(
            Installation newCurrentInstallation,
            ref Robot robot,
            ref List<string> updatedFields
        )
        {
            if (
                newCurrentInstallation.InstallationCode.Equals(
                    robot.CurrentInstallation?.InstallationCode,
                    StringComparison.Ordinal
                )
            )
                return;

            updatedFields.Add(
                $"\nCurrentInstallation ({robot.CurrentInstallation} -> {newCurrentInstallation})\n"
            );
            robot.CurrentInstallation = newCurrentInstallation;
        }

        public static void UpdateRobotCapabilitiesIfChanged(
            IList<RobotCapabilitiesEnum> newRobotCapabilities,
            ref Robot robot,
            ref List<string> updatedFields
        )
        {
            if (
                robot.RobotCapabilities != null
                && Enumerable.SequenceEqual(newRobotCapabilities, robot.RobotCapabilities)
            )
                return;

            updatedFields.Add(
                $"\nRobotCapabilities ({robot.RobotCapabilities} -> {newRobotCapabilities})\n"
            );
            robot.RobotCapabilities = newRobotCapabilities;
        }

        private async void OnIsarMissionUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var isarMission = (IsarMissionMessage)mqttArgs.Message;

            MissionStatus status;
            try
            {
                status = MissionRun.GetMissionStatusFromString(isarMission.Status);
            }
            catch (ArgumentException e)
            {
                _logger.LogError(
                    e,
                    "Failed to parse mission status from MQTT message. Mission with ISARMissionId '{IsarMissionId}' was not updated",
                    isarMission.MissionId
                );
                return;
            }

            var flotillaMissionRun = await MissionRunService.ReadByIsarMissionId(
                isarMission.MissionId,
                readOnly: true
            );
            if (flotillaMissionRun is null)
            {
                string errorMessage =
                    $"Mission with isar mission Id {isarMission.MissionId} was not found";
                _logger.LogError("{Message}", errorMessage);

                var isarRobot = await RobotService.ReadByIsarId(isarMission.IsarId, readOnly: true);

                // Check if return home mission fails
                if (status == MissionStatus.Failed && isarRobot != null)
                {
                    string errorDescription =
                        isarMission.ErrorDescription ?? "The initiated mission failed";
                    string reportMessage = $"Failed mission for robot {isarRobot.Name}";

                    SignalRService.ReportGeneralFailToSignalR(
                        isarRobot,
                        reportMessage,
                        errorDescription
                    );
                }

                return;
            }

            if (flotillaMissionRun.Status == status)
            {
                return;
            }
            if (
                flotillaMissionRun.Status == MissionStatus.Aborted
                && status == MissionStatus.Cancelled
            )
            {
                status = MissionStatus.Aborted;
            }

            MissionRun updatedFlotillaMissionRun;
            try
            {
                updatedFlotillaMissionRun =
                    await MissionRunService.UpdateMissionRunStatusByIsarMissionId(
                        isarMission.MissionId,
                        status,
                        isarMission.ErrorDescription
                    );
            }
            catch (MissionRunNotFoundException)
            {
                return;
            }

            _logger.LogInformation(
                "Mission '{Id}' (ISARMissionID='{IsarMissionId}') status updated to '{Status}' for robot '{RobotName}' with ISAR id '{IsarId}'",
                updatedFlotillaMissionRun.Id,
                isarMission.MissionId,
                isarMission.Status,
                isarMission.RobotName,
                isarMission.IsarId
            );

            if (!updatedFlotillaMissionRun.IsCompleted)
                return;

            var robot = await RobotService.ReadByIsarId(isarMission.IsarId, readOnly: true);
            if (robot is null)
            {
                _logger.LogError(
                    "Could not find robot '{RobotName}' with ISAR id '{IsarId}'",
                    isarMission.RobotName,
                    isarMission.IsarId
                );
                return;
            }

            _logger.LogInformation(
                "Robot '{Id}' ('{Name}') - completed mission run {MissionRunId}",
                robot.IsarId,
                robot.Name,
                updatedFlotillaMissionRun.Id
            );
            if (robot.CurrentMissionId == flotillaMissionRun.Id)
            {
                await RobotService.UpdateCurrentMissionId(robot.Id, null);
            }

            try
            {
                await LastMissionRunService.SetLastMissionRun(
                    updatedFlotillaMissionRun.Id,
                    updatedFlotillaMissionRun.MissionId
                );
            }
            catch (MissionNotFoundException)
            {
                _logger.LogError(
                    "Mission not found when setting last mission run for mission definition {missionId}",
                    updatedFlotillaMissionRun.MissionId
                );
                return;
            }

            await TaskDurationService.UpdateAverageDurationPerTask(robot.Model.Type);
        }

        private async void OnIsarTaskUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var task = (IsarTaskMessage)mqttArgs.Message;

            IsarTaskStatus status;
            try
            {
                status = IsarTask.StatusFromString(task.Status);
            }
            catch (ArgumentException e)
            {
                _logger.LogError(
                    e,
                    "Failed to parse mission status from MQTT message. Mission '{Id}' was not updated",
                    task.MissionId
                );
                return;
            }

            if (!task.IsInspectionTask(task.TaskType))
            {
                _logger.LogInformation(
                    "Received status update to status {status} for task of type {taskType}. As this is not an inspection task, the task update will be disregarded.",
                    status,
                    task.TaskType
                );
                return;
            }

            MissionTask missionTask;
            try
            {
                missionTask = await MissionTaskService.UpdateMissionTaskStatus(
                    task.TaskId,
                    status,
                    task.ErrorDescription
                );
            }
            catch (MissionTaskNotFoundException)
            {
                return;
            }

            var missionRun = await MissionRunService.ReadByIsarMissionId(
                task.MissionId,
                readOnly: true
            );
            if (missionRun is null)
            {
                _logger.LogWarning("Mission run with ID {Id} was not found", task.MissionId);
            }

            _ = SignalRService.SendMessageAsync(
                "Mission run updated",
                missionRun?.InspectionArea.Installation,
                missionRun != null ? new MissionRunResponse(missionRun) : null
            );

            _logger.LogInformation(
                "Task '{Id}' updated to '{Status}' for robot '{RobotName}' with ISAR id '{IsarId}'",
                task.TaskId,
                task.Status,
                task.RobotName,
                task.IsarId
            );
        }

        private async void OnIsarBatteryUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var batteryStatus = (IsarBatteryMessage)mqttArgs.Message;

            _updateRobotSemaphore.WaitOne();
            _logger.LogDebug("Semaphore acquired for updating battery");

            var robot = await BatteryLevelService.UpdateBatteryLevel(
                batteryStatus.BatteryLevel,
                batteryStatus.IsarId
            );
            if (robot != null && robot.BatteryState != batteryStatus.BatteryState)
            {
                await RobotService.UpdateRobotBatteryState(robot.Id, batteryStatus.BatteryState);
            }

            _updateRobotSemaphore.Release();
            _logger.LogDebug("Semaphore released after updating battery");

            if (robot == null)
                return;
            robot.BatteryLevel = batteryStatus.BatteryLevel;

            if (robot.FlotillaStatus == RobotFlotillaStatus.Normal && robot.IsRobotBatteryTooLow())
            {
                _logger.LogInformation(
                    "Sending robot '{RobotName}' to its dock as its battery level is too low.",
                    robot.Name
                );
                EmergencyActionService.SendRobotToDock(
                    new RobotEmergencyEventArgs(
                        robot,
                        RobotFlotillaStatus.Recharging,
                        "Robot battery level too low to complete mission"
                    )
                );
            }
            else if (
                robot.FlotillaStatus == RobotFlotillaStatus.Recharging
                && robot.IsRobotReadyToStartMissions()
            )
            {
                _logger.LogInformation(
                    "Releasing robot '{RobotName}' from its dock as its battery and pressure levels are good enough to run missions.",
                    robot.Name
                );
                EmergencyActionService.ReleaseRobotFromDock(
                    new RobotEmergencyEventArgs(robot, RobotFlotillaStatus.Normal)
                );
            }
        }

        private async void OnIsarPressureUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var pressureStatus = (IsarPressureMessage)mqttArgs.Message;

            _updateRobotSemaphore.WaitOne();
            _logger.LogDebug("Semaphore acquired for updating pressure");

            var robot = await PressureLevelService.UpdatePressureLevel(
                pressureStatus.PressureLevel,
                pressureStatus.IsarId
            );

            _updateRobotSemaphore.Release();
            _logger.LogDebug("Semaphore released after updating pressure");

            if (robot == null)
                return;
            robot.PressureLevel = pressureStatus.PressureLevel;

            if (
                robot.FlotillaStatus == RobotFlotillaStatus.Normal
                && (robot.IsRobotPressureTooLow() || robot.IsRobotPressureTooHigh())
            )
            {
                _logger.LogInformation(
                    "Sending robot '{RobotName}' to its dock as its pressure is too low or high.",
                    robot.Name
                );
                EmergencyActionService.SendRobotToDock(
                    new RobotEmergencyEventArgs(
                        robot,
                        RobotFlotillaStatus.Recharging,
                        "Robot couldn't complete mission as pressure level were outside acceptable range"
                    )
                );
            }
            else if (
                robot.FlotillaStatus == RobotFlotillaStatus.Recharging
                && robot.IsRobotReadyToStartMissions()
            )
            {
                _logger.LogInformation(
                    "Releasing robot '{RobotName}' from its dock as its battery and pressure levels are good enough to run missions.",
                    robot.Name
                );
                EmergencyActionService.ReleaseRobotFromDock(
                    new RobotEmergencyEventArgs(robot, RobotFlotillaStatus.Normal)
                );
            }
        }

        private async void OnIsarPoseUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var poseStatus = (IsarPoseMessage)mqttArgs.Message;
            var pose = new Pose(poseStatus.Pose);

            _updateRobotSemaphore.WaitOne();
            _logger.LogDebug("Semaphore acquired for updating pose");

            await RobotPoseService.UpdateRobotPose(pose, poseStatus.IsarId);

            _updateRobotSemaphore.Release();
            _logger.LogDebug("Semaphore released after updating pose");
        }

        private async void OnIsarCloudHealthUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var cloudHealthStatus = (IsarCloudHealthMessage)mqttArgs.Message;

            var robot = await RobotService.ReadByIsarId(cloudHealthStatus.IsarId, readOnly: true);
            if (robot == null)
            {
                _logger.LogInformation(
                    "Received message from unknown ISAR instance {Id} with robot name {Name}",
                    cloudHealthStatus.IsarId,
                    cloudHealthStatus.RobotName
                );
                return;
            }

            string message = $"Failed telemetry request for robot {cloudHealthStatus.RobotName}.";

            TeamsMessageService.TriggerTeamsMessageReceived(new TeamsMessageEventArgs(message));
        }

        private async void OnIsarStartup(object? sender, MqttReceivedArgs mqttArgs)
        {
            var startupMessage = (IsarStartupMessage)mqttArgs.Message;

            var robot = await RobotService.ReadByIsarId(startupMessage.IsarId, readOnly: true);
            if (robot == null)
            {
                _logger.LogInformation(
                    "Received message from unknown ISAR instance {Id}",
                    startupMessage.IsarId
                );
                return;
            }

            _logger.LogInformation(
                "Received ISAR restart event for robot {robotName} with ISAR id {isarId}. Will restart ongoing missionRun, if any, by moving it back to the queue.",
                robot.Name,
                robot.IsarId
            );
            try
            {
                await MissionScheduling.MoveCurrentMissionRunBackToQueue(robot.Id);
            }
            catch (MissionRunNotFoundException)
            {
                _logger.LogInformation(
                    "Tried to restart mission run on isar restarte event. No ongoing mission found."
                );
            }
            catch (MissionException)
            {
                _logger.LogError(
                    "Tried to restart mission run on isar restarte event. Failed to move current mission run back to queue"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Tried to restart mission run on isar restarte event. Failed to move current mission run back to queue for robot {robotName} with ISAR id {isarId} with unexpected exception",
                    robot.Name,
                    robot.IsarId
                );
            }
        }

        private async void OnSaraInspectionResultUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var inspectionResult = (SaraInspectionResultMessage)mqttArgs.Message;

            var inspectionResultMessage = new InspectionResultMessage
            {
                InspectionId = inspectionResult.InspectionId,
                StorageAccount = inspectionResult.StorageAccount,
                BlobContainer = inspectionResult.BlobContainer,
                BlobName = inspectionResult.BlobName,
            };

            var installation = await InstallationService.ReadByInstallationCode(
                inspectionResult.BlobContainer,
                readOnly: true
            );

            if (installation == null)
            {
                _logger.LogError(
                    "Installation with code {Code} not found when processing SARA inspection result update",
                    inspectionResult.BlobContainer
                );
                return;
            }

            _ = SignalRService.SendMessageAsync(
                "Inspection Visulization Ready",
                installation,
                inspectionResultMessage
            );
        }
    }
}
