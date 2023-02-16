using System;
using Api.Database.Models;
using Api.Services.Models;
using Xunit;

namespace Api.Test.Services
{
    public class TestPose
    {
        [Fact]
        public void TestRotation()
        {
            var mockAngleAxisParameters = new EchoVector(1, 0, 0);
            float mockAngle = MathF.PI / 2;

            var expected = new Orientation() { X = 0.7071F, Y = 0, Z = 0, W = 0.7071F };

            Assert.Equal(expected.X, new Pose(mockAngleAxisParameters, mockAngleAxisParameters, mockAngle).Orientation.X, 3.0);
        }
        [Fact]
        public void AssertCoordinateConversion()
        {
            var pose = new Pose(new Position(25.041F, 23.682F, 0), new Orientation(0, 0, 0.8907533F, 0.4544871F), "robots");
            var predefinedPosition = pose.Position;
            var predefinedOrientation = pose.Orientation;
            var echoPose = ConvertPredefinedPoseToEchoPose(predefinedPosition, predefinedOrientation, "robot");

            var flotillaPose = new Pose(echoPose.Position, echoPose.Orientation.Axis, echoPose.Orientation.Angle);
            Assert.Equal(predefinedOrientation, flotillaPose.Orientation);
        }

        private EchoPose ConvertPredefinedPoseToEchoPose(Position position, Orientation orientation, string frame)
        {
            var enuPosition = new EchoVector(position.X, position.Y, position.Z);
            var axisAngle = ConvertOrientation(orientation);
            return new EchoPose(enuPosition, axisAngle, "robot");
        }
        private static AxisAngle ConvertOrientation(Orientation orientation)
        // This is the method used to convert predefined poses to the Angle-Axis representation used by
        {
            float qw = orientation.W;
            float angle = -2 * MathF.Acos(qw);
            if (orientation.Z >= 0)
                angle = 2 * MathF.Acos(qw);

            return new AxisAngle(new EchoVector(0, 0, 1), angle);
        }
        public class AxisAngle
        {
            public EchoVector Axis;
            public float Angle;
            public AxisAngle(EchoVector axis, float angle)
            {
                Axis = axis;
                Angle = angle;
            }
        }
        public class EchoPose
        {
            public EchoVector Position;
            public AxisAngle Orientation;
            public string Frame;

            public EchoPose(EchoVector position, AxisAngle orientation, string frame)
            {
                Position = position;
                Orientation = orientation;
                Frame = frame;
            }
        }
    }
}
