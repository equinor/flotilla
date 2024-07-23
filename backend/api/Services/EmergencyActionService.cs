using Api.Services.Events;
namespace Api.Services
{
    public interface IEmergencyActionService
    {
        public void SendRobotToSafezone(RobotEmergencyEventArgs e);

        public void ReleaseRobotFromSafezone(RobotEmergencyEventArgs e);
    }

    public class EmergencyActionService : IEmergencyActionService
    {

        public EmergencyActionService()
        {
        }

        public void SendRobotToSafezone(RobotEmergencyEventArgs e)
        {
            OnSendRobotToSafezoneTriggered(e);
        }

        public void ReleaseRobotFromSafezone(RobotEmergencyEventArgs e)
        {
            OnReleaseRobotFromSafezoneTriggered(e);
        }

        public static event EventHandler<RobotEmergencyEventArgs>? SendRobotToSafezoneTriggered;

        protected virtual void OnSendRobotToSafezoneTriggered(RobotEmergencyEventArgs e)
        {
            SendRobotToSafezoneTriggered?.Invoke(this, e);
        }

        public static event EventHandler<RobotEmergencyEventArgs>? ReleaseRobotFromSafezoneTriggered;

        protected virtual void OnReleaseRobotFromSafezoneTriggered(RobotEmergencyEventArgs e)
        {
            ReleaseRobotFromSafezoneTriggered?.Invoke(this, e);
        }

    }
}
