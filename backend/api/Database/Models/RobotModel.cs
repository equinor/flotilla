using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using Api.Controllers.Models;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    /// <summary>
    /// The type of robot model
    /// </summary>
    public enum RobotType
    {
        /// WARNING:
        /// Changing the names of these enum options is the same as changing their value,
        /// so it will require updating the database with the new names because the enum
        /// is stored as strings in database
        TaurobInspector,
        TaurobOperator,
        ExR2,
        Robot,
        Turtlebot,
        AnymalX,
        AnymalD,
    }

    public class RobotModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        /// <summary>
        /// The type of robot model
        /// </summary>
        [Required]
        //Save robot type as string in database
        [Column(TypeName = "nvarchar(56)")]
        public RobotType Type { get; set; }

        /// <summary>
        /// Lower battery warning threshold in percentage
        /// </summary>
        public float? BatteryWarningThreshold { get; set; }

        /// <summary>
        /// Upper pressure warning threshold in mBar
        /// </summary>
        public float? UpperPressureWarningThreshold { get; set; }

        /// <summary>
        /// Lower pressure warning threshold in mBar
        /// </summary>
        public float? LowerPressureWarningThreshold { get; set; }

        /// <summary>
        /// The average time in seconds spent by this model on a single tag (excluding recording duration for video/audio)
        /// </summary>
        public float? AverageDurationPerTag { get; set; }

        public RobotModel() { }

        public RobotModel(CreateRobotModelQuery query)
        {
            Type = query.RobotType;
            BatteryWarningThreshold = query.BatteryWarningThreshold;
            UpperPressureWarningThreshold = query.UpperPressureWarningThreshold;
            LowerPressureWarningThreshold = query.LowerPressureWarningThreshold;
        }

        public void Update(UpdateRobotModelQuery updateQuery)
        {
            BatteryWarningThreshold = updateQuery.BatteryWarningThreshold;
            UpperPressureWarningThreshold = updateQuery.UpperPressureWarningThreshold;
            LowerPressureWarningThreshold = updateQuery.LowerPressureWarningThreshold;
        }

        /// <summary>
        /// Updates the <see cref="AverageDurationPerTag"/> based on the data in the <paramref name="recentMissionsForModelType"/> provided
        /// </summary>
        /// <param name="recentMissionsForModelType"></param>
        public void UpdateAverageDurationPerTag(List<Mission> recentMissionsForModelType)
        {
            if (recentMissionsForModelType.Any(mission => mission.Robot.Model.Type != Type))
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "{0} should only include missions for this model type ('{1}')",
                        nameof(recentMissionsForModelType),
                        Type
                    ),
                    nameof(recentMissionsForModelType)
                );

            // The time spent on each tasks, not including the duration of video/audio recordings
            var timeSpentPerTask = recentMissionsForModelType
                .SelectMany(
                    mission =>
                        mission.Tasks
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
            if (timeSpentPerTask.All(time => time < 0))
                return;

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
                .Select(d => d < percentile1 ? percentile1 : (d > percentile9 ? percentile9 : d))
                .Average();

            AverageDurationPerTag = (float)result;
        }
    }
}
