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

        private IList<MissionRun> MissionRunQueue =>
            MissionRunService
                .ReadAll(
                    new MissionRunQueryStringParameters
                    {
                        Statuses = new List<MissionStatus> { MissionStatus.Pending },
                        OrderBy = "DesiredStartTime",
                        PageSize = 100
                    }
                )
                .Result;

        private IMissionRunService MissionRunService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionRunService>();

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
                foreach (var queuedMissionRun in MissionRunQueue)
                {
                    var freshMissionRun = MissionRunService.ReadById(queuedMissionRun.Id).Result;
                    if (freshMissionRun == null)
                    {
                        continue;
                    }
                    if (
                        freshMissionRun.Robot.Status is not RobotStatus.Available
                        || !freshMissionRun.Robot.Enabled
                        || freshMissionRun.DesiredStartTime > DateTimeOffset.UtcNow
                    )
                    {
                        continue;
                    }

                    try
                    {
                        await StartMissionRun(queuedMissionRun);
                    }
                    catch (MissionException e)
                    {
                        const MissionStatus NewStatus = MissionStatus.Failed;
                        _logger.LogWarning(
                            "Mission run {id} was not started successfully. Status updated to '{status}'.\nReason: {failReason}",
                            queuedMissionRun.Id,
                            NewStatus,
                            e.Message
                        );
                        queuedMissionRun.Status = NewStatus;
                        queuedMissionRun.StatusReason = $"Failed to start: '{e.Message}'";
                        await MissionRunService.Update(queuedMissionRun);
                    }
                }
                await Task.Delay(_timeDelay, stoppingToken);
            }
        }

        private async Task StartMissionRun(MissionRun queuedMissionRun)
        {
            var result = await RobotController.StartMission(
                queuedMissionRun.Robot.Id,
                queuedMissionRun.Id
            );
            if (result.Result is not OkObjectResult)
            {
                string errorMessage = "Unknown error from robot controller";
                if (result.Result is ObjectResult returnObject)
                    errorMessage = returnObject.Value?.ToString() ?? errorMessage;
                throw new MissionException(errorMessage);
            }
            _logger.LogInformation("Started mission run '{id}'", queuedMissionRun.Id);
        }
    }
}
