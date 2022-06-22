using Api.Database.Models;
using Api.Services;
using Api.Utilities;

namespace Api.EventHandlers
{
    public class ScheduledMissionEventHandler : BackgroundService
    {
        private readonly ILogger<ScheduledMissionEventHandler> _logger;
        private readonly int _timeDelay;
        private List<ScheduledMission>? _upcomingScheduledMissions;
        private readonly IsarService _isarService;
        private readonly RobotService _robotService;
        private readonly ScheduledMissionService _scheduledMissionService;

        public ScheduledMissionEventHandler(ILogger<ScheduledMissionEventHandler> logger, IServiceScopeFactory factory)
        {
            _logger = logger;
            ScheduledMissionService.ScheduledMissionUpdated += OnScheduledMissionUpdated;

            _timeDelay = 1000; // 1 second
            _isarService = factory.CreateScope().ServiceProvider.GetRequiredService<IsarService>();
            _robotService = factory.CreateScope().ServiceProvider.GetRequiredService<RobotService>();
            _scheduledMissionService = factory.CreateScope().ServiceProvider.GetRequiredService<ScheduledMissionService>();
            UpdateUpcomingScheduledMissions();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_upcomingScheduledMissions is not null)
                {
                    foreach (var upcomingScheduledMission in _upcomingScheduledMissions)
                    {
                        if (upcomingScheduledMission.Robot.Status is not RobotStatus.Available) { continue; }
                        if (upcomingScheduledMission.StartTime > DateTimeOffset.UtcNow) { break; }

                        bool startedSuccessfull = await StartScheduledMission(upcomingScheduledMission);
                        if (startedSuccessfull) { UpdateUpcomingScheduledMissions(); }
                        else { _logger.LogWarning("Mission {id} was not started successfully.", upcomingScheduledMission.Id); };
                    }
                }
                await Task.Delay(_timeDelay, stoppingToken);
            }
        }

        private void OnScheduledMissionUpdated(object? sender, EventArgs eventArgs)
        {
            UpdateUpcomingScheduledMissions();
        }

        private async void UpdateUpcomingScheduledMissions()
        {
            _upcomingScheduledMissions = await _scheduledMissionService.GetUpcomingScheduledMissions();
        }

        private async Task<bool> StartScheduledMission(ScheduledMission scheduledMission)
        {
            var robot = await _robotService.Read(scheduledMission.Robot.Id);
            if (robot is null)
            {
                _logger.LogWarning("Could not find robot {id}", scheduledMission.Robot.Id);
                return false;
            }
            if (robot.Status is not RobotStatus.Available)
            {
                _logger.LogWarning("Robot {id} is not available", scheduledMission.Robot.Id);
                return false;
            }
            try
            {
                var report = await _isarService.StartMission(robot: scheduledMission.Robot, missionId: scheduledMission.IsarMissionId);
                _logger.LogInformation("Started mission {id}", scheduledMission.Id);
            }
            catch (MissionException e)
            {
                _logger.LogError(e, "Failed to start mission {id}", scheduledMission.Id);
                return false;
            }
            scheduledMission.Status = ScheduledMissionStatus.Started;
            _scheduledMissionService.Update(scheduledMission);
            return true;
        }
    }
}
