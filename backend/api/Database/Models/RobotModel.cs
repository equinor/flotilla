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
        ///     The average time in seconds spent by this model on a single tag (excluding recording duration for video/audio)
        /// </summary>
        public float? AverageDurationPerTag { get; set; }
    }
}
