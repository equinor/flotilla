using System;
using Api.Utilities;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class AutoScheduleFrequency
    {
        private const int CalendarDayStep = 1;

        public IList<TimeAndDay> SchedulingTimesCETperWeek { get; set; } = [];

        public bool HasValidValue()
        {
            return SchedulingTimesCETperWeek.Count != 0;
        }

        public IEnumerable<DateTimeOffset> GetSchedulingInstants(
            DateTimeOffset fromExclusive,
            DateTimeOffset untilInclusive
        )
        {
            var firstDate = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTime(fromExclusive, TimeZoneUtilities.CetZone).DateTime
            );
            var lastDate = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTime(untilInclusive, TimeZoneUtilities.CetZone).DateTime
            );
            for (var date = firstDate; date <= lastDate; date = date.AddDays(CalendarDayStep))
            {
                foreach (var time in GetDistinctTimesForDay(date.DayOfWeek))
                {
                    if (
                        TimeZoneUtilities.TryConvertCetWallClockToUtcUsingStandardTime(
                            date,
                            time,
                            out var instant
                        )
                        && instant > fromExclusive
                        && instant <= untilInclusive
                    )
                        yield return instant;
                }
            }
        }

        private IEnumerable<TimeOnly> GetDistinctTimesForDay(DayOfWeek day) =>
            SchedulingTimesCETperWeek
                .Where(t => t.DayOfWeek == day)
                .Select(t => TimeZoneUtilities.ToPostgresPrecision(t.TimeOfDay))
                .Distinct();
    }

    public class TimeAndDay
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly TimeOfDay { get; set; }

        public TimeAndDay(DayOfWeek dayOfWeek, TimeOnly timeOfDay)
        {
            DayOfWeek = dayOfWeek;
            TimeOfDay = timeOfDay;
        }
    }
}
