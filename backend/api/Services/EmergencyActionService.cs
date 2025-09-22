using Api.Services.Events;

namespace Api.Services
{
    public interface IEmergencyActionService
    {
        public void LockdownRobot(RobotEmergencyEventArgs e);

        public void ReleaseRobotFromLockdown(RobotEmergencyEventArgs e);
    }

    public class EmergencyActionService : IEmergencyActionService
    {
        public void LockdownRobot(RobotEmergencyEventArgs e)
        {
            OnLockdownRobotTriggered(e);
        }

        public void ReleaseRobotFromLockdown(RobotEmergencyEventArgs e)
        {
            OnReleaseRobotFromLockdownTriggered(e);
        }

        public static event EventHandler<RobotEmergencyEventArgs>? LockdownRobotTriggered;

        protected virtual void OnLockdownRobotTriggered(RobotEmergencyEventArgs e)
        {
            LockdownRobotTriggered?.Invoke(this, e);
        }

        public static event EventHandler<RobotEmergencyEventArgs>? ReleaseRobotFromLockdownTriggered;

        protected virtual void OnReleaseRobotFromLockdownTriggered(RobotEmergencyEventArgs e)
        {
            ReleaseRobotFromLockdownTriggered?.Invoke(this, e);
        }
    }
}
