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

        private IReportService ReportService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IReportService>();
        private IRobotService RobotService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IRobotService>();
        private IScheduledMissionService ScheduledMissionService =>
            _scopeFactory
                .CreateScope()
                .ServiceProvider.GetRequiredService<IScheduledMissionService>();

        public MqttEventHandler(ILogger<MqttEventHandler> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;

            // Reason for using factory: https://www.thecodebuzz.com/using-dbcontext-instance-in-ihostedservice/
            _scopeFactory = scopeFactory;

            Subscribe();
        }

        public override void Subscribe()
        {
            MqttService.MqttIsarConnectReceived += OnIsarConnect;
            MqttService.MqttIsarMissionReceived += OnMissionUpdate;
            MqttService.MqttIsarTaskReceived += OnTaskUpdate;
            MqttService.MqttIsarStepReceived += OnStepUpdate;
            MqttService.MqttIsarBatteryReceived += OnBatteryUpdate;
            MqttService.MqttIsarPoseReceived += OnPoseUpdate;
        }

        public override void Unsubscribe()
        {
            MqttService.MqttIsarConnectReceived -= OnIsarConnect;
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

        private async void OnIsarConnect(object? sender, MqttReceivedArgs mqttArgs)
        {
            var isarRobot = (IsarConnectMessage)mqttArgs.Message;
            var robot = await RobotService.ReadByName(isarRobot.RobotId);
            if (robot == null)
            {
                _logger.LogError(
                    "ISAR instance for robot {name} connected. Could not find robot {name} in the database.",
                    isarRobot.RobotId,
                    isarRobot.RobotId
                );
                return;
            }
            else
            {
                robot.Host = isarRobot.Host;
                robot.Port = isarRobot.Port;
                robot.Enabled = true;
                await RobotService.Update(robot);
                _logger.LogInformation(
                    "ISAR instance for robot {name} with id {id} is connected. Robot is enabled and host ({host}) and port ({port}) is updated.",
                    isarRobot.RobotId,
                    robot.Id,
                    isarRobot.Host,
                    isarRobot.Port
                );
            }
        }

        private async void OnMissionUpdate(object? sender, MqttReceivedArgs mqttArgs)
        {
            var mission = (IsarMissionMessage)mqttArgs.Message;
            ReportStatus status;
            try
            {
                status = ReportStatusMethods.FromString(mission.Status);
            }
            catch (ArgumentException e)
            {
                _logger.LogError(
                    e,
                    "Failed to parse report status from MQTT message. Report: {id} was not updated.",
                    mission.MissionId
                );
                return;
            }

            bool success = await ReportService.UpdateMissionStatus(mission.MissionId, status);

            if (success)
                _logger.LogInformation(
                    "{time} - Mission {id} updated to {status} for {robot}",
                    mission.Timestamp,
                    mission.MissionId,
                    mission.Status,
                    mission.RobotId
                );

            var robot = await RobotService.ReadByName(mission.RobotId);
            if (robot == null)
            {
                _logger.LogError(
                    "Could not find robot with name {id}. The robot status is not updated.",
                    mission.RobotId
                );
            }
            else if (mission.Status.Equals("in_progress", StringComparison.OrdinalIgnoreCase))
            {
                robot.Status = RobotStatus.Busy;
                await RobotService.Update(robot);
                _logger.LogInformation(
                    "Mission with ISAR mission id {id} is started by the robot {name}. Robot status set to Busy.",
                    mission.MissionId,
                    mission.RobotId
                );
            }
            else if (mission.Status.Equals("completed", StringComparison.OrdinalIgnoreCase))
            {
                robot.Status = RobotStatus.Available;
                await RobotService.Update(robot);
                _logger.LogInformation(
                    "Mission with ISAR mission id {id} is completed by the robot {name}. Robot status set to Available.",
                    mission.MissionId,
                    mission.RobotId
                );

                var scheduledMissions = await ScheduledMissionService.ReadByStatus(
                    ScheduledMissionStatus.Ongoing
                );
                if (scheduledMissions is not null)
                {
                    foreach (var sm in scheduledMissions)
                    {
                        if (sm.Robot.Name == robot.Name)
                        {
                            await ScheduledMissionService.Delete(sm.Id);
                            _logger.LogInformation(
                                "Mission with ISAR mission id {id} is completed by the robot {name}. Matching scheduledMission with id {id} is deleted.",
                                mission.MissionId,
                                mission.RobotId,
                                sm.Id
                            );
                        }
                    }
                }
            }
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
                    "Failed to parse report status from MQTT message. Report: {id} was not updated.",
                    task.MissionId
                );
                return;
            }

            bool success = await ReportService.UpdateTaskStatus(task.TaskId, status);

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
                    "Failed to parse report status from MQTT message. Report: {id} was not updated.",
                    step.MissionId
                );
                return;
            }

            bool success = await ReportService.UpdateStepStatus(step.StepId, status);

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
                _logger.LogInformation("Updated battery on robot {name} ", robot.Name);
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
                _logger.LogInformation("Updated pose on robot {name} ", robot.Name);
            }
        }
    }
}
