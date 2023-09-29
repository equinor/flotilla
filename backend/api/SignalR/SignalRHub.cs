using Microsoft.AspNetCore.SignalR;

namespace Api.SignalRHubs
{
    public interface ISignalRClient
    {
        Task ReceiveMessage(string user, string message);
    }

    public class SignalRHub : Hub<ISignalRClient>
    {
        public async Task SendMessage(string user, string message)
            => await Clients.All.ReceiveMessage(user, message);
    }
}