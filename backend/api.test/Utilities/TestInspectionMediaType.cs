using Api.Database.Models;
using Api.Utilities;
using Xunit;

namespace Api.Test.Utilities
{
    public class InspectionMediaTypeTest
    {
        [Theory]
        // Stored content type wins over extension and sensor type.
        [InlineData("image/jpeg", "clip.mp4", SensorType.Video, "image/jpeg")]
        // Default octet-stream is treated as unset, so the extension wins.
        [InlineData("application/octet-stream", "clip.mp4", SensorType.Image, "video/mp4")]
        // Blob extension wins over sensor type.
        [InlineData(null, "photo.png", SensorType.Video, "image/png")]
        // Sensor-type fallback when there is no stored type and no usable extension.
        [InlineData(null, null, SensorType.Video, "video/mp4")]
        [InlineData(null, null, SensorType.Image, "image/png")]
        [InlineData(null, null, SensorType.AcousticMeasurement, "video/mp4")]
        // Unknown extension + unsupported sensor type fall back to the default.
        [InlineData(null, "reading.bin", SensorType.Audio, "application/octet-stream")]
        public void CheckThatResolveReturnsExpectedContentType(
            string? storedContentType,
            string? blobName,
            SensorType sensorType,
            string expected
        )
        {
            var resolved = InspectionMediaType.Resolve(storedContentType, blobName, sensorType);

            Assert.Equal(expected, resolved);
        }
    }
}
