namespace Api.Services.Models
{
    public class UpdateRobotTelemetryMessage
    {
        public required string RobotId { get; set; }
        public required string TelemetryName { get; set; }
        public object? TelemetryValue { get; set; }
    }
}
