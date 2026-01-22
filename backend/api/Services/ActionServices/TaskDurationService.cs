using Api.Controllers.Models;
using Api.Database.Models;

namespace Api.Services.ActionServices
{
    public interface ITaskDurationService
    {
        public Task UpdateAverageDurationPerTask(Robot robot);
    }

    public class TaskDurationService(
        ILogger<TaskDurationService> logger,
        IConfiguration configuration,
        IRobotService robotService,
        IMissionRunService missionRunService
    ) : ITaskDurationService
    {
        public async Task UpdateAverageDurationPerTask(Robot robot)
        {
            int timeRangeInDays = configuration.GetValue<int>(
                "TimeRangeForMissionDurationEstimationInDays"
            );
            long minEpochTime = DateTimeOffset.Now.AddDays(-timeRangeInDays).ToUnixTimeSeconds();

            var missionRunsForEstimation = await missionRunService.ReadAll(
                new MissionRunQueryStringParameters
                {
                    MinCreationTime = minEpochTime,
                    RobotId = robot.Id,
                    PageSize = QueryStringParameters.MaxPageSize,
                },
                readOnly: true
            );

            await UpdateAverageDuration(missionRunsForEstimation, robot);
        }

        private async Task UpdateAverageDuration(
            List<MissionRun> recentMissionRunsForModelType,
            Robot robot
        )
        {
            // The time spent on each tasks, not including the duration of video/audio recordings
            var timeSpentPerTask = recentMissionRunsForModelType
                .SelectMany(missionRun =>
                    missionRun
                        .Tasks.Where(task => task.EndTime is not null && task.StartTime is not null)
                        .Select(task =>
                            (task.EndTime! - task.StartTime!).Value.TotalSeconds
                            - (task.Inspection?.VideoDuration ?? 0)
                        )
                )
                .ToList();

            // If no valid task times, return
            if (timeSpentPerTask.All(time => time < 0))
            {
                return;
            }

            // Percentiles to exclude when calculating average
            const double P1 = 0.1;
            const double P9 = 0.9;
            double percentile1 = timeSpentPerTask
                .OrderBy(d => d)
                .ElementAt((int)Math.Floor(P1 * (timeSpentPerTask.Count - 1)));
            double percentile9 = timeSpentPerTask
                .OrderBy(d => d)
                .ElementAt((int)Math.Floor(P9 * (timeSpentPerTask.Count - 1)));

            // Calculate average, excluding outliers by using percentiles
            double result = timeSpentPerTask
                .Select(d =>
                    d < percentile1 ? percentile1
                    : d > percentile9 ? percentile9
                    : d
                )
                .Average();

            robot.AverageDurationPerTag = (float)result;

            await robotService.Update(robot);

            logger.LogInformation(
                "Robot '{robotId}' - Updated average time spent per tag to {AverageTimeSpent}s",
                robot.Id,
                robot.AverageDurationPerTag
            );
        }
    }
}
