using Api.Services.Events;
namespace Api.Services
{
    public interface IEmergencyActionService
    {
        public void TriggerEmergencyButtonPressedForRobot(EmergencyButtonPressedForRobotEventArgs e);

        public void TriggerEmergencyButtonDepressedForRobot(EmergencyButtonPressedForRobotEventArgs e);
    }

    public class EmergencyActionService : IEmergencyActionService
    {
        private readonly ILogger<EmergencyActionService> _logger;
        private readonly IRobotService _robotService;

        public EmergencyActionService(ILogger<EmergencyActionService> logger, IRobotService robotService)
        {
            _logger = logger;
            _robotService = robotService;
        }

        public void TriggerEmergencyButtonPressedForRobot(EmergencyButtonPressedForRobotEventArgs e)
        {
            OnEmergencyButtonPressedForRobot(e);
        }

        public void TriggerEmergencyButtonDepressedForRobot(EmergencyButtonPressedForRobotEventArgs e)
        {
            OnEmergencyButtonPressedForRobot(e);
        }

        public static event EventHandler<EmergencyButtonPressedForRobotEventArgs>? EmergencyButtonPressedForRobot;

        protected virtual void OnEmergencyButtonPressedForRobot(EmergencyButtonPressedForRobotEventArgs e)
        {
            EmergencyButtonPressedForRobot?.Invoke(this, e);
        }

        public static event EventHandler<EmergencyButtonPressedForRobotEventArgs>? EmergencyButtonDepressedForRobot;

        protected virtual void OnEmergencyButtonDepressedForRobot(EmergencyButtonPressedForRobotEventArgs e)
        {
            EmergencyButtonDepressedForRobot?.Invoke(this, e);
        }
    }
}
