using Api.Models;
using Api.Services;
using Api.Utilities;
namespace Api.Scheduler
{
    public class EventScheduler : BackgroundService
    {
        private readonly ILogger<EventScheduler> _logger;
        private readonly int _timeDelay;
        private readonly EventService _eventService;
        private readonly IsarService _isarService;
        private readonly RobotService _robotService;

        public EventScheduler(ILogger<EventScheduler> logger, IServiceScopeFactory factory)
        {
            _logger = logger;
            _timeDelay = 1000; // 1 second
            _eventService = factory.CreateScope().ServiceProvider.GetRequiredService<EventService>();
            _isarService = factory.CreateScope().ServiceProvider.GetRequiredService<IsarService>();
            _robotService = factory.CreateScope().ServiceProvider.GetRequiredService<RobotService>();
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var evnt = await _eventService.NextPendingEvent();
                if (evnt == null)
                {
                    _logger.LogInformation("There are no upcoming Pending events.");
                    await Task.Delay(_timeDelay, stoppingToken);
                    continue;
                }
                if (evnt.StartTime < DateTimeOffset.UtcNow)
                {
                    var startedSuccessfull = await _startEvent(evnt);
                }
                else
                {
                    _logger.LogInformation($"The event is not ready to start.");
                }
                await Task.Delay(_timeDelay, stoppingToken);
            }
        }

        private async Task<Boolean> _startEvent(Event evnt)
        {
            var robot = await _robotService.Read(evnt.Robot.Id);
            if (robot == null) return false;
            try
            {
                // var report = await _isarService.StartMission(robot: evnt.Robot, missionId: evnt.IsarMissionId);
            }
            catch (MissionException)
            {
                return false;
            }
            _logger.LogInformation($"Event {evnt.Id} started!");
            return true;
        }
    }
}
