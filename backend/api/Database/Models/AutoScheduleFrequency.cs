using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    [Owned]
    public class AutoScheduleFrequency
    {
        [Required]
        // In Central European Time
        public IList<TimeOnly> TimesOfDayCET { get; set; } = new List<TimeOnly>();

        [Required]
        public IList<DayOfWeek> DaysOfWeek { get; set; } = new List<DayOfWeek>();

        public string? AutoScheduledJobs { get; set; }

        public bool HasValidValue()
        {
            return TimesOfDayCET.Count != 0 && DaysOfWeek.Count != 0;
        }

        public void ValidateAutoScheduleFrequency()
        {
            if (TimesOfDayCET.Count == 0)
            {
                throw new ArgumentException(
                    "AutoScheduleFrequency must have at least one time of day"
                );
            }

            if (DaysOfWeek.Count == 0)
            {
                throw new ArgumentException(
                    "AutoScheduleFrequency must have at least one day of week"
                );
            }
        }

        public IList<(TimeSpan, TimeOnly)>? GetSchedulingTimesUntilMidnight()
        {
            // NCS is always in CET
            TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(
                "Central European Standard Time"
            );
            DateTime nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzi);
            TimeOnly nowLocalTimeOnly = TimeOnly.FromDateTime(nowLocal);

            var autoScheduleNext = new List<(TimeSpan, TimeOnly)>();

            if (DaysOfWeek.Contains(nowLocal.DayOfWeek))
            {
                autoScheduleNext.AddRange(
                    TimesOfDayCET
                        .Where(time => time >= nowLocalTimeOnly)
                        .Select(time => (time - nowLocalTimeOnly, time))
                );
            }
            return autoScheduleNext.Count > 0 ? autoScheduleNext : null;
        }
    }
}
