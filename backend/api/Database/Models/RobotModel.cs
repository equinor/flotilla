using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Controllers.Models;
#pragma warning disable CS8618
namespace Api.Database.Models
{
    /// <summary>
    ///     The type of robot model
    /// </summary>
    public enum RobotType
    {
        /// WARNING:
        /// Changing the names of these enum options is the same as changing their value,
        /// so it will require updating the database with the new names because the enum
        /// is stored as strings in database
        TaurobInspector,
        TaurobOperator,
        Robot,
        Turtlebot,
        AnymalX,
        AnymalD,
    }

    public class RobotModel
    {
        public RobotModel() { }

        public RobotModel(CreateRobotModelQuery query)
        {
            Type = query.RobotType;
            BatteryWarningThreshold = query.BatteryWarningThreshold;
            UpperPressureWarningThreshold = query.UpperPressureWarningThreshold;
            LowerPressureWarningThreshold = query.LowerPressureWarningThreshold;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        /// <summary>
        ///     The type of robot model
        /// </summary>
        [Required]
        public RobotType Type { get; set; }

        /// <summary>
        ///     Lower battery warning threshold in percentage
        /// </summary>
        public float? BatteryWarningThreshold { get; set; }

        /// <summary>
        ///     Upper pressure warning threshold in Bar
        /// </summary>
        public float? UpperPressureWarningThreshold { get; set; }

        /// <summary>
        ///     Lower pressure warning threshold in Bar
        /// </summary>
        public float? LowerPressureWarningThreshold { get; set; }

        /// <summary>
        ///     Lower battery threshold at which to allow missions to be scheduled, in percentage
        /// </summary>
        public float? BatteryMissionStartThreshold { get; set; }

        /// <summary>
        ///     The average time in seconds spent by this model on a single tag (excluding recording duration for video/audio)
        /// </summary>
        public float? AverageDurationPerTag { get; set; }

        public void Update(UpdateRobotModelQuery updateQuery)
        {
            BatteryWarningThreshold = updateQuery.BatteryWarningThreshold;
            UpperPressureWarningThreshold = updateQuery.UpperPressureWarningThreshold;
            LowerPressureWarningThreshold = updateQuery.LowerPressureWarningThreshold;
            BatteryMissionStartThreshold = updateQuery.BatteryMissionStartThreshold;
        }
    }
}
