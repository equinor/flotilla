using System;
using System.Linq;
using Api.Database.Models;
using Xunit;

namespace Api.Test.Utilities
{
    public class ScheduledMissionTimeTests
    {
        [Theory]
        [InlineData(
            "2026-10-16T20:00:00Z",
            "2026-10-17T20:00:00Z",
            DayOfWeek.Saturday,
            1,
            0,
            "2026-10-16T23:00:00Z"
        )]
        [InlineData(
            "2026-10-24T22:00:00Z",
            "2026-10-25T22:00:00Z",
            DayOfWeek.Sunday,
            2,
            30,
            "2026-10-25T01:30:00Z"
        )]
        [InlineData(
            "2026-12-31T22:00:00Z",
            "2027-01-01T22:00:00Z",
            DayOfWeek.Friday,
            1,
            0,
            "2027-01-01T00:00:00Z"
        )]
        public void RecurrenceUsesOsloTimezoneAndDatedInstants(
            string from,
            string until,
            DayOfWeek day,
            int hour,
            int minute,
            string expected
        )
        {
            var schedule = new AutoScheduleFrequency
            {
                SchedulingTimesCETperWeek = [new TimeAndDay(day, new TimeOnly(hour, minute))],
            };
            Assert.Equal(
                DateTimeOffset.Parse(expected),
                Assert.Single(
                    schedule.GetSchedulingInstants(
                        DateTimeOffset.Parse(from),
                        DateTimeOffset.Parse(until)
                    )
                )
            );
        }

        [Fact]
        public void SpringForwardNonexistentTimeIsNotShiftedIntoAnotherSlot()
        {
            var schedule = new AutoScheduleFrequency
            {
                SchedulingTimesCETperWeek = [new TimeAndDay(DayOfWeek.Sunday, new TimeOnly(2, 30))],
            };
            Assert.Empty(
                schedule.GetSchedulingInstants(
                    DateTimeOffset.Parse("2026-03-28T23:00:00Z"),
                    DateTimeOffset.Parse("2026-03-29T23:00:00Z")
                )
            );
        }

        [Fact]
        public void WindowExcludesNowIncludesUpperBoundaryAndDeduplicatesSlots()
        {
            var schedule = new AutoScheduleFrequency
            {
                SchedulingTimesCETperWeek =
                [
                    new TimeAndDay(DayOfWeek.Friday, new TimeOnly(22, 0)),
                    new TimeAndDay(DayOfWeek.Saturday, new TimeOnly(22, 0)),
                    new TimeAndDay(DayOfWeek.Saturday, new TimeOnly(22, 0)),
                ],
            };
            Assert.Equal(
                DateTimeOffset.Parse("2026-10-17T20:00:00Z"),
                Assert.Single(
                    schedule.GetSchedulingInstants(
                        DateTimeOffset.Parse("2026-10-16T20:00:00Z"),
                        DateTimeOffset.Parse("2026-10-17T20:00:00Z")
                    )
                )
            );
        }
    }
}
