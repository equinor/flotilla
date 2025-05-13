using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.MissionLoaders;

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

        private IMissionRunService MissionRunService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionRunService>();

        private IMissionLoader MissionLoader =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMissionLoader>();

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

        public async Task<IList<(TimeSpan, TimeOnly)>?> TestableDoWork()
        {
            return await DoWork(false);
        }

        private async Task<IList<(TimeSpan, TimeOnly)>?> DoWork(bool? scheduleJobs = true)
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

            await ResetAutoScheduledJobsObjects(missionDefinitions);

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

            var jobDelays = new List<(TimeSpan, TimeOnly)>();
            foreach (var missionDefinition in selectedMissionDefinitions)
            {
                jobDelays = await AutoScheduleService.StartJobsForMissionDefinition(
                    missionDefinition,
                    scheduleJobs
                );
            }

            return jobDelays;
        }

        public async Task ResetAutoScheduledJobsObjects(List<MissionDefinition> missionDefinitions)
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
