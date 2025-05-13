using System.Text.Json;
using Api.Database.Models;
using Api.Services.MissionLoaders;
using Hangfire;

namespace Api.Services
{
    public interface IAutoScheduleService
    {
        public Task<List<(TimeSpan, TimeOnly)>?> StartJobsForMissionDefinition(
            MissionDefinition missionDefinition,
            bool? scheduleJobs = true
        );

        public Task<AutoScheduleFrequency?> UpdateAutoScheduleFrequency(
            MissionDefinition missionDefinition,
            AutoScheduleFrequency? newAutoScheduleFrequency
        );

        public Task UpdateAutoMissionScheduledJobs(
            MissionDefinition missionDefinition,
            TimeOnly scheduledTimeInLocalTime
        );

        public Task SkipAutoMissionScheduledJob(
            MissionDefinition missionDefinition,
            TimeOnly scheduledTimeInLocalTime
        );

        public Task SkipAllAutoMissions(MissionDefinition missionDefinition);
        public Dictionary<TimeOnly, string> DeserializeAutoScheduleJobs(
            MissionDefinition missionDefinition
        );
        public void ReportSkipAutoScheduleToSignalR(
            string message,
            MissionDefinition missionDefinition
        );
        public void ReportAutoScheduleFailToSignalR(
            string message,
            MissionDefinition missionDefinition
        );
    }

