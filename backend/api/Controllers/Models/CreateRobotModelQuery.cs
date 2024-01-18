using Api.Database.Models;

namespace Api.Controllers.Models
{
    public struct CreateRobotModelQuery
    {
        /// <summary>
        /// The type of robot model
        /// </summary>
        public RobotType RobotType { get; set; }

        /// <summary>
        /// Lower battery warning threshold in percentage
        /// </summary>
        public float? BatteryWarningThreshold { get; set; }

        /// <summary>
        /// Upper pressure warning threshold in Bar
        /// </summary>
        public float? UpperPressureWarningThreshold { get; set; }

        /// <summary>
        /// Lower pressure warning threshold in Bar
        /// </summary>
        public float? LowerPressureWarningThreshold { get; set; }
    }
}
