using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Utilities;
using Xunit;

namespace Api.Test.Controllers
{
    public class AtexPressureValidationTests
    {
        [Theory]
        [InlineData(null, null, true)]
        [InlineData(0.1f, null, true)]
        [InlineData(null, 0.3f, true)]
        [InlineData(0.1f, 0.3f, true)]
        [InlineData(0.3f, 0.3f, false)]
        [InlineData(0.4f, 0.3f, false)]
        [InlineData(float.NaN, 0.3f, false)]
        [InlineData(0.1f, float.PositiveInfinity, false)]
        public void ValidatesPressureLimits(float? lower, float? upper, bool valid)
        {
            Assert.Equal(valid, !AtexPressureValidation.Validate(lower, upper).Any());
        }

        [Fact]
        public void CreateAndUpdateModelsValidatePressureLimits()
        {
            object query = new CreateRobotQuery
            {
                LowerPressureWarningThreshold = 0.3f,
                UpperPressureWarningThreshold = 0.1f,
            };
            var results = new List<ValidationResult>();
            Assert.False(
                Validator.TryValidateObject(query, new ValidationContext(query), results, true)
            );

            var robot = new Robot
            {
                LowerPressureWarningThreshold = 0.3f,
                UpperPressureWarningThreshold = 0.1f,
            };
            Assert.NotEmpty(robot.Validate(new ValidationContext(robot)));
        }

        [Fact]
        public void MapsExplicitConfigurationWithoutInferringAtexFromRobotType()
        {
            var query = new CreateRobotQuery
            {
                RobotType = RobotType.TaurobInspector,
                Documentation = [],
                IsAtex = true,
                LowerPressureWarningThreshold = 0.1f,
                UpperPressureWarningThreshold = 0.3f,
            };
            var robot = new Robot(query, new Installation());
            var response = new RobotResponse(robot);
            Assert.True(response.IsAtex);
            Assert.Equal(
                query.LowerPressureWarningThreshold,
                response.LowerPressureWarningThreshold
            );
            Assert.Equal(
                query.UpperPressureWarningThreshold,
                response.UpperPressureWarningThreshold
            );
            Assert.False(new Robot().IsAtex);

            query.IsAtex = false;
            Assert.False(new Robot(query, new Installation()).IsAtex);
        }
    }
}
