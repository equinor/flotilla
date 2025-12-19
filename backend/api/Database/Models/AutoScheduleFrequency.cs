using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class AutoScheduleFrequency
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        public IList<TimeAndDay> SchedulingTimesCETperWeek { get; set; } = new List<TimeAndDay>();

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

        public IList<(TimeSpan, TimeOnly)>? GetSchedulingTimesUntilMidnight()
        {
            // NCS is always in CET
            TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(
                "Central European Standard Time"
            );
            DateTime nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzi);
            TimeOnly nowLocalTimeOnly = TimeOnly.FromDateTime(nowLocal);

            var autoScheduleNext = new List<(TimeSpan, TimeOnly)>();

            autoScheduleNext.AddRange(
                SchedulingTimesCETperWeek
                    .Where(schedulingTime => schedulingTime.DayOfWeek == nowLocal.DayOfWeek)
                    .Where(schedulingTime => schedulingTime.TimeOfDay > nowLocalTimeOnly)
                    .Select(schedulingTime =>
                        (schedulingTime.TimeOfDay - nowLocalTimeOnly, schedulingTime.TimeOfDay)
                    )
            );
            return autoScheduleNext.Count > 0 ? autoScheduleNext : null;
        }
    }

    [Owned]
    public class TimeAndDay
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly TimeOfDay { get; set; }

        public TimeAndDay(DayOfWeek dayOfWeek, TimeOnly timeOfDay)
        {
            DayOfWeek = dayOfWeek;
            TimeOfDay = timeOfDay;
        }
    }
}
