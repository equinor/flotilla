namespace Api.Controllers.Models
{
    public struct UpdateRobotModelQuery
    {
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

        /// <summary>
        ///     Lower battery threshold at which to allow missions to be scheduled, in percentage
        /// </summary>
        public float? BatteryMissionStartThreshold { get; set; }
    }
}
