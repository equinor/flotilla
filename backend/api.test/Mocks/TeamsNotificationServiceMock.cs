using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Api.Services;

namespace Api.Test.Mocks
{
    public record TeamsNotification(TeamsDestination Destination, string CardJson);

    public class MockTeamsNotificationService : ITeamsNotificationService
    {
        private readonly List<TeamsNotification> _notifications = [];

        public IReadOnlyList<TeamsNotification> Notifications
        {
            get
            {
                lock (_notifications)
                    return _notifications.ToList();
            }
        }

        public Task SendTeamsMessageAsync(
            string message,
            TeamsDestination destination,
            CancellationToken cancellationToken = default
        )
        {
            lock (_notifications)
                _notifications.Add(new TeamsNotification(destination, message));

            return Task.CompletedTask;
        }
    }
}
