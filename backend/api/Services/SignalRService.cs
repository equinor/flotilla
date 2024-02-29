using System.Globalization;
using System.Text.Json;
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
        public void ReportFailureToSignalR(Robot robot, string message);
    }

    public class SignalRService(IHubContext<SignalRHub> signalRHub) : ISignalRService
    {
        private readonly JsonSerializerOptions _serializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public async Task SendMessageAsync<T>(string label, Installation? installation, T messageObject)
        {
            string json = JsonSerializer.Serialize(messageObject, _serializerOptions);
            await SendMessageAsync(label, installation, json);
        }

        public async Task SendMessageAsync(string label, Installation? installation, string message)
        {
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Local")
            {
                string? localDevUser = Environment.GetEnvironmentVariable("LOCAL_DEVUSERID");
                if (localDevUser is null || localDevUser.Equals("", StringComparison.Ordinal)) return;

                if (installation != null)
                    await signalRHub.Clients.Group(localDevUser + installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)).SendAsync(label, "all", message);
                else
                    await signalRHub.Clients.Group(localDevUser).SendAsync(label, "all", message);
            }
            else
            {
                if (installation != null)
                    await signalRHub.Clients.Group(installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)).SendAsync(label, "all", message);
                else
                    await signalRHub.Clients.All.SendAsync(label, "all", message);
            }


            await Task.CompletedTask;
        }

        public void ReportFailureToSignalR(Robot robot, string message)
        {
            _ = SendMessageAsync(
                "Alert",
                robot.CurrentInstallation,
                new AlertResponse("safezoneFailure", "Safezone failure", message, robot.CurrentInstallation.InstallationCode, robot.Id));
        }

    }
}
