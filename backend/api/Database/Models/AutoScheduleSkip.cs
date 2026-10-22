using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Api.Database.Models;

[PrimaryKey(nameof(MissionDefinitionId), nameof(ScheduledAt))]
public class AutoScheduleSkip
{
    [Required]
    public required string MissionDefinitionId { get; set; }
    public MissionDefinition MissionDefinition { get; set; } = null!;

    public DateTimeOffset ScheduledAt { get; set; }

    public static string OccurrenceId(string definitionId, DateTimeOffset scheduledAt)
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
