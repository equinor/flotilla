using Api.Controllers.Models;
using Api.Database.Models;
using Api.Mqtt;
using Api.Mqtt.Events;
using Api.Mqtt.MessageModels;
using Api.Services;
using Api.Utilities;

namespace Api.EventHandlers
{
    /// <summary>
    /// A background service which listens to events and performs callback functions.
    /// </summary>
    public class MqttEventHandler : EventHandlerBase
    {
        private readonly ILogger<MqttEventHandler> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private IMissionService MissionService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionService>();
        private IRobotService RobotService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IRobotService>();

        public MqttEventHandler(ILogger<MqttEventHandler> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;

            // Reason for using factory: https://www.thecodebuzz.com/using-dbcontext-instance-in-ihostedservice/
            _scopeFactory = scopeFactory;

            Subscribe();
        }

        public override void Subscribe()
        {
            MqttService.MqttIsarRobotStatusReceived += OnIsarRobotStatus;
            MqttService.MqttIsarRobotInfoReceived += OnIsarRobotInfo;
            MqttService.MqttIsarMissionReceived += OnMissionUpdate;
            MqttService.MqttIsarTaskReceived += OnTaskUpdate;
            MqttService.MqttIsarStepReceived += OnStepUpdate;
            MqttService.MqttIsarBatteryReceived += OnBatteryUpdate;
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
            MqttService.MqttIsarPoseReceived -= OnPoseUpdate;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await stoppingToken;
        }

        private async void OnIsarRobotStatus(object? sender, MqttReceivedArgs mqttArgs)
        {
            var isarRobotStatus = (IsarRobotStatusMessage)mqttArgs.Message;
            var robot = await RobotService.ReadByName(isarRobotStatus.RobotName);

            if (robot == null)
            {
                _logger.LogInformation(
                    "Received message from unknown ISAR instance with robot name {name}.",
                    isarRobotStatus.RobotName
                );
                return;
            }

            robot.Status = isarRobotStatus.RobotStatus;
            robot = await RobotService.Update(robot);
            _logger.LogInformation(
                "Updated status for robot {name} to {status}",
                robot.Name,
                robot.Status
            );
        }

        private async void OnIsarRobotInfo(object? sender, MqttReceivedArgs mqttArgs)
        {
            var isarRobotInfo = (IsarRobotInfoMessage)mqttArgs.Message;
            var robot = await RobotService.ReadByName(isarRobotInfo.RobotName);

            if (robot == null)
            {
                _logger.LogInformation(
                    "Received message from new ISAR instance with robot name {name}. Adding new robot to database.",
                    isarRobotInfo.RobotName
                );

                var robotQuery = new CreateRobotQuery()
                {
                    Name = isarRobotInfo.RobotName,
                    Model = isarRobotInfo.RobotModel,
                    SerialNumber = isarRobotInfo.SerialNumber,
                    VideoStreams = isarRobotInfo.VideoStreamQueries,
                    Host = isarRobotInfo.Host,
                    Port = isarRobotInfo.Port,
                    Status = RobotStatus.Available,
                    Enabled = true
                };

                robot = await RobotService.Create(robotQuery);
                _logger.LogInformation("Added robot {name} to database", robot.Name);
            }
            else if (RobotHasSignificantChange(robot, isarRobotInfo))
            {
                _logger.LogInformation(
                    "A change was discovered on robot {name} and the database will be updated",
                    robot.Name
                );

                var existingVideoStreams = new List<string>();
                foreach (var existingVideoStream in robot.VideoStreams)
                    existingVideoStreams.Add(existingVideoStream.Name);

                foreach (var videoStreamQuery in isarRobotInfo.VideoStreamQueries)
                {
                    if (!existingVideoStreams.Contains(videoStreamQuery.Name))
                    {
                        var videoStream = new VideoStream
                        {
                            Name = videoStreamQuery.Name,
                            Url = videoStreamQuery.Url,
                            Type = videoStreamQuery.Type
                        };
                        robot.VideoStreams.Add(videoStream);
                    }
                }

                robot.Host = isarRobotInfo.Host;
                robot.Port = isarRobotInfo.Port;

                robot = await RobotService.Update(robot);
                _logger.LogInformation("Updated robot {name} in database", robot.Name);
            }
        }

