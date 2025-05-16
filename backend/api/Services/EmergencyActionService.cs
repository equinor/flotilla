using Api.Services.Events;

namespace Api.Services
{
    public interface IEmergencyActionService
    {
        public void SendRobotToDock(RobotEmergencyEventArgs e);

        public void ReleaseRobotFromDock(RobotEmergencyEventArgs e);
    }

    public class EmergencyActionService : IEmergencyActionService
    {
        public void SendRobotToDock(RobotEmergencyEventArgs e)
        {
            OnSendRobotToDockTriggered(e);
        }

        public void ReleaseRobotFromDock(RobotEmergencyEventArgs e)
        {
            OnReleaseRobotFromDockTriggered(e);
        }

        public static event EventHandler<RobotEmergencyEventArgs>? SendRobotToDockTriggered;

        protected virtual void OnSendRobotToDockTriggered(RobotEmergencyEventArgs e)
        {
            SendRobotToDockTriggered?.Invoke(this, e);
        }

        public static event EventHandler<RobotEmergencyEventArgs>? ReleaseRobotFromDockTriggered;

        protected virtual void OnReleaseRobotFromDockTriggered(RobotEmergencyEventArgs e)
        {
            ReleaseRobotFromDockTriggered?.Invoke(this, e);
        }
    }
}
