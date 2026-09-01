using Api.Services;
using Api.Services.Events;
using Api.Utilities;

namespace Api.EventHandlers
{
    public class TeamsMessageEventHandler : EventHandlerBase
    {
        private readonly ILogger<TeamsMessageEventHandler> _logger;
        private readonly EventAggregatorSingletonService _eventAggregatorSingletonService;
        private readonly ITeamsNotificationService _teamsNotificationService;

        public TeamsMessageEventHandler(
            ILogger<TeamsMessageEventHandler> logger,
            EventAggregatorSingletonService eventAggregatorSingletonService,
            ITeamsNotificationService teamsNotificationService
        )
        {
            _logger = logger;
            _eventAggregatorSingletonService = eventAggregatorSingletonService;
            _teamsNotificationService = teamsNotificationService;

            Subscribe();
        }

        public override void Subscribe()
        {
            _eventAggregatorSingletonService.Subscribe<TeamsMessageEventArgs>(
                OnTeamsMessageReceived
            );
        }

        public override void Unsubscribe() { }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await stoppingToken;
        }

        private async void OnTeamsMessageReceived(TeamsMessageEventArgs e)
        {
            try
            {
                await _teamsNotificationService.SendTeamsMessageAsync(
                    e.TeamsMessage,
                    TeamsDestination.SystemAlerts
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send Teams message to {Destination}",
                    TeamsDestination.SystemAlerts
                );
            }
        }
    }
}
