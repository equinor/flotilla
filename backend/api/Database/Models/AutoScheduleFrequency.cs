using System;
using Api.Utilities;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class AutoScheduleFrequency
    {
        public IList<TimeAndDay> SchedulingTimesCETperWeek { get; set; } = [];

        public string? AutoScheduledJobs { get; set; }

        public bool HasValidValue()
        {
            return SchedulingTimesCETperWeek.Count != 0;
        }

        public bool IsUnchanged(IList<TimeAndDay>? newSchedulingTimesCETperWeek)
        {
            if (newSchedulingTimesCETperWeek == null || SchedulingTimesCETperWeek == null)
            {
                return newSchedulingTimesCETperWeek == SchedulingTimesCETperWeek;
            }

            if (newSchedulingTimesCETperWeek.Count != SchedulingTimesCETperWeek.Count)
            {
                return false;
            }

            foreach (var schedulingTime in newSchedulingTimesCETperWeek)
            {
                if (
                    !SchedulingTimesCETperWeek.Any(existingTime =>
                        existingTime.DayOfWeek == schedulingTime.DayOfWeek
                        && existingTime.TimeOfDay == schedulingTime.TimeOfDay
                    )
                )
                {
                    return false;
                }
            }

            return true;
        }

        public IList<(DateTimeOffset, TimeOnly)>? GetSchedulingTimesUntilMidnight()
        {
            DateTime nowLocal = TimeZoneUtilities.NowCet();
            TimeOnly nowLocalTimeOnly = TimeOnly.FromDateTime(nowLocal);
            DateOnly todayLocal = DateOnly.FromDateTime(nowLocal);

            var autoScheduleNext = SchedulingTimesCETperWeek
                .Where(schedulingTime => schedulingTime.DayOfWeek == nowLocal.DayOfWeek)
                .Where(schedulingTime => schedulingTime.TimeOfDay > nowLocalTimeOnly)
                .Select(schedulingTime =>
                    (
                        TimeZoneUtilities.CetWallClockToUtcInstant(
                            todayLocal,
                            schedulingTime.TimeOfDay
                        ),
                        schedulingTime.TimeOfDay
                    )
                )
                .ToList();

            return autoScheduleNext.Count > 0 ? autoScheduleNext : null;
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
