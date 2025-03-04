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
                    _logger.LogInformation(
                        "Mission definition with Id {MissionDefinitionId} does not have a last successfull mission run.",
                        missionDefinition.Id
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
                _logger.LogWarning(
                    "Mission definition {MissionDefinitionId} has no inspection area.",
                    missionDefinition.Id
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
                _logger.LogInformation(
                    "No robots found for installation code {InstallationCode}.",
                    missionDefinition.InstallationCode
                );
                return;
            }

            var robot = robots.FirstOrDefault(r =>
                r.CurrentInspectionArea?.Id == missionDefinition.InspectionArea.Id
            );
            if (robot == null)
            {
                _logger.LogWarning(
                    "No robot found for mission definition {MissionDefinitionId} and inspection area {InspectionAreaId}.",
                    missionDefinition.Id,
                    missionDefinition.InspectionArea.Id
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