        private static bool RobotHasSignificantChange(
            Robot robot,
            IsarRobotInfoMessage isarRobotInfo
        )
        {
            if (robot.Host != isarRobotInfo.Host || robot.Port != isarRobotInfo.Port)
                return true;

            var existingVideoStreams = new List<string>();
            foreach (var existingVideoStream in robot.VideoStreams)
                existingVideoStreams.Add(existingVideoStream.Name);

            foreach (var videoStreamQuery in isarRobotInfo.VideoStreamQueries)
            {
                if (!existingVideoStreams.Contains(videoStreamQuery.Name))
                    return true;

                foreach (var existingVideoStream in robot.VideoStreams)
                {
                    if (
                        videoStreamQuery.Name == existingVideoStream.Name
                        && videoStreamQuery.Url != existingVideoStream.Url
                    )
                        return true;
                }
            }

            return false;
        }

        private async void OnMissionUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
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
                    "Failed to parse mission status from MQTT message. Mission: '{id}' was not updated.",
                    isarMission.MissionId
                );
                return;
            }

            var flotillaMission = await MissionService.UpdateMissionStatusByIsarMissionId(
                isarMission.MissionId,
                status
            );

            if (flotillaMission is null)
                _logger.LogInformation(
                    "{time} - Mission '{id}' status updated to '{status}' for robot '{robot}'",
                    isarMission.Timestamp,
                    isarMission.MissionId,
                    isarMission.Status,
                    isarMission.RobotId
                );

            var robot = await RobotService.ReadByName(isarMission.RobotId);
            if (robot is null)
            {
                _logger.LogError(
                    "Could not find robot with name {id}. The robot status is not updated.",
                    isarMission.RobotId
                );
                return;
            }

            robot.Status = flotillaMission.IsCompleted ? RobotStatus.Available : RobotStatus.Busy;

            await RobotService.Update(robot);
            _logger.LogInformation(
                "Mission with ISAR mission id '{id}' is started by the robot '{name}'. Robot status set to '{status}'.",
                isarMission.MissionId,
                isarMission.RobotId,
                robot.Status
            );
        }

        private async void OnTaskUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var task = (IsarTaskMessage)mqttArgs.Message;
            IsarTaskStatus status;
            try
            {
                status = IsarTaskStatusMethods.FromString(task.Status);
            }
            catch (ArgumentException e)
            {
                _logger.LogError(
                    e,
                    "Failed to parse mission status from MQTT message. Report: {id} was not updated.",
                    task.MissionId
                );
                return;
            }

            bool success = await MissionService.UpdateTaskStatusByIsarTaskId(
                task.MissionId,
                task.TaskId,
                status
            );

            if (success)
                _logger.LogInformation(
                    "{time} - Task {id} updated to {status} for {robot}",
                    task.Timestamp,
                    task.TaskId,
                    task.Status,
                    task.RobotId
                );
        }

        private async void OnStepUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var step = (IsarStepMessage)mqttArgs.Message;
            IsarStep.IsarStepStatus status;
            try
            {
                status = IsarStep.StatusFromString(step.Status);
            }
            catch (ArgumentException e)
            {
                _logger.LogError(
                    e,
                    "Failed to parse mission status from MQTT message. Report: {id} was not updated.",
                    step.MissionId
                );
                return;
            }

            bool success = await MissionService.UpdateStepStatusByIsarStepId(
                step.MissionId,
                step.TaskId,
                step.StepId,
                status
            );

            if (success)
                _logger.LogInformation(
                    "{time} - Step {id} updated to {status} for {robot}",
                    step.Timestamp,
                    step.StepId,
                    step.Status,
                    step.RobotId
                );
        }

        private async void OnBatteryUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var batteryStatus = (IsarBatteryMessage)mqttArgs.Message;
            var robot = await RobotService.ReadByName(batteryStatus.RobotId);
            if (robot == null)
            {
                _logger.LogWarning(
                    "Could not find corresponding robot for battery update with ID {id} ",
                    batteryStatus.RobotId
                );
            }
            else
            {
                robot.BatteryLevel = batteryStatus.BatteryLevel;
                await RobotService.Update(robot);
                _logger.LogDebug("Updated battery on robot {name} ", robot.Name);
            }
        }

        private async void OnPoseUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var poseStatus = (IsarPoseMessage)mqttArgs.Message;
            var robot = await RobotService.ReadByName(poseStatus.RobotId);
            if (robot == null)
            {
                _logger.LogWarning(
                    "Could not find corresponding robot for pose update with ID {id} ",
                    poseStatus.RobotId
                );
            }
            else
            {
                poseStatus.Pose.CopyIsarPoseToRobotPose(robot.Pose);
                await RobotService.Update(robot);
                _logger.LogDebug("Updated pose on robot {name} ", robot.Name);
            }
        }
    }
}
