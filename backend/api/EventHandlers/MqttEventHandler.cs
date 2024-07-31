using System.Text.Json;
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

        public MqttEventHandler(ILogger<MqttEventHandler> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            // Reason for using factory: https://www.thecodebuzz.com/using-dbcontext-instance-in-ihostedservice/
            _scopeFactory = scopeFactory;

            Subscribe();
        }

        private IBatteryTimeseriesService BatteryTimeseriesService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IBatteryTimeseriesService>();
        private IInspectionService InspectionService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IInspectionService>();
        private IInstallationService InstallationService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IInstallationService>();
        private ILastMissionRunService LastMissionRunService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILastMissionRunService>();
        private IMissionRunService MissionRunService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionRunService>();
        private IMissionSchedulingService MissionScheduling => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionSchedulingService>();
        private IMissionTaskService MissionTaskService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionTaskService>();
        private IPressureTimeseriesService PressureTimeseriesService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IPressureTimeseriesService>();
        private IRobotService RobotService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IRobotService>();
        private IPoseTimeseriesService PoseTimeseriesService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IPoseTimeseriesService>();
        private ISignalRService SignalRService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ISignalRService>();
        private ITaskDurationService TaskDurationService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ITaskDurationService>();
        private ITeamsMessageService TeamsMessageService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ITeamsMessageService>();
        private IEmergencyActionService EmergencyActionService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IEmergencyActionService>();

        public override void Subscribe()
        {
            MqttService.MqttIsarStatusReceived += OnIsarStatus;
            MqttService.MqttIsarRobotInfoReceived += OnIsarRobotInfo;
            MqttService.MqttIsarMissionReceived += OnIsarMissionUpdate;
            MqttService.MqttIsarTaskReceived += OnIsarTaskUpdate;
            MqttService.MqttIsarStepReceived += OnIsarStepUpdate;
            MqttService.MqttIsarBatteryReceived += OnIsarBatteryUpdate;
            MqttService.MqttIsarPressureReceived += OnIsarPressureUpdate;
            MqttService.MqttIsarPoseReceived += OnIsarPoseUpdate;
            MqttService.MqttIsarCloudHealthReceived += OnIsarCloudHealthUpdate;
        }

        public override void Unsubscribe()
        {
            MqttService.MqttIsarStatusReceived -= OnIsarStatus;
            MqttService.MqttIsarRobotInfoReceived -= OnIsarRobotInfo;
            MqttService.MqttIsarMissionReceived -= OnIsarMissionUpdate;
            MqttService.MqttIsarTaskReceived -= OnIsarTaskUpdate;
            MqttService.MqttIsarStepReceived -= OnIsarStepUpdate;
            MqttService.MqttIsarBatteryReceived -= OnIsarBatteryUpdate;
            MqttService.MqttIsarPressureReceived -= OnIsarPressureUpdate;
            MqttService.MqttIsarPoseReceived -= OnIsarPoseUpdate;
            MqttService.MqttIsarCloudHealthReceived -= OnIsarCloudHealthUpdate;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) { await stoppingToken; }


        private async void OnIsarStatus(object? sender, MqttReceivedArgs mqttArgs)
        {
            var isarStatus = (IsarStatusMessage)mqttArgs.Message;

            var robot = await RobotService.ReadByIsarId(isarStatus.IsarId);

            if (robot == null)
            {
                _logger.LogInformation("Received message from unknown ISAR instance {Id} with robot name {Name}", isarStatus.IsarId, isarStatus.RobotName);
                return;
            }

            if (robot.Status == isarStatus.Status) { return; }

            if (await MissionRunService.OngoingLocalizationMissionRunExists(robot.Id)) Thread.Sleep(5000); // Give localization mission update time to complete

            var preUpdatedRobot = await RobotService.ReadByIsarId(isarStatus.IsarId);
            if (preUpdatedRobot == null)
            {
                _logger.LogInformation("Received message from unknown ISAR instance {Id} with robot name {Name}", isarStatus.IsarId, isarStatus.RobotName);
                return;
            }
            _logger.LogInformation("OnIsarStatus: Robot {robotName} has status {robotStatus} and current area {areaName}", preUpdatedRobot.Name, preUpdatedRobot.Status, preUpdatedRobot.CurrentArea?.Name);

            var updatedRobot = await RobotService.UpdateRobotStatus(robot.Id, isarStatus.Status);
            _logger.LogInformation("Updated status for robot {Name} to {Status}", updatedRobot.Name, updatedRobot.Status);


            _logger.LogInformation("OnIsarStatus: Robot {robotName} has status {robotStatus} and current area {areaName}", updatedRobot.Name, updatedRobot.Status, updatedRobot.CurrentArea?.Name);

            if (isarStatus.Status == RobotStatus.Available) MissionScheduling.TriggerRobotAvailable(new RobotAvailableEventArgs(robot.Id));
            else if (isarStatus.Status == RobotStatus.Offline)
            {
                await RobotService.UpdateCurrentArea(robot.Id, null);
            }
        }

        private async void OnIsarRobotInfo(object? sender, MqttReceivedArgs mqttArgs)
        {
            var isarRobotInfo = (IsarRobotInfoMessage)mqttArgs.Message;

            var installation = await InstallationService.ReadByName(isarRobotInfo.CurrentInstallation, readOnly: true);

            if (installation is null)
            {
                _logger.LogError(
                    new InstallationNotFoundException($"No installation with code {isarRobotInfo.CurrentInstallation} found"),
                    "Could not create new robot due to missing installation"
                );
                return;
            }

            try
            {
                var robot = await RobotService.ReadByIsarId(isarRobotInfo.IsarId);

                if (robot == null)
                {
                    _logger.LogInformation(
                        "Received message from new ISAR instance '{Id}' with robot name '{Name}'. Adding new robot to database",
                        isarRobotInfo.IsarId, isarRobotInfo.RobotName);

                    var robotQuery = new CreateRobotQuery
                    {
                        IsarId = isarRobotInfo.IsarId,
                        Name = isarRobotInfo.RobotName,
                        RobotType = isarRobotInfo.RobotType,
                        SerialNumber = isarRobotInfo.SerialNumber,
                        CurrentInstallationCode = installation.InstallationCode,
                        VideoStreams = isarRobotInfo.VideoStreamQueries,
                        Host = isarRobotInfo.Host,
                        Port = isarRobotInfo.Port,
                        RobotCapabilities = isarRobotInfo.Capabilities,
                        Status = RobotStatus.Available,
                    };

                    try
                    {
                        var newRobot = await RobotService.CreateFromQuery(robotQuery);
                        _logger.LogInformation("Added robot '{RobotName}' with ISAR id '{IsarId}' to database", newRobot.Name, newRobot.IsarId);
                    }
                    catch (DbUpdateException)
                    {
                        _logger.LogError($"Failed to add robot {robotQuery.Name} with to the database");
                        return;
                    }

                    return;
                }

                List<string> updatedFields = [];

                if (isarRobotInfo.VideoStreamQueries is not null) UpdateVideoStreamsIfChanged(isarRobotInfo.VideoStreamQueries, ref robot, ref updatedFields);
                if (isarRobotInfo.Host is not null) UpdateHostIfChanged(isarRobotInfo.Host, ref robot, ref updatedFields);

                UpdatePortIfChanged(isarRobotInfo.Port, ref robot, ref updatedFields);

                if (isarRobotInfo.CurrentInstallation is not null) UpdateCurrentInstallationIfChanged(installation, ref robot, ref updatedFields);
                if (isarRobotInfo.Capabilities is not null) UpdateRobotCapabilitiesIfChanged(isarRobotInfo.Capabilities, ref robot, ref updatedFields);
                if (updatedFields.Count < 1) return;

                robot = await RobotService.Update(robot);

                _logger.LogInformation("Updated robot '{Id}' ('{RobotName}') in database: {Updates}", robot.Id, robot.Name, updatedFields);

            }
            catch (DbUpdateException e) { _logger.LogError(e, "Could not add robot to db"); }
            catch (Exception e) { _logger.LogError(e, "Could not update robot in db"); }
        }

        private static void UpdateVideoStreamsIfChanged(List<CreateVideoStreamQuery> videoStreamQueries, ref Robot robot, ref List<string> updatedFields)
        {
            var updatedStreams = videoStreamQueries
                .Select(
                    stream =>
                        new VideoStream
                        {
                            Name = stream.Name,
                            Url = stream.Url,
                            Type = stream.Type
                        }
                )
                .ToList();

            var existingVideoStreams = robot.VideoStreams;
            if (updatedStreams.Count == robot.VideoStreams.Count && updatedStreams.TrueForAll(stream => existingVideoStreams.Contains(stream))) return;
            var serializerOptions = new JsonSerializerOptions { WriteIndented = true };
            updatedFields.Add(
                $"\nVideoStreams ({JsonSerializer.Serialize(robot.VideoStreams, serializerOptions)} "
                + "\n-> "
                + $"\n{JsonSerializer.Serialize(updatedStreams, serializerOptions)})\n"
            );
            robot.VideoStreams = updatedStreams;
        }

        private static void UpdateHostIfChanged(string host, ref Robot robot, ref List<string> updatedFields)
        {
            if (host.Equals(robot.Host, StringComparison.Ordinal)) return;

            updatedFields.Add($"\nHost ({robot.Host} -> {host})\n");
            robot.Host = host;
        }

        private static void UpdatePortIfChanged(int port, ref Robot robot, ref List<string> updatedFields)
        {
            if (port.Equals(robot.Port)) return;

            updatedFields.Add($"\nPort ({robot.Port} -> {port})\n");
            robot.Port = port;
        }

        private static void UpdateCurrentInstallationIfChanged(Installation newCurrentInstallation, ref Robot robot, ref List<string> updatedFields)
        {
            if (newCurrentInstallation.InstallationCode.Equals(robot.CurrentInstallation?.InstallationCode, StringComparison.Ordinal)) return;

            updatedFields.Add($"\nCurrentInstallation ({robot.CurrentInstallation} -> {newCurrentInstallation})\n");
            robot.CurrentInstallation = newCurrentInstallation;
        }

        public static void UpdateRobotCapabilitiesIfChanged(IList<RobotCapabilitiesEnum> newRobotCapabilities, ref Robot robot, ref List<string> updatedFields)
        {
            if (robot.RobotCapabilities != null && Enumerable.SequenceEqual(newRobotCapabilities, robot.RobotCapabilities)) return;

            updatedFields.Add($"\nRobotCapabilities ({robot.RobotCapabilities} -> {newRobotCapabilities})\n");
            robot.RobotCapabilities = newRobotCapabilities;
        }

        private async void OnIsarMissionUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var isarMission = (IsarMissionMessage)mqttArgs.Message;

            MissionStatus status;
            try { status = MissionRun.GetMissionStatusFromString(isarMission.Status); }
            catch (ArgumentException e)
            {
                _logger.LogError(e, "Failed to parse mission status from MQTT message. Mission with ISARMissionId '{IsarMissionId}' was not updated", isarMission.MissionId);
                return;
            }

            var flotillaMissionRun = await MissionRunService.ReadByIsarMissionId(isarMission.MissionId, readOnly: true);
            if (flotillaMissionRun is null)
            {
                string errorMessage = $"Mission with isar mission Id {isarMission.IsarId} was not found";
                _logger.LogError("{Message}", errorMessage);
                return;
            }

            if (flotillaMissionRun.IsLocalizationMission())
            {
                if (flotillaMissionRun.Tasks.Any((task) => task.Status == Database.Models.TaskStatus.Successful || task.Status == Database.Models.TaskStatus.PartiallySuccessful))
                {
                    try
                    {
                        var robotWithUpdatedArea = await RobotService.UpdateCurrentArea(flotillaMissionRun.Robot.Id, flotillaMissionRun.Area.Id);
                    }
                    catch (RobotNotFoundException)
                    {
                        _logger.LogError("Could not find robot '{RobotName}' with ID '{Id}'", flotillaMissionRun.Robot.Name, flotillaMissionRun.Robot.Id);
                        return;
                    }
                }
                else if (flotillaMissionRun.Tasks.All((task) => task.Status == Database.Models.TaskStatus.Cancelled || task.Status == Database.Models.TaskStatus.Failed) || flotillaMissionRun.Status == MissionStatus.Aborted)
                {
                    try
                    {
                        await RobotService.UpdateCurrentArea(flotillaMissionRun.Robot.Id, null);

                        _logger.LogError("Localization mission run {MissionRunId} was unsuccessful on {RobotId}, scheduled missions will be aborted", flotillaMissionRun.Id, flotillaMissionRun.Robot.Id);
                        try { await MissionScheduling.AbortAllScheduledMissions(flotillaMissionRun.Robot.Id, "Aborted: Robot was not localized"); }
                        catch (RobotNotFoundException) { _logger.LogError("Failed to abort scheduled missions for robot {RobotId}", flotillaMissionRun.Robot.Id); }
                    }
                    catch (RobotNotFoundException)
                    {
                        _logger.LogError("Could not find robot '{RobotName}' with ID '{Id}'", flotillaMissionRun.Robot.Name, flotillaMissionRun.Robot.Id);
                        return;
                    }

                    SignalRService.ReportGeneralFailToSignalR(flotillaMissionRun.Robot, "Failed Localization Mission", $"Failed localization mission for robot {flotillaMissionRun.Robot.Name}.");
                    _logger.LogError("Localization mission for robot '{RobotName}' failed.", isarMission.RobotName);
                }
            }

            if (flotillaMissionRun.Status == status) { return; }

            MissionRun updatedFlotillaMissionRun;
            try { updatedFlotillaMissionRun = await MissionRunService.UpdateMissionRunStatusByIsarMissionId(isarMission.MissionId, status); }
            catch (MissionRunNotFoundException) { return; }

            _logger.LogInformation(
                "Mission '{Id}' (ISARMissionID='{IsarMissionId}') status updated to '{Status}' for robot '{RobotName}' with ISAR id '{IsarId}'",
                updatedFlotillaMissionRun.Id, isarMission.MissionId, isarMission.Status, isarMission.RobotName, isarMission.IsarId
            );

            if (!updatedFlotillaMissionRun.IsCompleted) return;

            var robot = await RobotService.ReadByIsarId(isarMission.IsarId);
            if (robot is null)
            {
                _logger.LogError("Could not find robot '{RobotName}' with ISAR id '{IsarId}'", isarMission.RobotName, isarMission.IsarId);
                return;
            }

            if (updatedFlotillaMissionRun.IsReturnHomeMission() && (updatedFlotillaMissionRun.Status == MissionStatus.Cancelled || updatedFlotillaMissionRun.Status == MissionStatus.Failed))
            {
                try
                {
                    await RobotService.UpdateCurrentArea(robot.Id, null);
                }
                catch (RobotNotFoundException)
                {
                    _logger.LogError("Could not find robot '{RobotName}' with ID '{Id}'", robot.Name, robot.Id);
                    return;
                }
            }

            try
            {
                await RobotService.UpdateCurrentMissionId(robot.Id, null);
            }
            catch (RobotNotFoundException)
            {
                _logger.LogError("Robot {robotName} not found when updating current mission id to null", robot.Name);
                return;
            }

            _logger.LogInformation("Robot '{Id}' ('{Name}') - completed mission run {MissionRunId}", robot.IsarId, robot.Name, updatedFlotillaMissionRun.Id);

            if (updatedFlotillaMissionRun.IsLocalizationMission() && (updatedFlotillaMissionRun.Status == MissionStatus.Successful || updatedFlotillaMissionRun.Status == MissionStatus.PartiallySuccessful))
            {
                _logger.LogInformation("Triggering localization mission successful. The robot {robotName} have status {robotStatus} and current area {areaName}", robot.Name, robot.Status, robot.CurrentArea?.Name);
                MissionScheduling.TriggerLocalizationMissionSuccessful(new LocalizationMissionSuccessfulEventArgs(robot.Id));
            }

            if (updatedFlotillaMissionRun.MissionId == null)
            {
                _logger.LogInformation("Mission run {missionRunId} does not have a mission definition assosiated with it", updatedFlotillaMissionRun.Id);
                return;
            }

            try { await LastMissionRunService.SetLastMissionRun(updatedFlotillaMissionRun.Id, updatedFlotillaMissionRun.MissionId); }
            catch (MissionNotFoundException)
            {
                _logger.LogError("Mission not found when setting last mission run for mission definition {missionId}", updatedFlotillaMissionRun.MissionId);
                return;
            }

            await TaskDurationService.UpdateAverageDurationPerTask(robot.Model.Type);
        }

        private async void OnIsarTaskUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var task = (IsarTaskMessage)mqttArgs.Message;

            IsarTaskStatus status;
            try { status = IsarTask.StatusFromString(task.Status); }
            catch (ArgumentException e)
            {
                _logger.LogError(e, "Failed to parse mission status from MQTT message. Mission '{Id}' was not updated", task.MissionId);
                return;
            }

            try { await MissionTaskService.UpdateMissionTaskStatus(task.TaskId, status); }
            catch (MissionTaskNotFoundException) { return; }

            var missionRun = await MissionRunService.ReadByIsarMissionId(task.MissionId, readOnly: true);
            if (missionRun is null)
            {
                _logger.LogWarning("Mission run with ID {Id} was not found", task.MissionId);
            }

            _ = SignalRService.SendMessageAsync("Mission run updated", missionRun?.Area?.Installation, missionRun != null ? new MissionRunResponse(missionRun) : null);

            _logger.LogInformation(
                "Task '{Id}' updated to '{Status}' for robot '{RobotName}' with ISAR id '{IsarId}'", task.TaskId, task.Status, task.RobotName, task.IsarId);
        }

        private async void OnIsarStepUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var step = (IsarStepMessage)mqttArgs.Message;

            // Flotilla does not care about DriveTo, Localization, MoveArm or ReturnToHome steps
            var stepType = IsarStep.StepTypeFromString(step.StepType);
            if (stepType is IsarStepType.DriveToPose or IsarStepType.Localize or IsarStepType.MoveArm or IsarStepType.ReturnToHome) return;

            IsarStepStatus status;
            try { status = IsarStep.StatusFromString(step.Status); }
            catch (ArgumentException e)
            {
                _logger.LogError(e, "Failed to parse mission status from MQTT message. Mission '{Id}' was not updated", step.MissionId);
                return;
            }

            try { await InspectionService.UpdateInspectionStatus(step.StepId, status); }
            catch (InspectionNotFoundException) { return; }

            var missionRun = await MissionRunService.ReadByIsarMissionId(step.MissionId, readOnly: true);
            if (missionRun is null) _logger.LogWarning("Mission run with ID {Id} was not found", step.MissionId);

            _ = SignalRService.SendMessageAsync("Mission run updated", missionRun?.Area?.Installation, missionRun != null ? new MissionRunResponse(missionRun) : null);

            _logger.LogInformation(
                "Inspection '{Id}' updated to '{Status}' for robot '{RobotName}' with ISAR id '{IsarId}'", step.StepId, step.Status, step.RobotName, step.IsarId);
        }

        private async void OnIsarBatteryUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var batteryStatus = (IsarBatteryMessage)mqttArgs.Message;
            var robot = await BatteryTimeseriesService.AddBatteryEntry(batteryStatus.BatteryLevel, batteryStatus.IsarId);
            if (robot == null) return;
            robot.BatteryLevel = batteryStatus.BatteryLevel;

            if (robot.FlotillaStatus == RobotFlotillaStatus.Normal && robot.IsRobotBatteryTooLow())
            {
                _logger.LogInformation("Sending robot '{RobotName}' to its safe zone as its battery level is too low.", robot.Name);
                EmergencyActionService.SendRobotToSafezone(new RobotEmergencyEventArgs(robot.Id, RobotFlotillaStatus.Recharging));
            }
            else if (robot.FlotillaStatus == RobotFlotillaStatus.Recharging && !(robot.IsRobotBatteryTooLow() || robot.IsRobotPressureTooHigh() || robot.IsRobotPressureTooLow()))
            {
                _logger.LogInformation("Releasing robot '{RobotName}' from its safe zone as its battery and pressure levels are good enough to run missions.", robot.Name);
                EmergencyActionService.ReleaseRobotFromSafezone(new RobotEmergencyEventArgs(robot.Id, RobotFlotillaStatus.Normal));
            }
        }

        private async void OnIsarPressureUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var pressureStatus = (IsarPressureMessage)mqttArgs.Message;
            var robot = await PressureTimeseriesService.AddPressureEntry(pressureStatus.PressureLevel, pressureStatus.IsarId);
            if (robot == null) return;
            robot.PressureLevel = pressureStatus.PressureLevel;

            if (robot.FlotillaStatus == RobotFlotillaStatus.Normal && (robot.IsRobotPressureTooLow() || robot.IsRobotPressureTooHigh()))
            {
                _logger.LogInformation("Sending robot '{RobotName}' to its safe zone as its pressure is too low or high.", robot.Name);
                EmergencyActionService.SendRobotToSafezone(new RobotEmergencyEventArgs(robot.Id, RobotFlotillaStatus.Recharging));
            }
            else if (robot.FlotillaStatus == RobotFlotillaStatus.Recharging && !(robot.IsRobotBatteryTooLow() || robot.IsRobotPressureTooHigh() || robot.IsRobotPressureTooLow()))
            {
                _logger.LogInformation("Releasing robot '{RobotName}' from its safe zone as its battery and pressure levels are good enough to run missions.", robot.Name);
                EmergencyActionService.ReleaseRobotFromSafezone(new RobotEmergencyEventArgs(robot.Id, RobotFlotillaStatus.Normal));
            }
        }

        private async void OnIsarPoseUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var poseStatus = (IsarPoseMessage)mqttArgs.Message;
            var pose = new Pose(poseStatus.Pose);

            await PoseTimeseriesService.AddPoseEntry(pose, poseStatus.IsarId);
        }

        private async void OnIsarCloudHealthUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var cloudHealthStatus = (IsarCloudHealthMessage)mqttArgs.Message;

            var robot = await RobotService.ReadByIsarId(cloudHealthStatus.IsarId);
            if (robot == null)
            {
                _logger.LogInformation("Received message from unknown ISAR instance {Id} with robot name {Name}", cloudHealthStatus.IsarId, cloudHealthStatus.RobotName);
                return;
            }

            string messageTitle = "Failed Telemetry";
            string message = $"Failed telemetry request for robot {cloudHealthStatus.RobotName}.";
            SignalRService.ReportGeneralFailToSignalR(robot, messageTitle, message);

            TeamsMessageService.TriggerTeamsMessageReceived(new TeamsMessageEventArgs(message));
        }
    }
}
