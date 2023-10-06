using System.Threading.Tasks;
namespace Api.Services
{
    public class MockSignalRService : ISignalRService
    {
        public MockSignalRService()
        {
        }

        public async Task SendMessageAsync<T>(string label, T messageObject)
        {
            await Task.CompletedTask;
        }

        public async Task SendMessageAsync<T>(string label, string user, T messageObject)
        {
            await Task.CompletedTask;
        }

        public async Task SendMessageAsync(string label, string message)
        {
            await Task.CompletedTask;
        }

        public async Task SendMessageAsync(string label, string user, string message)
        {
            await Task.CompletedTask;
        }
    }
}
