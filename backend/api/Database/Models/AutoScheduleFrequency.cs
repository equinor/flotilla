using System;
using Api.Utilities;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class AutoScheduleFrequency
    {
        public IList<TimeAndDay> SchedulingTimesCETperWeek { get; set; } = [];

        public bool HasValidValue()
        {
            return SchedulingTimesCETperWeek.Count != 0;
        }

        public IEnumerable<DateTimeOffset> GetSchedulingInstants(
            DateTimeOffset from,
            DateTimeOffset until
        )
        {
            var firstDate = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTime(from, TimeZoneUtilities.CetZone).DateTime
            );
            var lastDate = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTime(until, TimeZoneUtilities.CetZone).DateTime
            );
            for (var date = firstDate; date <= lastDate; date = date.AddDays(1))
            {
                foreach (
                    var time in SchedulingTimesCETperWeek
                        .Where(t => t.DayOfWeek == date.DayOfWeek)
                        // Match Npgsql's timestamp precision, including duplicate fractional slots.
                        .Select(t => new TimeOnly(
                            t.TimeOfDay.Ticks - t.TimeOfDay.Ticks % TimeSpan.TicksPerMicrosecond
                        ))
                        .Distinct()
                )
                {
                    // A nonexistent spring-forward time has no occurrence. In autumn, use
                    // standard time once, matching TimeZoneInfo's existing conversion policy.
                    if (TimeZoneUtilities.CetZone.IsInvalidTime(date.ToDateTime(time)))
                        continue;
                    var instant = TimeZoneUtilities.CetWallClockToUtcInstant(date, time);
                    if (instant > from && instant <= until)
                        yield return instant;
                }
            }
        }
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
