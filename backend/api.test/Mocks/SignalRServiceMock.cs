using System.Threading.Tasks;
using Api.Database.Models;
namespace Api.Services
{
    public class MockSignalRService : ISignalRService
    {
        public MockSignalRService()
        {
        }

        public async Task SendMessageAsync<T>(string label, Installation? installation, T messageObject)
        {
            await Task.CompletedTask;
        }

        public async Task SendMessageAsync(string label, Installation? installation, string message)
        {
            await Task.CompletedTask;
        }

        public void ReportSafeZoneFailureToSignalR(Robot robot, string message)
        {
            return;
        }

        public void ReportScheduleFailureToSignalR(Robot robot, string message)
        {
            return;
        }

        public void ReportSafeZoneSuccessToSignalR(Robot robot, string message)
        {
            return;
        }
    }
}
