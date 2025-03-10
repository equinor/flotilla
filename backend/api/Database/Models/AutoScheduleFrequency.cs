using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    [Owned]
    public class AutoScheduleFrequency
    {
        [Required]
        // In local time
        public IList<TimeOnly> TimesOfDay { get; set; } = new List<TimeOnly>();

        [Required]
        public IList<DayOfWeek> DaysOfWeek { get; set; } = new List<DayOfWeek>();

        public bool HasValidValue()
        {
            return TimesOfDay.Count != 0 && DaysOfWeek.Count != 0;
        }

        public void ValidateAutoScheduleFrequency()
        {
            if (TimesOfDay.Count == 0)
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

        public IList<TimeSpan>? GetSchedulingTimesUntilMidnight()
        {
            // NCS is always in CET
            TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(
                "Central European Standard Time"
            );
            DateTime nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzi);
            TimeOnly nowLocalTimeOnly = TimeOnly.FromDateTime(nowLocal);
            TimeSpan timeTilUtcMidnight =
                new TimeOnly(23, 59, 59) - TimeOnly.FromDateTime(DateTime.UtcNow);

            var autoScheduleNext = new List<TimeSpan>();

            if (DaysOfWeek.Contains(nowLocal.DayOfWeek))
            {
                autoScheduleNext.AddRange(
                    TimesOfDay
                        .Where(time =>
                            (time >= nowLocalTimeOnly)
                            && (time - nowLocalTimeOnly <= timeTilUtcMidnight)
                        )
                        .Select(time => time - nowLocalTimeOnly)
                );
            }
            if (DaysOfWeek.Contains(nowLocal.AddDays(1).DayOfWeek))
            {
                autoScheduleNext.AddRange(
                    TimesOfDay
                        .Where(time =>
                            (time < nowLocalTimeOnly)
                            && (time - nowLocalTimeOnly <= timeTilUtcMidnight)
                        )
                        .Select(time => time - nowLocalTimeOnly)
                );
            }

            return autoScheduleNext.Count > 0 ? autoScheduleNext : null;
        }
    }
}
