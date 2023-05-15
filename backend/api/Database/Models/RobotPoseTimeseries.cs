using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Api.Database.Models
{
    // Cannot use Pose as owned entity in keyless entity
    // https://learn.microsoft.com/en-us/ef/core/modeling/keyless-entity-types?tabs=data-annotations
    [Keyless]
    public class RobotPoseTimeseries : TimeseriesBase
    {
        [Required]
        public float PositionX { get; set; }

        [Required]
        public float PositionY { get; set; }

        [Required]
        public float PositionZ { get; set; }

        [Required]
        public float OrientationX { get; set; }

        [Required]
        public float OrientationY { get; set; }

        [Required]
        public float OrientationZ { get; set; }

        [Required]
        public float OrientationW { get; set; }

        public RobotPoseTimeseries(Pose pose)
        {
            PositionX = pose.Position.X;
            PositionY = pose.Position.Y;
            PositionZ = pose.Position.Z;
            OrientationX = pose.Orientation.X;
            OrientationY = pose.Orientation.Y;
            OrientationZ = pose.Orientation.Z;
            OrientationW = pose.Orientation.W;
        }

        public RobotPoseTimeseries() { }
    }
}
