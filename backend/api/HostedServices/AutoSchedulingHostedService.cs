using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Utilities;

namespace Api.HostedServices
{
    public class AutoSchedulingHostedService(
        ILogger<AutoSchedulingHostedService> logger,
        IServiceScopeFactory scopeFactory
    ) : IHostedService, IDisposable
    {
        private readonly ILogger<AutoSchedulingHostedService> _logger = logger;
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private Timer? _timer;

        private IMissionDefinitionService MissionDefinitionService =>
            _scopeFactory
                .CreateScope()
                .ServiceProvider.GetRequiredService<IMissionDefinitionService>();

        private IAutoScheduleService AutoScheduleService =>
            _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IAutoScheduleService>();

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Auto Scheduling Hosted Service Running.");

            _timer = new Timer(_ => _ = RunAndReschedule());
            _ = RunAndReschedule();
            return Task.CompletedTask;
        }

        // In-memory Hangfire loses scheduled jobs on restart, so every run rebuilds today's schedule.
        private async Task RunAndReschedule()
        {
            try
            {
                await DoWork();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto scheduling run failed.");
            }
            finally
            {
                _timer?.Change(TimeUntilNextCetMidnight(), Timeout.InfiniteTimeSpan);
            }
        }

        // Re-armed each run so the daily tick stays aligned to CET midnight across DST changes.
        private static TimeSpan TimeUntilNextCetMidnight()
        {
            DateOnly tomorrowCet = DateOnly.FromDateTime(TimeZoneUtilities.NowCet()).AddDays(1);
            DateTimeOffset nextMidnightUtc = TimeZoneUtilities.CetWallClockToUtcInstant(
                tomorrowCet,
                new TimeOnly(0, 0)
            );
            var untilMidnight = nextMidnightUtc - DateTimeOffset.UtcNow;
            return untilMidnight > TimeSpan.Zero ? untilMidnight : TimeSpan.Zero;
        }

        public async Task<IList<(DateTimeOffset, TimeOnly)>?> TestableDoWork()
        {
            return await DoWork(false);
        }

        private async Task<IList<(DateTimeOffset, TimeOnly)>?> DoWork(bool? scheduleJobs = true)
        {
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

            // GetSchedulingTimesUntilMidnight() returns null once all of today's times have passed.
            var selectedMissionDefinitions = missionDefinitions
                .Where(m => m.AutoScheduleFrequency?.GetSchedulingTimesUntilMidnight() != null)
                .ToList();

            if (selectedMissionDefinitions.Count == 0)
            {
                _logger.LogInformation(
                    "No auto scheduled missions have remaining scheduling times today."
                );
                return null;
            }

            List<(DateTimeOffset, TimeOnly)>? jobDelays = null;
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
