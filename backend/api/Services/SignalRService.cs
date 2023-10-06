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
        private readonly IHubContext<SignalRHub> _signalRContext;

        public SignalRService(IHubContext<SignalRHub> signalRContext)
        {
            _signalRContext = signalRContext;
        }

        public async Task SendMessageAsync<T>(string label, T messageObject)
        {
            await _signalRContext.Clients.All.SendAsync(label, "all", JsonSerializer.Serialize(messageObject, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }

        public async Task SendMessageAsync<T>(string label, string user, T messageObject)
        {
            await _signalRContext.Clients.All.SendAsync(label, user, JsonSerializer.Serialize(messageObject, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }

        public async Task SendMessageAsync(string label, string message)
        {
            await _signalRContext.Clients.All.SendAsync(label, "all", message);
        }

        public async Task SendMessageAsync(string label, string user, string message)
        {
            await _signalRContext.Clients.All.SendAsync(label, user, message);
        }
    }
}
