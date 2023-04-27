using System.Text.Json;

namespace Api.Utilities
{
    public static class ObjectCopier
    {
        public static T Clone<T>(T source)
        {
            string serialized = JsonSerializer.Serialize(source);
            return JsonSerializer.Deserialize<T>(serialized)!;
        }
    }
}
