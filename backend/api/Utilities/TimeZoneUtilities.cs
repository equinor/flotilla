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

        public static DateTimeOffset ToPostgresPrecision(DateTimeOffset instant) =>
            new(instant.UtcTicks - instant.UtcTicks % TimeSpan.TicksPerMicrosecond, TimeSpan.Zero);

        public static TimeOnly ToPostgresPrecision(TimeOnly time) =>
            new(time.Ticks - time.Ticks % TimeSpan.TicksPerMicrosecond);

        public static bool TryConvertCetWallClockToUtcUsingStandardTime(
            DateOnly cetDate,
            TimeOnly cetTime,
            out DateTimeOffset instant
        )
        {
            var unspecified = DateTime.SpecifyKind(
                cetDate.ToDateTime(cetTime),
                DateTimeKind.Unspecified
            );
            instant = default;
            if (CetZone.IsInvalidTime(unspecified))
                return false;
            instant = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(unspecified, CetZone));
            return true;
        }
    }
}
