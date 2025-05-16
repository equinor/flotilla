using System;
using Api.Database.Models;
using Api.Services.Models;
using Xunit;

namespace Api.Test.Database
{
    public class TestPose
    {
        [Fact]
        public void TestRotationNorth()
        {
            var mockAngleAxisParameters = new EnuPosition(0, 0, 1);
            float mockAngle = 0;

            var expected = new Orientation
            {
                X = 0,
                Y = 0,
                Z = 0.7071F,
                W = 0.7071F,
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

            var expected = new Orientation
            {
                X = 0,
                Y = 0,
                Z = -0.7071F,
                W = 0.7071F,
            };

            var pose = new Pose(mockAngleAxisParameters, mockAngle);

            Assert.Equal(expected.Z, pose.Orientation.Z, 3.0);
        }

        [Fact]
        public void TestNegativaRotation()
        {
            var mockAngleAxisParameters = new EnuPosition(0, 0, 1);
            float mockAngle = -180F * MathF.PI / 180F;

            var expected = new Orientation
            {
                X = 0,
                Y = 0,
                Z = 1F,
                W = 0,
            };

            Assert.Equal(
                expected.Z,
                new Pose(mockAngleAxisParameters, mockAngle).Orientation.Z,
                3.0
            );
        }
    }
}
