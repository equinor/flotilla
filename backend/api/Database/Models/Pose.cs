#nullable disable
using System.ComponentModel.DataAnnotations;
using Api.Mqtt.MessageModels;
using Api.Services.Models;
using Microsoft.EntityFrameworkCore;
namespace Api.Database.Models
{
    [Owned]
    public class Orientation
    {

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
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is not Orientation) { return false; }
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
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    [Owned]
    public class Pose
    {

        public Pose()
        {
            Position = new Position();
            Orientation = new Orientation();
        }

        public Pose(IsarPoseMqtt isarPose)
        {
            Position = new Position(isarPose.Position.X, isarPose.Position.Y, isarPose.Position.Z);
            Orientation = new Orientation(isarPose.Orientation.X, isarPose.Orientation.Y, isarPose.Orientation.Z, isarPose.Orientation.W);
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

        public Pose(EchoVector enuPosition, float echoAngle)
        {
            Position = new Position(enuPosition.East, enuPosition.North, enuPosition.Up);
            Orientation = AxisAngleToQuaternion(echoAngle);
        }

        public Pose(Position position, Orientation orientation)
        {
            Position = position;
            Orientation = orientation;
        }
        [Required]
        public Position Position { get; set; }

        [Required]
        public Orientation Orientation { get; set; }

        // Since this is a ground robot the only quaternion vector
        // that makes sense is up (0, 0, 1)
        // Echo representes North at 0deg and increases this value clockwise
        // Our representation has East at 0deg with rotations anti-clockwise
        public Orientation AxisAngleToQuaternion(float echoAngle)
        {
            float angle;
            echoAngle %= 2F * MathF.PI;

            if (echoAngle < 0) { echoAngle += 2F * MathF.PI; }

            angle = (450 * MathF.PI / 180) - echoAngle;

            var quaternion = new Orientation
            {
                X = 0,
                Y = 0,
                Z = MathF.Sin(angle / 2),
                W = MathF.Cos(angle / 2)
            };

            return quaternion;
        }
    }
}
