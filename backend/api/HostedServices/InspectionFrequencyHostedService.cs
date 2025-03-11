using System;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.MissionLoaders;
using Api.Utilities;
using Hangfire;

namespace Api.HostedServices
{
    public class AutoSchedulingHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<AutoSchedulingHostedService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer? _timer = null;

        public AutoSchedulingHostedService(
            ILogger<AutoSchedulingHostedService> logger,
            IServiceScopeFactory scopeFactory
        )
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        private IMissionDefinitionService MissionDefinitionService =>
            _scopeFactory
                .CreateScope()
                .ServiceProvider.GetRequiredService<IMissionDefinitionService>();

        private IMissionSchedulingService MissionSchedulingService =>
            _scopeFactory
                .CreateScope()
                .ServiceProvider.GetRequiredService<IMissionSchedulingService>();

        private IRobotService RobotService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IRobotService>();

        private IMissionLoader MissionLoader =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionLoader>();

        private ISignalRService SignalRService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ISignalRService>();

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Auto Scheduling Hosted Service Running.");

            var timeUntilMidnight = (
                DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow
            ).TotalSeconds;
            _timer = new Timer(
                DoWork,
                null,
                TimeSpan.FromSeconds(timeUntilMidnight),
                TimeSpan.FromDays(1)
            );

            DoWork(null);
            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            var missionQuery = new MissionDefinitionQueryStringParameters();

            List<MissionDefinition>? missionDefinitions;
            try
            {
                missionDefinitions =
                    await MissionDefinitionService.ReadByHasAutoScheduleFrequency();
            }
            catch (InvalidDataException e)
            {
                _logger.LogError(e, "{ErrorMessage}", e.Message);
                return;
            }

            if (missionDefinitions == null)
            {
                _logger.LogInformation("No mission definitions with auto scheduling found.");
                return;
            }

            var selectedMissionDefinitions = missionDefinitions.Where(m =>
                m.AutoScheduleFrequency != null
                && m.AutoScheduleFrequency.GetSchedulingTimesUntilMidnight() != null
            );

            if (selectedMissionDefinitions.Any() == false)
            {
                _logger.LogInformation(
                    "No mission definitions with auto scheduling found that are due for inspection today."
                );
                return;
            }

            foreach (var missionDefinition in selectedMissionDefinitions)
            {
                if (missionDefinition.LastSuccessfulRun == null)
                {
                    string message =
                        $"Mission definition with Id {missionDefinition.Id} does not have a last successful mission run.";
                    ReportMessageToSignalR(
                        message,
                        missionDefinition.Id,
                        missionDefinition.InstallationCode
                    );
                    continue;
                }

                var jobDelays =
                    missionDefinition.AutoScheduleFrequency!.GetSchedulingTimesUntilMidnight();

                if (jobDelays == null)
                {
                    _logger.LogWarning(
                        "No job schedules found for mission definition {MissionDefinitionId}.",
                        missionDefinition.Id
                    );
                    return;
                }

                foreach (var jobDelay in jobDelays)
                {
                    _logger.LogInformation(
                        "Scheduling mission run for mission definition {MissionDefinitionId} in {TimeLeft}.",
                        missionDefinition.Id,
                        jobDelay
                    );
                    BackgroundJob.Schedule(
                        () => AutomaticScheduleMissionRun(missionDefinition),
                        jobDelay
                    );
                }
            }
        }

        public async Task AutomaticScheduleMissionRun(MissionDefinition missionDefinition)
        {
            _logger.LogInformation(
                "Scheduling mission run for mission definition {MissionDefinitionId}.",
                missionDefinition.Id
            );

            if (missionDefinition.InspectionArea == null)
            {
                string message =
                    $"Mission definition {missionDefinition.Id} has no inspection area.";
                ReportMessageToSignalR(
                    message,
                    missionDefinition.Id,
                    missionDefinition.InstallationCode
                );
                return;
            }

            IList<Robot> robots;
            try
            {
                robots = await RobotService.ReadRobotsForInstallation(
                    missionDefinition.InstallationCode
                );
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{ErrorMessage}", e.Message);
                return;
            }

            if (robots == null)
            {
                string message =
                    $"No robots found for installation code {missionDefinition.InstallationCode}.";
                ReportMessageToSignalR(
                    message,
                    missionDefinition.Id,
                    missionDefinition.InstallationCode
                );
                return;
            }

            var robot = robots.FirstOrDefault(r =>
                r.CurrentInspectionArea?.Id == missionDefinition.InspectionArea.Id
            );
            if (robot == null)
            {
                string message =
                    $"No robot found for mission definition {missionDefinition.Id} and inspection area {missionDefinition.InspectionArea.Id}.";
                ReportMessageToSignalR(
                    message,
                    missionDefinition.Id,
                    missionDefinition.InstallationCode
                );

                return;
            }

            _logger.LogInformation(
                "Scheduling mission run for mission definition {MissionDefinitionId} and robot {RobotId}.",
                missionDefinition.Id,
                robot.Id
            );

            try
            {
                await MissionSchedulingService.ScheduleMissionRunFromMissionDefinitionLastSuccessfullRun(
                    missionDefinition.Id,
                    robot.Id
                );
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{ErrorMessage}", e.Message);
                return;
            }

            return;
        }

        private void ReportMessageToSignalR(
            string message,
            string missionDefinitionId,
            string installationCode
        )
        {
            _logger.LogError(message);

            SignalRService.ReportAutoScheduleFailToSignalR(
                missionDefinitionId,
                message,
                installationCode
            );
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Auto Scheduling Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
