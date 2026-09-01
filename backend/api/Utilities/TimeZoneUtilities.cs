using System;

namespace Api.Utilities
{
    // NCS is always in CET; centralizes the zone lookup and DST-correct CET->UTC conversion.
    public static class TimeZoneUtilities
    {
        public static TimeZoneInfo CetZone { get; } =
            TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

        public static DateTime NowCet()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CetZone);
        }

        public static DateTimeOffset CetWallClockToUtcInstant(DateOnly cetDate, TimeOnly cetTime)
        {
            var unspecified = DateTime.SpecifyKind(
                cetDate.ToDateTime(cetTime),
                DateTimeKind.Unspecified
            );
            return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(unspecified, CetZone));
        }
    }
}
