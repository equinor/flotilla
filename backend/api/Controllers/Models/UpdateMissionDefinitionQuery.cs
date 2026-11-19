using System.ComponentModel.DataAnnotations;
using Api.Database.Models;

namespace Api.Controllers.Models
{
    public struct UpdateMissionDefinitionQuery
    {
        /// <summary>
        /// Change the comment describing the mission definition
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Change the display name
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Change the time and day in the automated scheduling frequency
        /// Will be unchanged if null. Use an empty list to remove all scheduled times.
        /// </summary>
        public IList<TimeAndDayQuery>? SchedulingTimesCETperWeek { get; set; }

        public IList<TaskQuery>? Tasks { get; set; }
    }

    public struct TimeAndDayQuery : IValidatableObject
    {
        /// <summary>
        /// The day of the week to schedule the mission
        /// </summary>
        public DayOfWeek DayOfWeek { get; set; }

        /// <summary>
        /// The time of day to schedule the mission, in whole seconds
        /// </summary>
        public TimeOnly TimeOfDay { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (TimeOfDay.Ticks % TimeSpan.TicksPerSecond != 0)
                yield return new ValidationResult(
                    "Scheduling times must use whole seconds; fractional seconds are not supported.",
                    [nameof(TimeOfDay)]
                );
        }
    }

    public struct SkipAutoMissionQuery
    {
        /// <summary>
        /// The exact scheduled timestamp returned with the dated occurrence to skip
        /// </summary>
        [Required]
        public required DateTimeOffset ScheduledAt { get; set; }
    }
}
