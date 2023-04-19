namespace Api.Database.Models
{
    public class RobotModel

    {
        public enum RobotType
        {
            TaurobInspector,
            TaurobOperator,
            ExR2,
            Robot,
            Turtlebot,
            AnymalX,
            AnymalD,
        }

        float battery_warning_threshold;
        float pressure_warning_threshold;
    }

}
