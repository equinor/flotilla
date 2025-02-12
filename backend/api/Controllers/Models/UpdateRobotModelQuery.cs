using System.ComponentModel.DataAnnotations;

namespace Api.Controllers.Models
{
    public struct UpdateRobotModelQuery
    {
        /// <summary>
        /// Lower battery warning threshold in percentage
        /// </summary>
        [Range(0, 100, ErrorMessage = "Value must be between 0 and 100")]
        public float? BatteryWarningThreshold { get; set; }

        /// <summary>
        /// Upper pressure warning threshold in Bar
        /// </summary>
        [Range(0, float.MaxValue, ErrorMessage = "Value must be a non-negative number")]
        public float? UpperPressureWarningThreshold { get; set; }

        /// <summary>
        /// Lower pressure warning threshold in Bar
        /// </summary>
        [Range(0, float.MaxValue, ErrorMessage = "Value must be a non-negative number")]
        public float? LowerPressureWarningThreshold { get; set; }

        /// <summary>
        ///     Lower battery threshold at which to allow missions to be scheduled, in percentage
        /// </summary>
        [Range(0, 100, ErrorMessage = "Value must be between 0 and 100")]
        public float? BatteryMissionStartThreshold { get; set; }
    }
}
