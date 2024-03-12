using System.Text.Json.Serialization;
namespace Api.Services.Models
{
    [method: JsonConstructor]
    public class AlertResponse(string code, string title, string message, string installationCode, string? robotId)
    {
        public string AlertCode { get; set; } = code;
        public string AlertTitle { get; set; } = title;
        public string AlertMessage { get; set; } = message;
        public string InstallationCode { get; set; } = installationCode;
        public string? RobotId { get; set; } = robotId;
    }
}
