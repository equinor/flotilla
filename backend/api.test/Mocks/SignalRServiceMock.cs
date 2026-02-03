using System.Threading.Tasks;
using Api.Database.Models;
using Api.Services;

namespace Api.Test.Mocks
{
    public class MockSignalRService : ISignalRService
    {
        public async Task SendMessageAsync<T>(
            string label,
            Installation? installation,
            T messageObject
        )
        {
            await Task.CompletedTask;
        }

        public async Task SendMessageAsync<T>(
            string label,
            string? installationCode,
            T messageObject
        )
        {
            await Task.CompletedTask;
        }

        public async Task SendMessageAsync(string label, string? installationCode, string message)
        {
            await Task.CompletedTask;
        }

        public void ReportDockFailureToSignalR(Robot robot, string message) { }

        public void ReportGeneralFailToSignalR(Robot robot, string title, string message) { }

        public void ReportAutoScheduleToSignalR(
            string type,
            string missionDefinitionId,
            string message,
            string installationCode
        ) { }
    }
}
