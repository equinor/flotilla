using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Api.Database.Models.TaskStatus;

namespace Api.Services
{
    public interface IStatisticsService
    {
        public Task<RobotStatisticsResponse> GetRobotStatistics(
            string robotId,
            DateTime fromTime,
            DateTime toTime
        );
    }

    public class StatisticsService(FlotillaDbContext context, IAccessRoleService accessRoleService)
        : IStatisticsService
    {
        private static readonly MissionStatus[] CompletedMissionStatuses =
        [
            MissionStatus.Successful,
            MissionStatus.PartiallySuccessful,
            MissionStatus.Failed,
            MissionStatus.Aborted,
            MissionStatus.Cancelled,
        ];

        public async Task<RobotStatisticsResponse> GetRobotStatistics(
            string robotId,
            DateTime fromTime,
            DateTime toTime
        )
        {
            var accessibleInstallationCodes = await accessRoleService.GetAllowedInstallationCodes(
                AccessMode.Read
            );

            var completedRuns = context
                .MissionRuns.AsNoTracking()
                .Where(m => m.Robot.Id == robotId)
                .Where(m => m.IsDeprecated == false)
                .Where(m => m.CreationTime >= fromTime && m.CreationTime < toTime)
                .Where(m => CompletedMissionStatuses.Contains(m.Status))
                .Where(m =>
                    accessibleInstallationCodes.Contains(
                        m.InspectionArea.Installation.InstallationCode.ToUpper()
                    )
                );

            // A single robot completes at most a few hundred runs in a typical
            // window, so materialising the status/creation-time pairs keeps the
            // per-status counts and weekly buckets in memory cheap.
            var runData = await completedRuns
                .Select(m => new { m.Status, m.CreationTime })
                .ToListAsync();

            var taskCounts = await completedRuns
                .SelectMany(m => m.Tasks)
                .GroupBy(t => t.Status)
                .Select(group => new { Status = group.Key, Count = group.Count() })
                .ToListAsync();

            int successfulMissions = runData.Count(m => m.Status == MissionStatus.Successful);
            int partiallySuccessfulMissions = runData.Count(m =>
                m.Status == MissionStatus.PartiallySuccessful
            );
            var missions = new MissionStatisticsResponse
            {
                Total = runData.Count,
                Successful = successfulMissions,
                PartiallySuccessful = partiallySuccessfulMissions,
                Failed = runData.Count(m => m.Status == MissionStatus.Failed),
                Aborted = runData.Count(m => m.Status == MissionStatus.Aborted),
                Cancelled = runData.Count(m => m.Status == MissionStatus.Cancelled),
                SuccessRate = CalculateSuccessRate(
                    successfulMissions,
                    partiallySuccessfulMissions,
                    runData.Count
                ),
            };

            int TaskCount(TaskStatus status) =>
                taskCounts.FirstOrDefault(t => t.Status == status)?.Count ?? 0;

            int totalTasks = taskCounts.Sum(t => t.Count);
            int successfulTasks = TaskCount(TaskStatus.Successful);
            int partiallySuccessfulTasks = TaskCount(TaskStatus.PartiallySuccessful);
            var tasks = new TaskStatisticsResponse
            {
                Total = totalTasks,
                Successful = successfulTasks,
                PartiallySuccessful = partiallySuccessfulTasks,
                SuccessRate = CalculateSuccessRate(
                    successfulTasks,
                    partiallySuccessfulTasks,
                    totalTasks
                ),
            };

            var missionsPerWeek = new List<WeeklyMissionCountResponse>();
            int numberOfWeeks = (int)((toTime - fromTime).TotalDays / 7);
            for (int week = numberOfWeeks - 1; week >= 0; week--)
            {
                var weekStart = toTime.AddDays(-7 * (week + 1));
                var weekEnd = toTime.AddDays(-7 * week);
                missionsPerWeek.Add(
                    new WeeklyMissionCountResponse
                    {
                        WeekStart = weekStart,
                        WeekEnd = weekEnd,
                        Count = runData.Count(m =>
                            m.CreationTime >= weekStart && m.CreationTime < weekEnd
                        ),
                    }
                );
            }

            return new RobotStatisticsResponse
            {
                RobotId = robotId,
                FromTime = fromTime,
                ToTime = toTime,
                Missions = missions,
                Tasks = tasks,
                MissionsPerWeek = missionsPerWeek,
            };
        }

        private static double CalculateSuccessRate(
            int successful,
            int partiallySuccessful,
            int total
        )
        {
            return total == 0 ? 0 : (double)(successful + partiallySuccessful) / total;
        }
    }
}
