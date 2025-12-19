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
        /// Change the inspection frequency
        /// </summary>
        public TimeSpan? InspectionFrequency { get; set; }

        /// <summary>
        /// Change the time and day in the automated scheduling frequency
        /// Will be unchanged if null. Use an empty list to remove all scheduled times.
        /// </summary>
        public IList<TimeAndDayQuery>? SchedulingTimesCETperWeek { get; set; }
    }

    public struct TimeAndDayQuery
    {
        /// <summary>
        /// The day of the week to schedule the mission
        /// </summary>
        public DayOfWeek DayOfWeek { get; set; }

        /// <summary>
        /// The time of day to schedule the mission
        /// </summary>
        public TimeOnly TimeOfDay { get; set; }
    }

    public struct SkipAutoMissionQuery
    {
        /// <summary>
        /// The time of day to skip the mission
        /// </summary>
        public TimeOnly TimeOfDay { get; set; }
    }
}
