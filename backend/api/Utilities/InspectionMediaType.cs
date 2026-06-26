using Api.Database.Models;

namespace Api.Utilities
{
    public static class InspectionMediaType
    {
        public const string DefaultContentType = "application/octet-stream";

        // Resolution order: stored Content-Type, then blob extension, then sensor type.
        public static string Resolve(
            string? storedContentType,
            string? blobName,
            SensorType sensorType
        )
        {
            if (
                !string.IsNullOrWhiteSpace(storedContentType)
                && !storedContentType.Equals(DefaultContentType, StringComparison.OrdinalIgnoreCase)
            )
            {
                return storedContentType;
            }

            return FromExtension(blobName) ?? FromSensorType(sensorType);
        }

        private static string? FromExtension(string? blobName)
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                return null;
            }

            return Path.GetExtension(blobName).ToLowerInvariant() switch
            {
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => null,
            };
        }

        private static string FromSensorType(SensorType sensorType)
        {
            return sensorType switch
            {
                SensorType.Video or SensorType.ThermalVideo or SensorType.AcousticMeasurement =>
                    "video/mp4",
                SensorType.Image or SensorType.ThermalImage => "image/png",
                _ => DefaultContentType,
            };
        }
    }
}
