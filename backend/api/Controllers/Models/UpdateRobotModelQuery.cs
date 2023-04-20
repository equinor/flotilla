namespace Api.Controllers.Models
{
    public struct UpdateRobotModelQuery
    {
        /// <summary>
        /// Lower battery warning threshold in percentage
        /// </summary>
        public float? BatteryWarningThreshold { get; set; }

        /// <summary>
        /// Upper pressure warning threshold in mBar
        /// </summary>
        public float? UpperPressureWarningThreshold { get; set; }

        /// <summary>
        /// Lower pressure warning threshold in mBar
        /// </summary>
        public float? LowerPressureWarningThreshold { get; set; }
    }
}
