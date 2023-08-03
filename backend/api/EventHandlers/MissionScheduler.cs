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
                        Statuses = new List<MissionStatus> { MissionStatus.Pending },
                        OrderBy = "DesiredStartTime",
                        PageSize = 100
                    }
                )
                .Result;

        private IMissionService MissionService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionService>();

        private RobotController RobotController =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<RobotController>();

        private IRobotService RobotService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IRobotService>();

        public MissionScheduler(
            ILogger<MissionScheduler> logger,
            IServiceScopeFactory scopeFactory
        )
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

                    var robot = await RobotService.ReadById(freshMission.Robot.Id);
                    if (robot == null)
                    {
                        continue;
                    }

                    if (
                        robot.Status is not RobotStatus.Available
                        || !freshMission.Robot.Enabled
                        || freshMission.DesiredStartTime > DateTimeOffset.UtcNow
                    )
                    {
                        continue;
                    }

                    if (MissionService.IsMissionSchedulerPaused)
                    {
                        continue;
                    }

                    try
                    {
                        var result = await StartMission(queuedMission);
                        Console.WriteLine(result);
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

        private async Task<ActionResult<Mission>> StartMission(Mission queuedMission)
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

            return result;
        }
    }
}
