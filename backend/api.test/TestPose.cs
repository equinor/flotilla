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
            var mockAngleAxisParameters = new EchoVector() { North = 0, East = 1, Up = 0 };
            float mockAngle = MathF.PI / 2;

            var expected = new Orientation() { X = 0.7071F, Y = 0, Z = 0, W = 0.7071F };

            Assert.Equal(expected.X, new Pose(enuPosition: mockAngleAxisParameters, axisAngleAxis: mockAngleAxisParameters, axisAngleAngle: mockAngle).Orientation.X, 3);
        }
    }
}
