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

    public class SignalRService : ISignalRService
    {
        private readonly IHubContext<SignalRHub> _signalRHub;

        public SignalRService(IHubContext<SignalRHub> signalRHub)
        {
            _signalRHub = signalRHub;
        }

        public async Task SendMessageAsync<T>(string label, T messageObject)
        {
            string json = JsonSerializer.Serialize(messageObject, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await _signalRHub.Clients.All.SendAsync(label, "all", json);
            await Task.CompletedTask;
        }

        public async Task SendMessageAsync<T>(string label, string user, T messageObject)
        {
            await _signalRHub.Clients.All.SendAsync(label, user, JsonSerializer.Serialize(messageObject, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            await Task.CompletedTask;
        }

        public async Task SendMessageAsync(string label, string message)
        {
            await _signalRHub.Clients.All.SendAsync(label, "all", message);
            await Task.CompletedTask;
        }

        public async Task SendMessageAsync(string label, string user, string message)
        {
            await _signalRHub.Clients.All.SendAsync(label, user, message);
            await Task.CompletedTask;
        }
    }
}
