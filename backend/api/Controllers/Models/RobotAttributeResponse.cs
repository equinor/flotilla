using System.Text.Json.Serialization;
using Api.Database.Models;

namespace Api.Controllers.Models
{
    public class RobotAttributeResponse
    {
        public string Id { get; set; }

        public string PropertyName { get; set; }

        public object? Value { get; set; }

        [JsonConstructor]
#nullable disable
        public RobotAttributeResponse() { }

#nullable enable

        public RobotAttributeResponse(string robotId, string propertyName, object? robotProperty)
        {
            Id = robotId;
            PropertyName = propertyName;
            Value = robotProperty;
            if (
                !typeof(RobotResponse)
                    .GetProperties()
                    .Any(property => property.Name == propertyName)
            )
            {
                throw new ArgumentException(
                    $"Property {robotProperty} does not match any attributes in the RobotAttributeResponse class"
                );
            }
        }
    }
}
