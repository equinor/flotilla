using Api.Services;
using Api.Services.Events;
using Api.Utilities;
namespace Api.EventHandlers
{
    public class MissionEventHandler : EventHandlerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MissionEventHandler> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public MissionEventHandler(
            ILogger<MissionEventHandler> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration config
        )
        {
            _logger = logger;
            _configuration = config;
            // Reason for using factory: https://www.thecodebuzz.com/using-dbcontext-instance-in-ihostedservice/
            _scopeFactory = scopeFactory;

            Subscribe();
        }

        private IServiceProvider GetServiceProvider()
        {
            return _scopeFactory.CreateScope().ServiceProvider;
        }

        public override void Subscribe()
        {
            MissionRunService.MissionRunCreated += OnMissionRunCreated;
        }

        public override void Unsubscribe()
        {
            MissionRunService.MissionRunCreated -= OnMissionRunCreated;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await stoppingToken;
        }

        private async void OnMissionRunCreated(object? sender, MissionRunCreatedEventArgs e)
        {
            Console.WriteLine("Hello World");
            _logger.LogError("Hello, is it me you're looking for?");
        }
    }
}
