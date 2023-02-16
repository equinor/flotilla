using System.ComponentModel.DataAnnotations;
using Api.Services.Models;
using Microsoft.EntityFrameworkCore;

#nullable disable
namespace Api.Database.Models
{
    [Owned]
    public class Orientation
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        public Orientation()
        {
            X = 0;
            Y = 0;
            Z = 0;
            W = 1;
        }

        public Orientation(float x = 0, float y = 0, float z = 0, float w = 1)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
        public override bool Equals(object obj)
        {
            if (obj is not Orientation)
                return false;
            const float tolerance = 1e-6F;
            Orientation orientation = (Orientation)obj;
            if (MathF.Abs(orientation.X - this.X) > tolerance)
            {
                return false;
            }
            if (MathF.Abs(orientation.Y - this.Y) > tolerance)
            {
                return false;
            }
            if (MathF.Abs(orientation.Z - this.Z) > tolerance)
            {
                return false;
            }
            if (MathF.Abs(orientation.W - this.W) > tolerance)
            {
                return false;
            }
            return true;
        }
    }

    [Owned]
    public class Position
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Position()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        public Position(float x = 0, float y = 0, float z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    [Owned]
    public class Pose
    {
        [Required]
        public Position Position { get; set; }

        [Required]
        public Orientation Orientation { get; set; }

        [MaxLength(200)]
        public string Frame { get; set; }
        private static float AxisAngleToQuaternionElement(float rotationAxis, float angle)
        {
            float quaterionElement = rotationAxis * MathF.Sin(angle / 2);
            return quaterionElement;
        }
        public Orientation AxisAngleToQuaternion(EchoVector axis, float angle)
        {
            var quaternion = new Orientation()
            {
                X = AxisAngleToQuaternionElement(axis.East, angle),
                Y = AxisAngleToQuaternionElement(axis.North, angle),
                Z = AxisAngleToQuaternionElement(axis.Up, angle),
                W = MathF.Cos(angle / 2)
            };
            return quaternion;
        }
        public Pose()
        {
            Position = new Position();
            Orientation = new Orientation();
            Frame = "defaultFrame";
        }

        public Pose(
            float x_pos,
            float y_pos,
            float z_pos,
            float x_ori,
            float y_ori,
            float z_ori,
            float w,
            string frame
        )
        {
            Position = new Position(x_pos, y_pos, z_pos);
            Orientation = new Orientation(x_ori, y_ori, z_ori, w);
            Frame = frame;
        }
        public Pose(
            EchoVector enuPosition,
            EchoVector axis,
            float angle
        )
        {
            Position = new Position(enuPosition.East, enuPosition.North, enuPosition.Up);
            Orientation = AxisAngleToQuaternion(axis, angle);
            Frame = "asset";
        }
        public Pose(
            Position position,
            Orientation orientation,
            string frame
        )
        {
            Position = position;
            Orientation = orientation;
            Frame = frame;
        }
    }
}
