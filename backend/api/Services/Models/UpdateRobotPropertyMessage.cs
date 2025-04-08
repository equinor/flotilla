namespace Api.Services.Models
{
    public class UpdateRobotPropertyMessage
    {
        public required string RobotId { get; set; }
        public required string PropertyName { get; set; }
        public object? PropertyValue { get; set; }
    }
}
