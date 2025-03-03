using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    [Owned]
    public class AutoScheduleFrequency
    {
        // In local time
        public IList<TimeOnly>? TimesOfDay { get; set; }

        public IList<DayOfWeek>? DaysOfWeek { get; set; }

        public void ValidateAutoScheduleFrequency()
        {
            if (TimesOfDay is null || TimesOfDay.Count == 0)
            {
                throw new ArgumentException(
                    "AutoScheduleFrequency must have at least one time of day"
                );
            }

            if (DaysOfWeek is null || DaysOfWeek.Count == 0)
            {
                throw new ArgumentException(
                    "AutoScheduleFrequency must have at least one day of week"
                );
            }
        }

        public IList<TimeSpan>? GetSchedulingTimesForNext24Hours()
        {
            if (TimesOfDay is null || DaysOfWeek is null)
            {
                return null;
            }

            // NCS is always in CET
            TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(
                "Central European Standard Time"
            );
            DateTime nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzi);
            TimeOnly nowLocalTimeOnly = new(nowLocal.Hour, nowLocal.Minute, nowLocal.Second);

            var autoScheduleNext24Hours = new List<TimeSpan>();
            if (DaysOfWeek.Contains(nowLocal.DayOfWeek))
            {
                foreach (TimeOnly time in TimesOfDay)
                {
                    if (time > nowLocalTimeOnly)
                    {
                        autoScheduleNext24Hours.Add(time - nowLocalTimeOnly);
                    }
                }
            }
            if (DaysOfWeek.Contains(nowLocal.DayOfWeek + 1))
            {
                foreach (TimeOnly time in TimesOfDay)
                {
                    if (time <= nowLocalTimeOnly)
                    {
                        autoScheduleNext24Hours.Add(time - nowLocalTimeOnly);
                    }
                }
            }
            return autoScheduleNext24Hours.Count > 0 ? autoScheduleNext24Hours : null;
        }
    }
}
