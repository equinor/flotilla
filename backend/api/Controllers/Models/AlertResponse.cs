using System.Text.Json.Serialization;
namespace Api.Controllers.Models
{
    [method: JsonConstructor]
    public class AlertResponse(string code, string name, string message, string installationCode, string? robotId)
    {
        public string AlertCode { get; set; } = code;
        public string AlertName { get; set; } = name;
        public string AlertMessage { get; set; } = message;
        public string InstallationCode { get; set; } = installationCode;
        public string? RobotId { get; set; } = robotId;
    }
}
