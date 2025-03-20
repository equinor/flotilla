using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Hangfire;

namespace Api.HostedServices
{
    public class AutoSchedulingHostedService(
        ILogger<AutoSchedulingHostedService> logger,
        IServiceScopeFactory scopeFactory
    ) : IHostedService, IDisposable
    {
        private readonly ILogger<AutoSchedulingHostedService> _logger = logger;
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private Timer? _timer = null;

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

        private IAutoScheduleService AutoScheduleService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IAutoScheduleService>();

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Auto Scheduling Hosted Service Running.");

            var timeUntilMidnight = (
                DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow
            ).TotalSeconds;
            _timer = new Timer(DoWork, null, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(3));

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

            await DeletePreviousDayScheduledJobs(missionDefinitions);

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
                    AutoScheduleService.ReportAutoScheduleFailToSignalR(message, missionDefinition);
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
                    var existingJobs = AutoScheduleService.DeserializeAutoScheduleJobs(
                        missionDefinition,
                        _logger
                    );

                    if (existingJobs.ContainsKey(jobDelay.Item2))
                        continue;

                    _logger.LogInformation(
                        "Scheduling mission run for mission definition {MissionDefinitionId} in {TimeLeft}.",
                        missionDefinition.Id,
                        jobDelay
                    );

                    var jobId = BackgroundJob.Schedule(
                        () => AutoScheduleMissionRun(missionDefinition),
                        jobDelay.Item1
                    );

                    existingJobs.Add(jobDelay.Item2, jobId);

                    missionDefinition.AutoScheduleFrequency!.AutoScheduledJobs =
                        JsonSerializer.Serialize(existingJobs);
                    await MissionDefinitionService.Update(missionDefinition);
                }
            }
        }

        public async Task AutoScheduleMissionRun(MissionDefinition missionDefinition)
        {
            _logger.LogInformation(
                "Scheduling mission run for mission definition {MissionDefinitionId}.",
                missionDefinition.Id
            );

            if (missionDefinition.InspectionArea == null)
            {
                string message =
                    $"Mission definition {missionDefinition.Id} has no inspection area.";
                AutoScheduleService.ReportAutoScheduleFailToSignalR(message, missionDefinition);
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
                AutoScheduleService.ReportAutoScheduleFailToSignalR(message, missionDefinition);
                return;
            }

            var robot = robots.FirstOrDefault(r =>
                r.CurrentInspectionArea?.Id == missionDefinition.InspectionArea.Id
            );
            if (robot == null)
            {
                string message =
                    $"No robot found for mission definition {missionDefinition.Id} and inspection area {missionDefinition.InspectionArea.Id}.";
                AutoScheduleService.ReportAutoScheduleFailToSignalR(message, missionDefinition);

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

        public async Task DeletePreviousDayScheduledJobs(List<MissionDefinition> missionDefinitions)
        {
            foreach (var missionDefinition in missionDefinitions)
            {
                if (
                    missionDefinition.AutoScheduleFrequency == null
                    || missionDefinition.AutoScheduleFrequency.AutoScheduledJobs == null
                )
                    continue;

                missionDefinition.AutoScheduleFrequency.AutoScheduledJobs = null;
                await MissionDefinitionService.Update(missionDefinition);
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
