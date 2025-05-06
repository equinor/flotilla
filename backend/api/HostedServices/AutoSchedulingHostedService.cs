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

        private IMissionRunService MissionRunService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionRunService>();

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
                PrivateDoWork,
                null,
                TimeSpan.FromSeconds(timeUntilMidnight),
                TimeSpan.FromDays(1)
            );

            PrivateDoWork(null);
            return Task.CompletedTask;
        }

        private async void PrivateDoWork(object? state)
        {
            await DoWork();
        }

        public async Task<IList<TimeSpan>?> TestableDoWork()
        {
            return await DoWork(false);
        }

        private async Task<IList<TimeSpan>?> DoWork(bool? scheduleJobs = true)
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
                return null;
            }

            if (missionDefinitions == null)
            {
                _logger.LogInformation("No mission definitions with auto scheduling found.");
                return null;
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
                return null;
            }

            var jobDelays = new List<TimeSpan>();
            foreach (var missionDefinition in selectedMissionDefinitions)
            {
                jobDelays = missionDefinition
                    .AutoScheduleFrequency!.GetSchedulingTimesUntilMidnight()
                    ?.ToList();

                if (jobDelays == null)
                {
                    _logger.LogWarning(
                        "No job schedules found for mission definition {MissionDefinitionId}.",
                        missionDefinition.Id
                    );
                    return null;
                }

                if (scheduleJobs == false)
                {
                    continue;
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

            return jobDelays;
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
                r.CurrentInspectionAreaId == missionDefinition.InspectionArea.Id
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
                var missionTasks = await MissionLoader.GetTasksForMission(
                    missionDefinition.Source.SourceId
                );
                if (missionTasks == null)
                {
                    _logger.LogError(
                        "No mission tasks were found for mission definition {MissionDefinitionId}.",
                        missionDefinition.Id
                    );
                    return;
                }

                var missionRun = new MissionRun
                {
                    Name = missionDefinition.Name,
                    Robot = robot,
                    MissionId = missionDefinition.Id,
                    Status = MissionStatus.Pending,
                    MissionRunType = MissionRunType.Normal,
                    DesiredStartTime = DateTime.UtcNow,
                    Tasks = missionTasks,
                    InstallationCode = missionDefinition.InstallationCode,
                    InspectionArea = missionDefinition.InspectionArea,
                };

                if (missionRun.Tasks.Any())
                {
                    missionRun.SetEstimatedTaskDuration();
                }

                await MissionRunService.Create(missionRun);
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
