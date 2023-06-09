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
            const float Tolerance = 1e-6F;
            var orientation = (Orientation)obj;
            if (MathF.Abs(orientation.X - X) > Tolerance)
            {
                return false;
            }
            if (MathF.Abs(orientation.Y - Y) > Tolerance)
            {
                return false;
            }
            if (MathF.Abs(orientation.Z - Z) > Tolerance)
            {
                return false;
            }
            if (MathF.Abs(orientation.W - W) > Tolerance)
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
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

        // Since this is a ground robot the only quaternion vector 
        // that makes sense is up (0, 0, 1)
        public Orientation AxisAngleToQuaternion(float angle)
        {
            var quaternion = new Orientation()
            {
                X = 0,
                Y = 0,
                Z = MathF.Sin(angle / 2),
                W = MathF.Cos(angle / 2)
            };
            return quaternion;
        }

        public Pose()
        {
            Position = new Position();
            Orientation = new Orientation();
        }

        public Pose(
            float x_pos,
            float y_pos,
            float z_pos,
            float x_ori,
            float y_ori,
            float z_ori,
            float w
        )
        {
            Position = new Position(x_pos, y_pos, z_pos);
            Orientation = new Orientation(x_ori, y_ori, z_ori, w);
        }

        public Pose(EchoVector enuPosition, float angle)
        {
            float clockAngle = -angle;
            Position = new Position(enuPosition.East, enuPosition.North, enuPosition.Up);
            Orientation = AxisAngleToQuaternion(clockAngle);
        }

        public Pose(Position position, Orientation orientation)
        {
            Position = position;
            Orientation = orientation;
        }
    }
}
