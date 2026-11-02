using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Api.Utilities;

public static class AutoScheduleKey
{
    // Stable mission/date-time key, also used as the eventual run ID, not the skip row ID.
    public static string Create(string definitionId, DateTimeOffset scheduledAt)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(
                definitionId
                    + "/"
                    + TimeZoneUtilities
                        .ToPostgresPrecision(scheduledAt)
                        .UtcTicks.ToString(CultureInfo.InvariantCulture)
            )
        );
        // Custom UUIDv8 retains the existing mission ID shape used by ISAR.
        hash[6] = (byte)((hash[6] & 0x0f) | 0x80);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);
        return new Guid(hash.AsSpan(0, 16), bigEndian: true).ToString();
    }
}
