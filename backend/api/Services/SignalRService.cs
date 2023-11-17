using System.Text.Json;
using Api.SignalRHubs;
using Microsoft.AspNetCore.SignalR;
namespace Api.Services
{
    public interface ISignalRService
    {
        public Task SendMessageAsync<T>(string label, T messageObject);
        public Task SendMessageAsync<T>(string label, string user, T messageObject);
        public Task SendMessageAsync(string label, string message);
        public Task SendMessageAsync(string label, string user, string message);
    }

    public class SignalRService(IHubContext<SignalRHub> signalRHub) : ISignalRService
    {
        private readonly JsonSerializerOptions _serializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public async Task SendMessageAsync<T>(string label, T messageObject)
        {
            string json = JsonSerializer.Serialize(messageObject, _serializerOptions);
            await signalRHub.Clients.All.SendAsync(label, "all", json);
            await Task.CompletedTask;
        }

        public async Task SendMessageAsync<T>(string label, string user, T messageObject)
        {
            await signalRHub.Clients.All.SendAsync(label, user, JsonSerializer.Serialize(messageObject, _serializerOptions));
            await Task.CompletedTask;
        }

        public async Task SendMessageAsync(string label, string message)
        {
            await signalRHub.Clients.All.SendAsync(label, "all", message);
            await Task.CompletedTask;
        }

        public async Task SendMessageAsync(string label, string user, string message)
        {
            await signalRHub.Clients.All.SendAsync(label, user, message);
            await Task.CompletedTask;
        }
    }
}
