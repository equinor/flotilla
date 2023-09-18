using Api.Services;
using Api.Services.Events;
using Api.Utilities;
namespace Api.EventHandlers
{
    public class EmergencyActionEventHandler : EventHandlerBase
    {
        public EmergencyActionEventHandler()
        {
            Subscribe();
        }

        public override void Subscribe()
        {
            EmergencyActionService.EmergencyButtonPressedForRobot += OnEmergencyButtonPressedForRobot;
        }

        public override void Unsubscribe()
        {
            EmergencyActionService.EmergencyButtonPressedForRobot -= OnEmergencyButtonPressedForRobot;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await stoppingToken;
        }

        private void OnEmergencyButtonPressedForRobot(object? sender, EmergencyButtonPressedForRobotEventArgs e) { }
    }
}
