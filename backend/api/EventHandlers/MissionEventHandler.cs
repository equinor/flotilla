using Api.Controllers;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.Events;
using Api.Utilities;
using Microsoft.AspNetCore.Mvc;
namespace Api.EventHandlers
{
    public class MissionEventHandler : EventHandlerBase
    {
        private readonly ILogger<MissionEventHandler> _logger;

        // The mutex is used to ensure multiple missions aren't attempted scheduled simultaneously whenever multiple mission runs are created
        private readonly Mutex _scheduleMissionMutex = new();
        private readonly IServiceScopeFactory _scopeFactory;

        public MissionEventHandler(
            ILogger<MissionEventHandler> logger,
            IServiceScopeFactory scopeFactory
        )
        {
            _logger = logger;
            _scopeFactory = scopeFactory;

            Subscribe();
        }

        private IList<MissionRun> MissionRunQueue =>
            MissionService
                .ReadAll(
                    new MissionRunQueryStringParameters
                    {
                        Statuses = new List<MissionStatus>
                        {
                            MissionStatus.Pending
                        },
                        OrderBy = "DesiredStartTime",
                        PageSize = 100
                    }
                )
                .Result;

        private IMissionRunService MissionService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionRunService>();

        private IRobotService RobotService => _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IRobotService>();

        private RobotController RobotController =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<RobotController>();

        public override void Subscribe()
        {
            MissionRunService.MissionRunCreated += OnMissionRunCreated;
            MqttEventHandler.RobotAvailable += OnRobotAvailable;
        }

        public override void Unsubscribe()
        {
            MissionRunService.MissionRunCreated -= OnMissionRunCreated;
            MqttEventHandler.RobotAvailable -= OnRobotAvailable;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await stoppingToken;
        }

        private void OnMissionRunCreated(object? sender, MissionRunCreatedEventArgs e)
        {
            _logger.LogInformation("Triggered MissionRunCreated event for mission run ID: {MissionRunId}", e.MissionRunId);

            if (MissionRunQueueIsEmpty())
            {
                _logger.LogInformation("Mission run {MissionRunId} was not started as there are no mission runs on the queue", e.MissionRunId);
                return;
            }

            var missionRun = MissionService.ReadById(e.MissionRunId).Result;

            if (missionRun == null)
            {
                _logger.LogError("Mission run with ID: {MissionRunId} was not found in the database", e.MissionRunId);
                return;
            }

            _scheduleMissionMutex.WaitOne();
            StartMissionRunIfSystemIsAvailable(missionRun);
            _scheduleMissionMutex.ReleaseMutex();
        }

        private async void OnRobotAvailable(object? sender, RobotAvailableEventArgs e)
        {
            _logger.LogInformation("Triggered RobotAvailable event for robot ID: {RobotId}", e.RobotId);
            var robot = await RobotService.ReadById(e.RobotId);
            if (robot == null)
            {
                _logger.LogError("Robot with ID: {RobotId} was not found in the database", e.RobotId);
                return;
            }

            if (MissionRunQueueIsEmpty())
            {
                _logger.LogInformation("The robot was changed to available but there are no mission runs in the queue to be scheduled");
                return;
            }

            var missionRun = MissionRunQueue.First(missionRun => missionRun.Robot.Id == robot.Id);

            _scheduleMissionMutex.WaitOne();
            StartMissionRunIfSystemIsAvailable(missionRun);
            _scheduleMissionMutex.ReleaseMutex();
        }

        private void StartMissionRunIfSystemIsAvailable(MissionRun missionRun)
        {
            if (!TheSystemIsAvailableToRunAMission(missionRun.Robot, missionRun).Result)
            {
                _logger.LogInformation("Mission {MissionRunId} was put on the queue as the system may not start a mission now", missionRun.Id);
                return;
            }

            try
            {
                StartMissionRun(missionRun);
            }
            catch (MissionException ex)
            {
                const MissionStatus NewStatus = MissionStatus.Failed;
                _logger.LogWarning(
                    "Mission run {MissionRunId} was not started successfully. Status updated to '{Status}'.\nReason: {FailReason}",
                    missionRun.Id,
                    NewStatus,
                    ex.Message
                );
                missionRun.Status = NewStatus;
                missionRun.StatusReason = $"Failed to start: '{ex.Message}'";
                MissionService.Update(missionRun);
            }
        }

        private bool MissionRunQueueIsEmpty()
        {
            return !MissionRunQueue.Any();
        }

        private async Task<bool> TheSystemIsAvailableToRunAMission(Robot robot, MissionRun missionRun)
        {
            bool ongoingMission = await OngoingMission();

            if (ongoingMission)
            {
                _logger.LogInformation("Mission run {MissionRunId} was not started as there is already an ongoing mission", missionRun.Id);
                return false;
            }
            if (robot.Status is not RobotStatus.Available)
            {
                _logger.LogInformation("Mission run {MissionRunId} was not started as the robot is not available", missionRun.Id);
                return false;
            }
            if (!robot.Enabled)
            {
                _logger.LogWarning("Mission run {MissionRunId} was not started as the robot {RobotId} is not enabled", missionRun.Id, robot.Id);
                return false;
            }
            if (missionRun.DesiredStartTime > DateTimeOffset.UtcNow)
            {
                _logger.LogInformation("Mission run {MissionRunId} was not started as the start time is in the future", missionRun.Id);
                return false;
            }
            return true;
        }

        private async Task<bool> OngoingMission()
        {
            var ongoingMissions = await MissionService.ReadAll(
                new MissionRunQueryStringParameters
                {
                    Statuses = new List<MissionStatus>
                    {
                        MissionStatus.Ongoing
                    },
                    OrderBy = "DesiredStartTime",
                    PageSize = 100
                });

            return ongoingMissions.Any();
        }

        private void StartMissionRun(MissionRun queuedMissionRun)
        {
            var result = RobotController.StartMission(
                queuedMissionRun.Robot.Id,
                queuedMissionRun.Id
            ).Result;
            if (result.Result is not OkObjectResult)
            {
                string errorMessage = "Unknown error from robot controller";
                if (result.Result is ObjectResult returnObject)
                {
                    errorMessage = returnObject.Value?.ToString() ?? errorMessage;
                }
                throw new MissionException(errorMessage);
            }
            _logger.LogInformation("Started mission run '{Id}'", queuedMissionRun.Id);
        }
    }
}
