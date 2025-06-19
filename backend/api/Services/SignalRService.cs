using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Database.Models;
using Api.Services.Models;
using Api.SignalRHubs;
using Microsoft.AspNetCore.SignalR;

namespace Api.Services
{
    public interface ISignalRService
    {
        public Task SendMessageAsync<T>(string label, Installation? installation, T messageObject);
        public Task SendMessageAsync(string label, Installation? installation, string message);
        public void ReportDockFailureToSignalR(Robot robot, string message);
        public void ReportGeneralFailToSignalR(Robot robot, string title, string message);
        public void ReportAutoScheduleToSignalR(
            string type,
            string title,
            string message,
            string installationCode
        );
    }

    public class SignalRService : ISignalRService
    {
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly IHubContext<SignalRHub> _signalRHub;

        public SignalRService(IHubContext<SignalRHub> signalRHub)
        {
            _signalRHub = signalRHub;
            _serializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            _serializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public async Task SendMessageAsync<T>(
            string label,
            Installation? installation,
            T messageObject
        )
        {
            string json = JsonSerializer.Serialize(messageObject, _serializerOptions);
            await SendMessageAsync(label, installation, json);
        }

        public async Task SendMessageAsync(string label, Installation? installation, string message)
        {
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Local")
            {
                string? localDevUser = Environment.GetEnvironmentVariable("LOCAL_DEVUSERID");
                if (localDevUser is null || localDevUser.Equals("", StringComparison.Ordinal))
                    return;

                if (installation != null)
                    await _signalRHub
                        .Clients.Group(
                            localDevUser
                                + installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)
                        )
                        .SendAsync(label, "all", message);
                else
                    await _signalRHub.Clients.Group(localDevUser).SendAsync(label, "all", message);
            }
            else
            {
                if (installation != null)
                    await _signalRHub
                        .Clients.Group(
                            installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)
                        )
                        .SendAsync(label, "all", message);
                else
                    await _signalRHub.Clients.All.SendAsync(label, "all", message);
            }

            await Task.CompletedTask;
        }

        public void ReportDockFailureToSignalR(Robot robot, string message)
        {
            _ = SendMessageAsync(
                "Alert",
                robot.CurrentInstallation,
                new AlertResponse(
                    "DockFailure",
                    "Dock failure",
                    message,
                    robot.CurrentInstallation.InstallationCode,
                    robot.Id
                )
            );
        }

        public void ReportAutoScheduleToSignalR(
            string type,
            string missionDefinitionId,
            string message,
            string installationCode
        )
        {
            _ = SendMessageAsync(
                "Alert",
                null,
                new AlertResponse(type, missionDefinitionId, message, installationCode, null)
            );
        }

        public void ReportGeneralFailToSignalR(Robot robot, string title, string message)
        {
            _ = SendMessageAsync(
                "Alert",
                robot.CurrentInstallation,
                new AlertResponse(
                    "generalFailure",
                    title,
                    message,
                    robot.CurrentInstallation.InstallationCode,
                    robot.Id
                )
            );
        }
    }
}
