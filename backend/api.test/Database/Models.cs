using System;
using Api.Database.Models;
using Api.Services.Models;
using Xunit;

namespace Api.Test.Services
{
    public class TestPose
    {
        [Fact]
        public void TestRotationNorth()
        {
            var mockAngleAxisParameters = new EnuPosition(0, 0, 1);
            float mockAngle = 0;

            var expected = new Orientation()
            {
                X = 0,
                Y = 0,
                Z = 0.7071F,
                W = 0.7071F
            };

            Assert.Equal(
                expected.Z,
                new Pose(mockAngleAxisParameters, mockAngle).Orientation.Z,
                3.0
            );
        }

        [Fact]
        public void TestRotationSouth()
        {
            var mockAngleAxisParameters = new EnuPosition(0, 0, 1);
            float mockAngle = MathF.PI;

            var expected = new Orientation()
            {
                X = 0,
                Y = 0,
                Z = -0.7071F,
                W = 0.7071F
            };

            var pose = new Pose(mockAngleAxisParameters, mockAngle);

            Assert.Equal(
                expected.Z,
                pose.Orientation.Z,
                3.0
            );
        }

        [Fact]
        public void TestNegativaRotation()
        {
            var mockAngleAxisParameters = new EnuPosition(0, 0, 1);
            float mockAngle = -180F * MathF.PI / 180F;

            var expected = new Orientation()
            {
                X = 0,
                Y = 0,
                Z = 1F,
                W = 0
            };

            Assert.Equal(
                expected.Z,
                new Pose(mockAngleAxisParameters, mockAngle).Orientation.Z,
                3.0
            );
        }

        [Fact]
        public void AssertCoordinateConversion()
        {
            var pose = new Pose(
                new Position(25.041F, 23.682F, 0),
                new Orientation(0, 0, 0.8907533F, 0.4544871F)
            );
            var predefinedPosition = pose.Position;
            var predefinedOrientation = pose.Orientation;
            var echoPose = ConvertPredefinedPoseToEchoPose(
                predefinedPosition,
                predefinedOrientation
            );

            var flotillaPose = new Pose(echoPose.Position, echoPose.Orientation.Angle);
            Assert.Equal(predefinedOrientation, flotillaPose.Orientation);
        }

        private static EchoPose ConvertPredefinedPoseToEchoPose(
            Position position,
            Orientation orientation
        )
        {
            var enuPosition = new EnuPosition(position.X, position.Y, position.Z);
            var axisAngle = ConvertOrientation(orientation);
            return new EchoPose(enuPosition, axisAngle);
        }

        private static AxisAngle ConvertOrientation(Orientation orientation)
        // This is the method used to convert predefined poses to the Angle-Axis representation used by Echo
        {
            float qw = orientation.W;
            float angle = -2 * MathF.Acos(qw);
            if (orientation.Z >= 0)
                angle = 2 * MathF.Acos(qw);

            angle = (450 * MathF.PI / 180) - angle;

            angle %= 2F * MathF.PI;

            if (angle < 0) angle += 2F * MathF.PI;

            return new AxisAngle(new EnuPosition(0, 0, 1), angle);
        }

        public class AxisAngle
        {
            public EnuPosition Axis;
            public float Angle;

            public AxisAngle(EnuPosition axis, float angle)
            {
                Axis = axis;
                Angle = angle;
            }
        }

        public class EchoPose
        {
            public EnuPosition Position;
            public AxisAngle Orientation;

            public EchoPose(EnuPosition position, AxisAngle orientation)
            {
                Position = position;
                Orientation = orientation;
            }
        }
    }
}
