using System.Globalization;
using System.Text.Json;
using Api.Database.Models;
using Api.SignalRHubs;
using Microsoft.AspNetCore.SignalR;
namespace Api.Services
{
    public interface ISignalRService
    {
        public Task SendMessageAsync<T>(string label, Installation? installation, T messageObject);
        public Task SendMessageAsync(string label, Installation? installation, string message);
    }

    public class SignalRService(IHubContext<SignalRHub> signalRHub, IAccessRoleService accessRoleService) : ISignalRService
    {
        private readonly JsonSerializerOptions _serializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public async Task SendMessageAsync<T>(string label, Installation? installation, T messageObject)
        {
            string json = JsonSerializer.Serialize(messageObject, _serializerOptions);
            await SendMessageAsync(label, installation, json);
        }

        public async Task SendMessageAsync(string label, Installation? installation, string message)
        {
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                string? nameId = accessRoleService.GetRequestNameId();
                if (nameId is null) return;
                if (installation != null)
                    await signalRHub.Clients.Group(nameId + installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)).SendAsync(label, "all", message);
                else
                    // TODO: can't do this if we use DEV. Then we instead need a generic group for connection id
                    await signalRHub.Clients.User(nameId).SendAsync(label, "all", message);
            }
            else
            {
                if (installation != null)
                    await signalRHub.Clients.Group(installation.InstallationCode.ToUpper(CultureInfo.CurrentCulture)).SendAsync(label, "all", message);
                else
                    // TODO: can't do this if we use DEV. Then we instead need a generic group for connection id
                    await signalRHub.Clients.All.SendAsync(label, "all", message);
            }
            

            await Task.CompletedTask;
        }
    }
}