    public class AutoScheduleService(
        ILogger<AutoScheduleService> logger,
        IMissionDefinitionService missionDefinitionService,
        IRobotService robotService,
        IMissionLoader missionLoader,
        IMissionRunService missionRunService,
        ISignalRService signalRService
    ) : IAutoScheduleService
    {
        public Dictionary<TimeOnly, string> DeserializeAutoScheduleJobs(
            MissionDefinition missionDefinition
        )
        {
            var existingJobs = new Dictionary<TimeOnly, string>();
            try
            {
                existingJobs =
                    JsonSerializer.Deserialize<Dictionary<TimeOnly, string>>(
                        missionDefinition.AutoScheduleFrequency?.AutoScheduledJobs ?? "{}"
                    ) ?? new();
            }
            catch (JsonException ex)
            {
                logger.LogError($"JSON deserialization failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError($"An unexpected error occurred: {ex.Message}");
            }

            return existingJobs;
        }

        public void ReportSkipAutoScheduleToSignalR(
            string message,
            MissionDefinition missionDefinition
        )
        {
            logger.LogInformation(message);
            signalRService.ReportAutoScheduleToSignalR(
                "skipAutoMission",
                missionDefinition.Name,
                message,
                missionDefinition.InstallationCode
            );
        }

        public void ReportAutoScheduleFailToSignalR(
            string message,
            MissionDefinition missionDefinition
        )
        {
            logger.LogError(message);

            signalRService.ReportAutoScheduleToSignalR(
                "AutoScheduleFail",
                missionDefinition.Name,
                message,
                missionDefinition.InstallationCode
            );
        }

        public async Task<List<(TimeSpan, TimeOnly)>?> StartJobsForMissionDefinition(
            MissionDefinition missionDefinition,
            bool? scheduleJobs = true
        )
        {
            if (missionDefinition.AutoScheduleFrequency is null)
                return null;

            var jobDelays = new List<(TimeSpan, TimeOnly)>();
            jobDelays = missionDefinition
                .AutoScheduleFrequency!.GetSchedulingTimesUntilMidnight()
                ?.ToList();

            if (jobDelays == null)
            {
                logger.LogWarning(
                    "No job schedules found for mission definition {MissionDefinitionId}.",
                    missionDefinition.Id
                );
                return null;
            }

            if (scheduleJobs == false)
            {
                return jobDelays;
            }

            foreach (var jobDelay in jobDelays)
            {
                var existingJobs = DeserializeAutoScheduleJobs(missionDefinition);

                if (existingJobs.ContainsKey(jobDelay.Item2))
                    continue;

                logger.LogInformation(
                    "Scheduling mission run for mission definition {MissionDefinitionId} in {TimeLeft}.",
                    missionDefinition.Id,
                    jobDelay
                );

                var jobId = BackgroundJob.Schedule(
                    () => AutoScheduleMissionRun(missionDefinition.Id, jobDelay.Item2),
                    jobDelay.Item1
                );

                existingJobs.Add(jobDelay.Item2, jobId);

                missionDefinition.AutoScheduleFrequency!.AutoScheduledJobs =
                    JsonSerializer.Serialize(existingJobs);
                await missionDefinitionService.Update(missionDefinition);
            }
            return jobDelays;
        }

        public async Task AutoScheduleMissionRun(string missionDefinitionId, TimeOnly timeOfDay)
        {
            logger.LogInformation(
                "Scheduling mission run for mission definition {MissionDefinitionId}.",
                missionDefinitionId
            );

            var missionDefinition = await missionDefinitionService.ReadById(missionDefinitionId);
            if (missionDefinition == null)
            {
                logger.LogError(
                    "Mission definition {MissionDefinitionId} not found.",
                    missionDefinitionId
                );
                return;
            }

            try
            {
                await UpdateAutoMissionScheduledJobs(missionDefinition, timeOfDay);
            }
            catch (Exception e)
            {
                logger.LogError(
                    e,
                    "Failed to update auto mission scheduled jobs for mission definition {MissionDefinitionId} at {TimeOfDay}.",
                    missionDefinition.Id,
                    timeOfDay
                );
            }

            if (missionDefinition.InspectionArea == null)
            {
                string message =
                    $"Mission definition {missionDefinition.Id} has no inspection area.";
                ReportAutoScheduleFailToSignalR(message, missionDefinition);
                return;
            }

            IList<Robot> robots;
            try
            {
                robots = await robotService.ReadRobotsForInstallation(
                    missionDefinition.InstallationCode
                );
            }
            catch (Exception e)
            {
                logger.LogError(e, "{ErrorMessage}", e.Message);
                return;
            }

            if (robots == null)
            {
                string message =
                    $"No robots found for installation code {missionDefinition.InstallationCode}.";
                ReportAutoScheduleFailToSignalR(message, missionDefinition);
                return;
            }

            var robot = robots.FirstOrDefault(r =>
                r.CurrentInspectionAreaId == missionDefinition.InspectionArea.Id
            );
            if (robot == null)
            {
                string message =
                    $"No robot found for mission definition {missionDefinition.Id} and inspection area {missionDefinition.InspectionArea.Id}.";
                ReportAutoScheduleFailToSignalR(message, missionDefinition);

                return;
            }

            logger.LogInformation(
                "Scheduling mission run for mission definition {MissionDefinitionId} and robot {RobotId}.",
                missionDefinition.Id,
                robot.Id
            );

            try
            {
                var missionTasks = await missionLoader.GetTasksForMission(
                    missionDefinition.Source.SourceId
                );
                if (missionTasks == null)
                {
                    logger.LogError(
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

                await missionRunService.Create(missionRun);
            }
            catch (Exception e)
            {
                logger.LogError(e, "{ErrorMessage}", e.Message);
                return;
            }
            return;
        }

        public async Task<AutoScheduleFrequency?> UpdateAutoScheduleFrequency(
            MissionDefinition missionDefinition,
            AutoScheduleFrequency? newAutoScheduleFrequency
        )
        {
            if (missionDefinition.AutoScheduleFrequency is null && newAutoScheduleFrequency is null)
            {
                return null;
            }
            if (
                missionDefinition.AutoScheduleFrequency is not null
                && newAutoScheduleFrequency is not null
            )
            {
                if (
                    missionDefinition.AutoScheduleFrequency.TimesOfDayCET.SequenceEqual(
                        newAutoScheduleFrequency.TimesOfDayCET
                    )
                    && missionDefinition.AutoScheduleFrequency.DaysOfWeek.SequenceEqual(
                        newAutoScheduleFrequency.DaysOfWeek
                    )
                )
                {
                    return missionDefinition.AutoScheduleFrequency;
                }
            }

            if (missionDefinition.AutoScheduleFrequency?.AutoScheduledJobs is not null)
            {
                await SkipAllAutoMissions(missionDefinition);
            }

            MissionDefinition updatedMissionDefinition;
            if (newAutoScheduleFrequency is null)
            {
                missionDefinition.AutoScheduleFrequency = null;
                updatedMissionDefinition = await missionDefinitionService.Update(missionDefinition);
                return updatedMissionDefinition.AutoScheduleFrequency;
            }
            missionDefinition.AutoScheduleFrequency ??= new AutoScheduleFrequency();

            missionDefinition.AutoScheduleFrequency.TimesOfDayCET =
                newAutoScheduleFrequency.TimesOfDayCET;
            missionDefinition.AutoScheduleFrequency.DaysOfWeek =
                newAutoScheduleFrequency.DaysOfWeek;

            await StartJobsForMissionDefinition(missionDefinition);
            updatedMissionDefinition = await missionDefinitionService.Update(missionDefinition);
            return updatedMissionDefinition.AutoScheduleFrequency;
        }

        public async Task UpdateAutoMissionScheduledJobs(
            MissionDefinition missionDefinition,
            TimeOnly scheduledTimeInLocalTime
        )
        {
            string message;

            var jobs = DeserializeAutoScheduleJobs(missionDefinition);

            if (missionDefinition.AutoScheduleFrequency == null || jobs.Count == 0)
            {
                message =
                    $"Mission definition {missionDefinition.Id} has no scheduled auto missions.";
                ReportAutoScheduleFailToSignalR(message, missionDefinition);
                return;
            }

            string? job;
            try
            {
                job = jobs[scheduledTimeInLocalTime];
            }
            catch (KeyNotFoundException)
            {
                message =
                    $"Mission definition {missionDefinition.Id} has no scheduled auto mission scheduled for {scheduledTimeInLocalTime}.";
                ReportAutoScheduleFailToSignalR(message, missionDefinition);
                return;
            }

            if (job == null || job == "")
            {
                message =
                    $"Mission definition {missionDefinition.Id} has no scheduled auto mission scheduled for {scheduledTimeInLocalTime}.";
                ReportAutoScheduleFailToSignalR(message, missionDefinition);
                return;
            }

            try
            {
                BackgroundJob.Delete(job);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to delete background job: {job}");
                return;
            }

            jobs.Remove(scheduledTimeInLocalTime);

            missionDefinition.AutoScheduleFrequency!.AutoScheduledJobs = JsonSerializer.Serialize(
                jobs
            );
            await missionDefinitionService.Update(missionDefinition);
            return;
        }

        public async Task SkipAutoMissionScheduledJob(
            MissionDefinition missionDefinition,
            TimeOnly scheduledTimeInLocalTime
        )
        {
            try
            {
                await UpdateAutoMissionScheduledJobs(missionDefinition, scheduledTimeInLocalTime);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    $"Failed to update auto mission scheduled jobs for mission definition {missionDefinition.Id} at {scheduledTimeInLocalTime}."
                );
                return;
            }

            var message =
                $"Skipped auto mission definition {missionDefinition.Name} planned for {scheduledTimeInLocalTime}.";
            ReportSkipAutoScheduleToSignalR(message, missionDefinition);
            return;
        }

        public async Task SkipAllAutoMissions(MissionDefinition missionDefinition)
        {
            var jobs = DeserializeAutoScheduleJobs(missionDefinition);
            foreach (var job in jobs)
            {
                try
                {
                    BackgroundJob.Delete(job.Value);
                    jobs.Remove(job.Key);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, $"Failed to delete background job: {job.Value}");
                }
            }
            if (jobs.Count == 0)
            {
                missionDefinition.AutoScheduleFrequency!.AutoScheduledJobs = null;
            }
            else
            {
                missionDefinition.AutoScheduleFrequency!.AutoScheduledJobs =
                    JsonSerializer.Serialize(jobs);
                logger.LogError(
                    "Failed to delete background some of the background jobs for mission definition {MissionDefinitionId}",
                    missionDefinition.Id
                );
            }
            await missionDefinitionService.Update(missionDefinition);
        }
    }
}
