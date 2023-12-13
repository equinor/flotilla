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
using Microsoft.IdentityModel.Tokens;
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


        private IServiceProvider GetServiceProvider() { return _scopeFactory.CreateScope().ServiceProvider; }

        public override void Subscribe()
        {
            MqttService.MqttIsarRobotStatusReceived += OnIsarRobotStatus;
            MqttService.MqttIsarRobotInfoReceived += OnIsarRobotInfo;
            MqttService.MqttIsarMissionReceived += OnMissionUpdate;
            MqttService.MqttIsarTaskReceived += OnTaskUpdate;
            MqttService.MqttIsarStepReceived += OnStepUpdate;
            MqttService.MqttIsarBatteryReceived += OnBatteryUpdate;
            MqttService.MqttIsarPressureReceived += OnPressureUpdate;
            MqttService.MqttIsarPoseReceived += OnPoseUpdate;
        }

        public override void Unsubscribe()
        {
            MqttService.MqttIsarRobotStatusReceived -= OnIsarRobotStatus;
            MqttService.MqttIsarRobotInfoReceived -= OnIsarRobotInfo;
            MqttService.MqttIsarMissionReceived -= OnMissionUpdate;
            MqttService.MqttIsarTaskReceived -= OnTaskUpdate;
            MqttService.MqttIsarStepReceived -= OnStepUpdate;
            MqttService.MqttIsarBatteryReceived -= OnBatteryUpdate;
            MqttService.MqttIsarPressureReceived -= OnPressureUpdate;
            MqttService.MqttIsarPoseReceived -= OnPoseUpdate;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) { await stoppingToken; }


        private async void OnIsarRobotStatus(object? sender, MqttReceivedArgs mqttArgs)
        {
            var provider = GetServiceProvider();
            var robotService = provider.GetRequiredService<IRobotService>();
            var missionSchedulingService = provider.GetRequiredService<IMissionSchedulingService>();

            var isarRobotStatus = (IsarRobotStatusMessage)mqttArgs.Message;

            var robot = await robotService.ReadByIsarId(isarRobotStatus.IsarId);

            if (robot == null)
            {
                _logger.LogInformation("Received message from unknown ISAR instance {Id} with robot name {Name}", isarRobotStatus.IsarId, isarRobotStatus.RobotName);
                return;
            }

            if (robot.Status == isarRobotStatus.RobotStatus)
            {
                _logger.LogInformation("Robot {robotName} received a new isar robot status {isarRobotStatus}, but the robot status was already the same", robot.Name, isarRobotStatus.RobotStatus);
                return;
            }

            await robotService.UpdateRobotStatus(robot.Id, isarRobotStatus.RobotStatus);
            _logger.LogInformation("Updated status for robot {Name} to {Status}", robot.Name, robot.Status);

            if (isarRobotStatus.RobotStatus == RobotStatus.Available) missionSchedulingService.TriggerRobotAvailable(new RobotAvailableEventArgs(robot.Id));
        }

        private async void OnIsarRobotInfo(object? sender, MqttReceivedArgs mqttArgs)
        {
            var provider = GetServiceProvider();
            var robotService = provider.GetRequiredService<IRobotService>();
            var installationService = provider.GetRequiredService<IInstallationService>();

            var isarRobotInfo = (IsarRobotInfoMessage)mqttArgs.Message;

            var installation = await installationService.ReadByName(isarRobotInfo.CurrentInstallation);

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
                var robot = await robotService.ReadByIsarId(isarRobotInfo.IsarId);

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
                        Status = RobotStatus.Available,
                        Enabled = true
                    };

                    var newRobot = await robotService.CreateFromQuery(robotQuery);
                    _logger.LogInformation("Added robot '{RobotName}' with ISAR id '{IsarId}' to database", newRobot.Name, newRobot.IsarId);

                    return;
                }

                List<string> updatedFields = [];

                if (isarRobotInfo.VideoStreamQueries is not null) UpdateVideoStreamsIfChanged(isarRobotInfo.VideoStreamQueries, ref robot, ref updatedFields);
                if (isarRobotInfo.Host is not null) UpdateHostIfChanged(isarRobotInfo.Host, ref robot, ref updatedFields);

                UpdatePortIfChanged(isarRobotInfo.Port, ref robot, ref updatedFields);

                if (isarRobotInfo.CurrentInstallation is not null) UpdateCurrentInstallationIfChanged(installation, ref robot, ref updatedFields);
                if (updatedFields.IsNullOrEmpty()) return;

                robot = await robotService.Update(robot);
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

        private async void OnMissionUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var provider = GetServiceProvider();
            var missionRunService = provider.GetRequiredService<IMissionRunService>();
            var robotService = provider.GetRequiredService<IRobotService>();
            var taskDurationService = provider.GetRequiredService<ITaskDurationService>();
            var lastMissionRunService = provider.GetRequiredService<ILastMissionRunService>();

            var isarMission = (IsarMissionMessage)mqttArgs.Message;

            MissionStatus status;
            try { status = MissionRun.MissionStatusFromString(isarMission.Status); }
            catch (ArgumentException e)
            {
                _logger.LogError(e, "Failed to parse mission status from MQTT message. Mission with ISARMissionId '{IsarMissionId}' was not updated", isarMission.MissionId);
                return;
            }

            MissionRun flotillaMissionRun;
            try { flotillaMissionRun = await missionRunService.UpdateMissionRunStatusByIsarMissionId(isarMission.MissionId, status); }
            catch (MissionRunNotFoundException) { return; }

            _logger.LogInformation(
                "Mission '{Id}' (ISARMissionID='{IsarMissionId}') status updated to '{Status}' for robot '{RobotName}' with ISAR id '{IsarId}'",
                flotillaMissionRun.Id, isarMission.MissionId, isarMission.Status, isarMission.RobotName, isarMission.IsarId
            );

            if (!flotillaMissionRun.IsCompleted) return;

            var robot = await robotService.ReadByIsarId(isarMission.IsarId);
            if (robot is null)
            {
                _logger.LogError("Could not find robot '{RobotName}' with ISAR id '{IsarId}'", isarMission.RobotName, isarMission.IsarId);
                return;
            }

            try { await robotService.UpdateCurrentMissionId(robot.Id, null); }
            catch (RobotNotFoundException)
            {
                _logger.LogError("Robot {robotName} not found when updating current mission id to null", robot.Name);
                return;
            }

            _logger.LogInformation("Robot '{Id}' ('{Name}') - completed mission run {MissionRunId}", robot.IsarId, robot.Name, flotillaMissionRun.Id);

            if (flotillaMissionRun.MissionId == null)
            {
                _logger.LogInformation("Mission run {missionRunId} does not have a mission definition assosiated with it", flotillaMissionRun.Id);
                return;
            }

            try { await lastMissionRunService.SetLastMissionRun(flotillaMissionRun.Id, flotillaMissionRun.MissionId); }
            catch (MissionNotFoundException)
            {
                _logger.LogError("Mission not found when setting last mission run for mission definition {missionId}", flotillaMissionRun.MissionId);
                return;
            }

            await taskDurationService.UpdateAverageDurationPerTask(robot.Model.Type);
        }

        private async void OnTaskUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var provider = GetServiceProvider();
            var missionRunService = provider.GetRequiredService<IMissionRunService>();
            var missionTaskService = provider.GetRequiredService<IMissionTaskService>();
            var signalRService = provider.GetRequiredService<ISignalRService>();
            var task = (IsarTaskMessage)mqttArgs.Message;

            IsarTaskStatus status;
            try { status = IsarTask.StatusFromString(task.Status); }
            catch (ArgumentException e)
            {
                _logger.LogError(e, "Failed to parse mission status from MQTT message. Mission '{Id}' was not updated", task.MissionId);
                return;
            }

            try { await missionTaskService.UpdateMissionTaskStatus(task.TaskId, status); }
            catch (MissionTaskNotFoundException) { return; }

            var missionRun = await missionRunService.ReadByIsarMissionId(task.MissionId);
            if (missionRun is null) _logger.LogWarning("Mission run with ID {Id} was not found", task.MissionId);

            _ = signalRService.SendMessageAsync("Mission run updated", missionRun?.Area?.Installation, missionRun);

            _logger.LogInformation(
                "Task '{Id}' updated to '{Status}' for robot '{RobotName}' with ISAR id '{IsarId}'", task.TaskId, task.Status, task.RobotName, task.IsarId);
        }

        private async void OnStepUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var provider = GetServiceProvider();
            var missionRunService = provider.GetRequiredService<IMissionRunService>();
            var inspectionService = provider.GetRequiredService<IInspectionService>();
            var signalRService = provider.GetRequiredService<ISignalRService>();

            var step = (IsarStepMessage)mqttArgs.Message;

            // Flotilla does not care about DriveTo or localization steps
            var stepType = IsarStep.StepTypeFromString(step.StepType);
            if (stepType is IsarStepType.DriveToPose or IsarStepType.Localize) return;

            IsarStepStatus status;
            try { status = IsarStep.StatusFromString(step.Status); }
            catch (ArgumentException e)
            {
                _logger.LogError(e, "Failed to parse mission status from MQTT message. Mission '{Id}' was not updated", step.MissionId);
                return;
            }

            try { await inspectionService.UpdateInspectionStatus(step.StepId, status); }
            catch (InspectionNotFoundException) { return; }

            var missionRun = await missionRunService.ReadByIsarMissionId(step.MissionId);
            if (missionRun is null) _logger.LogWarning("Mission run with ID {Id} was not found", step.MissionId);

            _ = signalRService.SendMessageAsync("Mission run updated", missionRun?.Area?.Installation, missionRun);

            _logger.LogInformation(
                "Inspection '{Id}' updated to '{Status}' for robot '{RobotName}' with ISAR id '{IsarId}'", step.StepId, step.Status, step.RobotName, step.IsarId);
        }

        private async void OnBatteryUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var provider = GetServiceProvider();
            var robotService = provider.GetRequiredService<IRobotService>();
            var timeseriesService = provider.GetRequiredService<ITimeseriesService>();

            var batteryStatus = (IsarBatteryMessage)mqttArgs.Message;

            var robot = await robotService.ReadByIsarId(batteryStatus.IsarId);
            if (robot == null)
            {
                _logger.LogWarning(
                    "Could not find corresponding robot for battery update on robot '{RobotName}' with ISAR id '{IsarId}'", batteryStatus.RobotName, batteryStatus.IsarId);
            }
            else
            {
                await robotService.UpdateRobotBatteryLevel(robot.Id, batteryStatus.BatteryLevel);
                await timeseriesService.Create(
                    new RobotBatteryTimeseries
                    {
                        MissionId = robot.CurrentMissionId,
                        BatteryLevel = batteryStatus.BatteryLevel,
                        RobotId = robot.Id,
                        Time = DateTime.UtcNow
                    }
                );
                _logger.LogDebug("Updated battery on robot '{RobotName}' with ISAR id '{IsarId}'", robot.Name, robot.IsarId);
            }
        }

        private async void OnPressureUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var provider = GetServiceProvider();
            var robotService = provider.GetRequiredService<IRobotService>();
            var timeseriesService = provider.GetRequiredService<ITimeseriesService>();

            var pressureStatus = (IsarPressureMessage)mqttArgs.Message;

            var robot = await robotService.ReadByIsarId(pressureStatus.IsarId);
            if (robot == null)
            {
                _logger.LogWarning(
                    "Could not find corresponding robot for pressure update on robot '{RobotName}' with ISAR id '{IsarId}'", pressureStatus.RobotName, pressureStatus.IsarId);
            }
            else
            {
                if (pressureStatus.PressureLevel == robot.PressureLevel) return;

                await robotService.UpdateRobotPressureLevel(robot.Id, pressureStatus.PressureLevel);
                await timeseriesService.Create(
                    new RobotPressureTimeseries
                    {
                        MissionId = robot.CurrentMissionId,
                        Pressure = pressureStatus.PressureLevel,
                        RobotId = robot.Id,
                        Time = DateTime.UtcNow
                    }
                );
                _logger.LogDebug("Updated pressure on '{RobotName}' with ISAR id '{IsarId}'", robot.Name, robot.IsarId);
            }
        }

        private async void OnPoseUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var provider = GetServiceProvider();
            var poseTimeseriesService = provider.GetRequiredService<IPoseTimeseriesService>();

            var poseStatus = (IsarPoseMessage)mqttArgs.Message;
            var pose = new Pose(poseStatus.Pose);

            await poseTimeseriesService.AddPoseEntry(pose, poseStatus.IsarId);
        }
    }
}
