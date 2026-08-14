using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Database.Models;
using Api.Services;

namespace Api.Test.Mocks
{
    public class MockSignalRService : ISignalRService
    {
        // Messages are added from MQTT background threads while tests poll this collection,
        // so access is locked and readers get a snapshot to enumerate safely.
        private readonly object _lock = new();
        private readonly List<object> _latestMessages = [];

        public IReadOnlyList<object> LatestMessages
        {
            get
            {
                lock (_lock)
                    return _latestMessages.ToList();
            }
        }

        private void AddMessage(string label, object? messageObject)
        {
            lock (_lock)
                _latestMessages.Add(new { Label = label, Message = messageObject });
        }

        public async Task SendMessageAsync<T>(
            string label,
            Installation? installation,
            T messageObject
        )
        {
            AddMessage(label, messageObject);
            await Task.CompletedTask;
        }

        public async Task SendMessageAsync<T>(
            string label,
            string? installationCode,
            T messageObject
        )
        {
            AddMessage(label, messageObject);
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
