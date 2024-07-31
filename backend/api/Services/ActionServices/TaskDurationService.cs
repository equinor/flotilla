using System.Globalization;
using Api.Controllers.Models;
using Api.Database.Models;
namespace Api.Services.ActionServices
{
    public interface ITaskDurationService
    {
        public Task UpdateAverageDurationPerTask(RobotType robotType);
    }

    public class TaskDurationService(ILogger<TaskDurationService> logger, IConfiguration configuration, IRobotModelService robotModelService, IMissionRunService missionRunService) : ITaskDurationService
    {
        public async Task UpdateAverageDurationPerTask(RobotType robotType)
        {
            int timeRangeInDays = configuration.GetValue<int>("TimeRangeForMissionDurationEstimationInDays");
            long minEpochTime = DateTimeOffset.Now
                .AddDays(-timeRangeInDays)
                .ToUnixTimeSeconds();

            var missionRunsForEstimation = await missionRunService.ReadAll(
                new MissionRunQueryStringParameters
                {
                    MinDesiredStartTime = minEpochTime,
                    RobotModelType = robotType,
                    PageSize = QueryStringParameters.MaxPageSize
                }
            );

            var model = await robotModelService.ReadByRobotType(robotType, readOnly: true);
            if (model is null)
            {
                logger.LogWarning("Could not update average duration for robot model {RobotType} as the model was not found", robotType);
                return;
            }

            await UpdateAverageDuration(missionRunsForEstimation, model);
        }

        private async Task UpdateAverageDuration(List<MissionRun> recentMissionRunsForModelType, RobotModel robotModel)
        {
            if (recentMissionRunsForModelType.Any(missionRun => missionRun.Robot.Model.Type != robotModel.Type))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "{0} should only include missions for this model type ('{1}')",
                        nameof(recentMissionRunsForModelType), robotModel.Type),
                    nameof(recentMissionRunsForModelType)
                );
            }

            // The time spent on each tasks, not including the duration of video/audio recordings
            var timeSpentPerTask = recentMissionRunsForModelType
                .SelectMany(
                    missionRun =>
                        missionRun.Tasks
                            .Where(task => task.EndTime is not null && task.StartTime is not null)
                            .Select(
                                task =>
                                    (task.EndTime! - task.StartTime!).Value.TotalSeconds
                                    - task.Inspections.Sum(
                                        inspection => inspection.VideoDuration ?? 0
                                    )
                            )
                )
                .ToList();

            // If no valid task times, return
            if (timeSpentPerTask.All(time => time < 0)) { return; }

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
                .Select(d => d < percentile1 ? percentile1 : d > percentile9 ? percentile9 : d)
                .Average();

            robotModel.AverageDurationPerTag = (float)result;

            await robotModelService.Update(robotModel);

            logger.LogInformation("Robot model '{ModelType}' - Updated average time spent per tag to {AverageTimeSpent}s", robotModel.Type, robotModel.AverageDurationPerTag);
        }
    }
}
