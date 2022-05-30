using Api.Database.Models;
using Api.Services;
using Api.Utilities;

namespace Api.EventHandlers
{
    public class ScheduledMissionEventHandler : BackgroundService
    {
        private readonly ILogger<ScheduledMissionEventHandler> _logger;
        private int _timeDelay;
        private ScheduledMission? _nextScheduledMission;
        private readonly IsarService _isarService;
        private readonly RobotService _robotService;
        private readonly ScheduledMissionService _scheduledMissionService;

        public ScheduledMissionEventHandler(ILogger<ScheduledMissionEventHandler> logger, IServiceScopeFactory factory)
        {
            _logger = logger;
            ScheduledMissionService.ScheduledMissionUpdated += OnScheduledMissionUpdated;

            _timeDelay = 1000; // 1 second
            _upcomingScheduledMissions = new List<ScheduledMission>();
            _isarService = factory.CreateScope().ServiceProvider.GetRequiredService<IsarService>();
            _robotService = factory.CreateScope().ServiceProvider.GetRequiredService<RobotService>();
            _scheduledMissionService = factory.CreateScope().ServiceProvider.GetRequiredService<ScheduledMissionService>();
            UpdateUpcomingMission();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_nextScheduledMission == null)
                {
                    _logger.LogInformation("There are no upcoming scheduled missions.");
                    await Task.Delay(_timeDelay, stoppingToken);
                    continue;
                }

                if (_nextScheduledMission.StartTime < DateTimeOffset.UtcNow)
                {
                    var startedSuccessfull = await StartScheduledMission(_nextScheduledMission);
                    if (startedSuccessfull) { UpdateUpcomingMission(); }
                }
                else
                {
                    _logger.LogInformation($"The event is not ready to start.");
                }
                await Task.Delay(_timeDelay, stoppingToken);
            }
        }

        private void OnScheduledMissionUpdated(object? sender, EventArgs eventArgs)
        {
            UpdateUpcomingMission();
        }

        private async void UpdateUpcomingMission()
        {
            _nextScheduledMission = await _scheduledMissionService.NextPendingScheduledMission();
            if (_nextScheduledMission is null)
                return;

            _logger.LogInformation($"ScheduledMission {_nextScheduledMission.Id} is the next mission!");
        }

        private async Task<Boolean> StartScheduledMission(ScheduledMission scheduledMission)
        {
            try
            {
                var robot = await _robotService.Read(scheduledMission.Robot.Id);
                if (robot == null) return false;
                if (robot.Status is not RobotStatus.Available) return false;
            }
            catch (ArgumentNullException)
            {
                return false;
            }
            try
            {
                var report = await _isarService.StartMission(robot: scheduledMission.Robot, missionId: scheduledMission.IsarMissionId);
            }
            catch (MissionException)
            {
                return false;
            }
            scheduledMission.Status = ScheduledMissionStatus.Started;
            _scheduledMissionService.Update(scheduledMission);
            return true;
        }
    }
}
