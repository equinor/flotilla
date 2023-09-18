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

        public EmergencyActionService()
        {
        }

        public void TriggerEmergencyButtonPressedForRobot(EmergencyButtonPressedForRobotEventArgs e)
        {
            OnEmergencyButtonPressedForRobot(e);
        }

        public void TriggerEmergencyButtonDepressedForRobot(EmergencyButtonPressedForRobotEventArgs e)
        {
            OnEmergencyButtonDepressedForRobot(e);
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
