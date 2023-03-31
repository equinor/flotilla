using Api.Controllers;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Api.EventHandlers
{
    public class MissionScheduler : BackgroundService
    {
        private readonly ILogger<MissionScheduler> _logger;
        private readonly int _timeDelay;
        private readonly IServiceScopeFactory _scopeFactory;

        private IList<Mission> MissionQueue =>
            MissionService
                .ReadAll(
                    new MissionQueryStringParameters
                    {
                        Status = MissionStatus.Pending,
                        OrderBy = "DesiredStartTime desc",
                        PageSize = 100
                    }
                )
                .Result;

        private IMissionService MissionService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionService>();

        private RobotController RobotController =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<RobotController>();

        public MissionScheduler(ILogger<MissionScheduler> logger, IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _timeDelay = 1000; // 1 second
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var queuedMission in MissionQueue)
                {
                    var freshMission = MissionService.ReadById(queuedMission.Id).Result;
                    if (freshMission == null)
                    {
                        continue;
                    }
                    if (
                        freshMission.Robot.Status is not RobotStatus.Available
                        || freshMission.DesiredStartTime > DateTimeOffset.UtcNow
                    )
                    {
                        continue;
                    }

                    try
                    {
                        await StartMission(queuedMission);
                    }
                    catch (MissionException e)
                    {
                        const MissionStatus NewStatus = MissionStatus.Failed;
                        _logger.LogWarning(
                            "Mission {id} was not started successfully. Status updated to '{status}'.\nReason: {failReason}",
                            queuedMission.Id,
                            NewStatus,
                            e.Message
                        );
                        queuedMission.Status = NewStatus;
                        queuedMission.StatusReason = $"Failed to start: '{e.Message}'";
                        await MissionService.Update(queuedMission);
                    }
                }
                await Task.Delay(_timeDelay, stoppingToken);
            }
        }

        private async Task StartMission(Mission queuedMission)
        {
            var result = await RobotController.StartMission(
                queuedMission.Robot.Id,
                queuedMission.Id
            );
            if (result.Result is not OkObjectResult)
            {
                string errorMessage = "Unknown error from robot controller";
                if (result.Result is ObjectResult returnObject)
                    errorMessage = returnObject.Value?.ToString() ?? errorMessage;
                throw new MissionException(errorMessage);
            }
            _logger.LogInformation("Started mission '{id}'", queuedMission.Id);
        }
    }
}
