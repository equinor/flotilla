using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Mqtt;
using Api.Mqtt.Events;
using Api.Mqtt.MessageModels;
using Api.Services;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.IdentityModel.Tokens;

namespace Api.EventHandlers
{
    /// <summary>
    ///     A background service which listens to events and performs callback functions.
    /// </summary>
    public class MqttEventHandler : EventHandlerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MqttEventHandler> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public MqttEventHandler(
            ILogger<MqttEventHandler> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration config
        )
        {
            _logger = logger;
            _configuration = config;
            // Reason for using factory: https://www.thecodebuzz.com/using-dbcontext-instance-in-ihostedservice/
            _scopeFactory = scopeFactory;

            Subscribe();
        }

        private IServiceProvider GetServiceProvider()
        {
            return _scopeFactory.CreateScope().ServiceProvider;
        }

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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await stoppingToken;
        }

        private async void OnIsarRobotStatus(object? sender, MqttReceivedArgs mqttArgs)
        {
            var provider = GetServiceProvider();
            var robotService = provider.GetRequiredService<IRobotService>();
            var isarRobotStatus = (IsarRobotStatusMessage)mqttArgs.Message;
            var robot = await robotService.ReadByIsarId(isarRobotStatus.IsarId);

            if (robot == null)
            {
                _logger.LogInformation(
                    "Received message from unknown ISAR instance {id} with robot name {name}.",
                    isarRobotStatus.IsarId,
                    isarRobotStatus.RobotName
                );
                return;
            }

            if (robot.Status == isarRobotStatus.RobotStatus || robot.Status == RobotStatus.SafePosition)
            {
                return;
            }

            if (robot.Status != isarRobotStatus.RobotStatus && robot.CurrentMissionId != null)
            {
                return;
            }

            robot.Status = isarRobotStatus.RobotStatus;
            robot = await robotService.Update(robot);
            _logger.LogInformation(
                "Updated status for robot {name} to {status}",
                robot.Name,
                robot.Status
            );
        }

        private async void OnIsarRobotInfo(object? sender, MqttReceivedArgs mqttArgs)
        {
            var provider = GetServiceProvider();
            var robotService = provider.GetRequiredService<IRobotService>();
            var robotModelService = provider.GetRequiredService<IRobotModelService>();

            var isarRobotInfo = (IsarRobotInfoMessage)mqttArgs.Message;
            var robot = await robotService.ReadByIsarId(isarRobotInfo.IsarId);

            if (robot == null)
            {
                _logger.LogInformation(
                    "Received message from new ISAR instance '{id}' with robot name '{name}'. Adding new robot to database.",
                    isarRobotInfo.IsarId,
                    isarRobotInfo.RobotName
                );

                var robotQuery = new CreateRobotQuery
                {
                    IsarId = isarRobotInfo.IsarId,
                    Name = isarRobotInfo.RobotName,
                    RobotType = isarRobotInfo.RobotType,
                    SerialNumber = isarRobotInfo.SerialNumber,
                    CurrentAsset = isarRobotInfo.CurrentAsset,
                    VideoStreams = isarRobotInfo.VideoStreamQueries,
                    Host = isarRobotInfo.Host,
                    Port = isarRobotInfo.Port,
                    Status = RobotStatus.Available,
                    Enabled = true
                };

                var robotModel = await robotModelService.ReadByRobotType(robotQuery.RobotType);
                if (robotModel == null)
                {
                    _logger.LogError(
                        "Could not create new robot for ISAR instance '{id}' because the provided robot type '{robotType}' does not exist",
                        isarRobotInfo.IsarId,
                        isarRobotInfo.RobotType
                    );
                    return;
                }

                var newRobot = new Robot(robotQuery) { Model = robotModel };
                newRobot = await robotService.Create(newRobot);
                _logger.LogInformation(
                    "Added robot '{robotName}' with ISAR id '{isarId}' to database",
                    newRobot.Name,
                    newRobot.IsarId
                );

                return;
            }

            List<string> updatedFields = new();

            if (isarRobotInfo.VideoStreamQueries is not null)
            {
                UpdateVideoStreamsIfChanged(isarRobotInfo.VideoStreamQueries, ref robot, ref updatedFields);
            }

            if (isarRobotInfo.Host is not null)
            {
                UpdateHostIfChanged(isarRobotInfo.Host, ref robot, ref updatedFields);
            }

            UpdatePortIfChanged(isarRobotInfo.Port, ref robot, ref updatedFields);

            if (isarRobotInfo.CurrentAsset is not null)
            {
                UpdateCurrentAssetIfChanged(isarRobotInfo.CurrentAsset, ref robot, ref updatedFields);
            }

            if (!updatedFields.IsNullOrEmpty())
            {
                robot = await robotService.Update(robot);
                _logger.LogInformation(
                    "Updated robot '{id}' ('{robotName}') in database: {updates}",
                    robot.Id,
                    robot.Name,
                    updatedFields
                );
            }
        }

        private static void UpdateVideoStreamsIfChanged(List<CreateVideoStreamQuery> videoStreamQueries,
            ref Robot robot, ref List<string> updatedFields)
        {
            var updatedStreams = videoStreamQueries
                .Select(
                    stream =>
                        new VideoStream { Name = stream.Name, Url = stream.Url, Type = stream.Type }
                )
                .ToList();

            var existingVideoStreams = robot.VideoStreams;
            if (
                updatedStreams.Count == robot.VideoStreams.Count
                && updatedStreams.TrueForAll(stream => existingVideoStreams.Contains(stream))
            )
            {
                return;
            }

            updatedFields.Add(
                $"\nVideoStreams ({JsonSerializer.Serialize(robot.VideoStreams, new JsonSerializerOptions { WriteIndented = true })} "
                + "\n-> "
                + $"\n{JsonSerializer.Serialize(updatedStreams, new JsonSerializerOptions { WriteIndented = true })})\n"
            );
            robot.VideoStreams = updatedStreams;
        }

        private static void UpdateHostIfChanged(string host, ref Robot robot, ref List<string> updatedFields)
        {
            if (host.Equals(robot.Host, StringComparison.Ordinal))
            {
                return;
            }

            updatedFields.Add($"\nHost ({robot.Host} -> {host})\n");
            robot.Host = host;
        }

        private static void UpdatePortIfChanged(int port, ref Robot robot,
            ref List<string> updatedFields)
        {
            if (port.Equals(robot.Port))
            {
                return;
            }

            updatedFields.Add($"\nPort ({robot.Port} -> {port})\n");
            robot.Port = port;
        }

        private static void UpdateCurrentAssetIfChanged(string newCurrentAsset, ref Robot robot,
            ref List<string> updatedFields)
        {
            if (newCurrentAsset.Equals(robot.CurrentAsset, StringComparison.Ordinal))
            {
                return;
            }

            updatedFields.Add($"\nCurrentAsset ({robot.CurrentAsset} -> {newCurrentAsset})\n");
            robot.CurrentAsset = newCurrentAsset;
        }

        private async void OnMissionUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var provider = GetServiceProvider();
            var missionService = provider.GetRequiredService<IMissionService>();
            var robotService = provider.GetRequiredService<IRobotService>();
            var robotModelService = provider.GetRequiredService<IRobotModelService>();

            var isarMission = (IsarMissionMessage)mqttArgs.Message;
            MissionStatus status;
            try
            {
                status = Mission.MissionStatusFromString(isarMission.Status);
            }
            catch (ArgumentException e)
            {
                _logger.LogError(
                    e,
                    "Failed to parse mission status from MQTT message. Mission with ISARMissionId '{isarMissionId}' was not updated.",
                    isarMission.MissionId
                );
                return;
            }

            var flotillaMission = await missionService.UpdateMissionStatusByIsarMissionId(
                isarMission.MissionId,
                status
            );

            if (flotillaMission is null)
            {
                _logger.LogError(
                    "No mission found with ISARMissionId '{isarMissionId}'. Could not update status to '{status}'",
                    isarMission.MissionId,
                    status
                );
                return;
            }

            _logger.LogInformation(
                "Mission '{id}' (ISARMissionID='{isarId}') status updated to '{status}' for robot '{robotName}' with ISAR id '{isarId}'",
                flotillaMission.Id,
                isarMission.MissionId,
                isarMission.Status,
                isarMission.RobotName,
                isarMission.IsarId
            );

            var robot = await robotService.ReadByIsarId(isarMission.IsarId);
            if (robot is null)
            {
                _logger.LogError(
                    "Could not find robot '{robotName}' with ISAR id '{isarId}'. The robot status is not updated.",
                    isarMission.RobotName,
                    isarMission.IsarId
                );
                return;
            }

            if (flotillaMission.IsCompleted && robot.Status != RobotStatus.SafePosition)
            {
                robot.Status = flotillaMission.IsCompleted ? RobotStatus.Available : RobotStatus.Busy;

            }

            if (flotillaMission.IsCompleted)
            {
                robot.CurrentMissionId = null;
            }

            await robotService.Update(robot);
            _logger.LogInformation(
                "Robot '{id}' ('{name}') - status set to '{status}'.",
                robot.IsarId,
                robot.Name,
                robot.Status
            );

            if (flotillaMission.IsCompleted)
            {
                int timeRangeInDays = _configuration.GetValue<int>(
                    "TimeRangeForMissionDurationEstimationInDays"
                );
                long minEpochTime = DateTimeOffset.Now
                    .AddDays(-timeRangeInDays)
                    .ToUnixTimeSeconds();
                var missionsForEstimation = await missionService.ReadAll(
                    new MissionQueryStringParameters
                    {
                        MinDesiredStartTime = minEpochTime,
                        RobotModelType = robot.Model.Type,
                        PageSize = QueryStringParameters.MaxPageSize
                    }
                );
                var model = robot.Model;
                model.UpdateAverageDurationPerTag(missionsForEstimation);

                await robotModelService.Update(model);

                _logger.LogInformation(
                    "Robot model '{modelType}' - Updated average time spent per tag to {averageTimeSpent}s",
                    model.Type,
                    model.AverageDurationPerTag
                );
            }
        }

        private async void OnTaskUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var provider = GetServiceProvider();
            var missionService = provider.GetRequiredService<IMissionService>();
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
                    "Failed to parse mission status from MQTT message. Mission '{id}' was not updated.",
                    task.MissionId
                );
                return;
            }

            bool success = await missionService.UpdateTaskStatusByIsarTaskId(
                task.MissionId,
                task.TaskId,
                status
            );

            if (success)
            {
                _logger.LogInformation(
                    "Task '{id}' updated to '{status}' for robot '{robotName}' with ISAR id '{isarId}'",
                    task.TaskId,
                    task.Status,
                    task.RobotName,
                    task.IsarId
                );
            }
        }

        private async void OnStepUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var provider = GetServiceProvider();
            var missionService = provider.GetRequiredService<IMissionService>();

            var step = (IsarStepMessage)mqttArgs.Message;

            // Flotilla does not care about DriveTo or localization steps
            var stepType = IsarStep.StepTypeFromString(step.StepType);
            if (stepType is IsarStepType.DriveToPose || stepType is IsarStepType.Localize)
            {
                return;
            }

            IsarStepStatus status;
            try
            {
                status = IsarStep.StatusFromString(step.Status);
            }
            catch (ArgumentException e)
            {
                _logger.LogError(
                    e,
                    "Failed to parse mission status from MQTT message. Mission '{id}' was not updated.",
                    step.MissionId
                );
                return;
            }

            bool success = await missionService.UpdateStepStatusByIsarStepId(
                step.MissionId,
                step.TaskId,
                step.StepId,
                status
            );

            if (success)
            {
                _logger.LogInformation(
                    "Inspection '{id}' updated to '{status}' for robot '{robotName}' with ISAR id '{isarId}'",
                    step.StepId,
                    step.Status,
                    step.RobotName,
                    step.IsarId
                );
            }
        }

        private async void OnBatteryUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var provider = GetServiceProvider();
            var robotService = provider.GetRequiredService<IRobotService>();

            var batteryStatus = (IsarBatteryMessage)mqttArgs.Message;
            var robot = await robotService.ReadByIsarId(batteryStatus.IsarId);
            if (robot == null)
            {
                _logger.LogWarning(
                    "Could not find corresponding robot for battery update on robot '{robotName}' with ISAR id '{isarId}'",
                    batteryStatus.RobotName,
                    batteryStatus.IsarId
                );
            }
            else
            {
                robot.BatteryLevel = batteryStatus.BatteryLevel;
                await robotService.Update(robot);
                _logger.LogDebug(
                    "Updated battery on robot '{robotName}' with ISAR id '{isarId}'",
                    robot.Name,
                    robot.IsarId
                );
            }
        }

        private async void OnPressureUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var provider = GetServiceProvider();
            var robotService = provider.GetRequiredService<IRobotService>();

            var pressureStatus = (IsarPressureMessage)mqttArgs.Message;
            var robot = await robotService.ReadByIsarId(pressureStatus.IsarId);
            if (robot == null)
            {
                _logger.LogWarning(
                    "Could not find corresponding robot for pressure update on robot '{robotName}' with ISAR id '{isarId}'",
                    pressureStatus.RobotName,
                    pressureStatus.IsarId
                );
            }
            else
            {
                robot.PressureLevel = pressureStatus.PressureLevel;
                await robotService.Update(robot);
                _logger.LogDebug(
                    "Updated pressure on '{robotName}' with ISAR id '{isarId}'",
                    robot.Name,
                    robot.IsarId
                );
            }
        }

        private async void OnPoseUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var provider = GetServiceProvider();
            var robotService = provider.GetRequiredService<IRobotService>();

            var poseStatus = (IsarPoseMessage)mqttArgs.Message;
            var robot = await robotService.ReadByIsarId(poseStatus.IsarId);
            if (robot == null)
            {
                _logger.LogWarning(
                    "Could not find corresponding robot for pose update on robot '{robotName}' with ISAR id '{isarId}'",
                    poseStatus.RobotName,
                    poseStatus.IsarId
                );
            }
            else
            {
                try
                {
                    poseStatus.Pose.CopyIsarPoseToRobotPose(robot.Pose);
                }
                catch (NullReferenceException e)
                {
                    _logger.LogWarning(
                        "NullReferenceException while updating pose on robot '{robotName}' with ISAR id '{isarId}': {message}",
                        robot.Name,
                        robot.IsarId,
                        e.Message
                    );
                }

                await robotService.Update(robot);
                _logger.LogDebug(
                    "Updated pose on robot '{robotName}' with ISAR id '{isarId}'",
                    robot.Name,
                    robot.IsarId
                );
            }
        }
    }
}
