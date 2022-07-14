using Api.Controllers;
using Api.Database.Models;
using Api.Services;
using Api.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Api.EventHandlers
{
    public class ScheduledMissionEventHandler : BackgroundService
    {
        private readonly ILogger<ScheduledMissionEventHandler> _logger;
        private readonly int _timeDelay;
        private List<ScheduledMission>? _upcomingScheduledMissions;
        private readonly IScheduledMissionService _scheduledMissionService;
        private readonly RobotController _robotController;

        public ScheduledMissionEventHandler(
            ILogger<ScheduledMissionEventHandler> logger,
            IServiceScopeFactory factory
        )
        {
            _logger = logger;
            ScheduledMissionService.ScheduledMissionUpdated += OnScheduledMissionUpdated;

            _timeDelay = 1000; // 1 second
            _scheduledMissionService = factory
                .CreateScope()
                .ServiceProvider.GetRequiredService<IScheduledMissionService>();
            _robotController = factory
                .CreateScope()
                .ServiceProvider.GetRequiredService<RobotController>();
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
                        if (upcomingScheduledMission.Robot.Status is not RobotStatus.Available)
                        {
                            continue;
                        }
                        if (upcomingScheduledMission.StartTime > DateTimeOffset.UtcNow)
                        {
                            break;
                        }

                        bool startedSuccessfull = await StartScheduledMission(
                            upcomingScheduledMission
                        );
                        if (startedSuccessfull)
                        {
                            UpdateUpcomingScheduledMissions();
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Mission {id} was not started successfully.",
                                upcomingScheduledMission.Id
                            );
                        }
                        ;
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
            _upcomingScheduledMissions = await _scheduledMissionService.ReadByStatus(ScheduledMissionStatus.Pending);
        }

        private async Task<bool> StartScheduledMission(ScheduledMission scheduledMission)
        {
            try
            {
                var result = await _robotController.StartMission(scheduledMission.Robot.Id, scheduledMission.EchoMissionId);
                if (result.Result is not OkObjectResult)
                {
                    throw new MissionException(result?.Result?.ToString() ?? "Unknown error from robot controller");
                }
                _logger.LogInformation("Started mission '{id}'", scheduledMission.Id);
            }
            catch (MissionException e)
            {
                _logger.LogError(e, "Failed to start mission '{id}'", scheduledMission.Id);
                return false;
            }
            scheduledMission.Status = ScheduledMissionStatus.Ongoing;
            _scheduledMissionService.Update(scheduledMission);
            return true;
        }
    }
}
