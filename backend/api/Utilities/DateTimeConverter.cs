using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Utilities
{
    public class DateTimeConverter : JsonConverter<DateTime>
    {
        private const string DateFormat = "yyyy-MM-ddTHH:mm:ss.fff";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (!DateTime.TryParse(reader.GetString(), out var value))
            {
                throw new JsonException($"Unexpected datetime format: '{reader.GetString()}'");
            }
            return value;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(DateFormat, CultureInfo.InvariantCulture));
        }
    }
}
