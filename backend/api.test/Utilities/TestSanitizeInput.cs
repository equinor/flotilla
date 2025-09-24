using Api.Database.Models;
using Api.Utilities;
using Xunit;

namespace Api.Test.Controllers
{
    public class SanitizeTest
    {
        [Fact]
        public void CheckThatGuidStringIsUnchangedBySanitizing()
        {
            // Arrange
            var guid = "78e2ef98-5c0b-487f-8624-443b54ff68aa";

            // Act
            var sanitizedGuid = Sanitize.SanitizeUserInput(guid);

            // Assert
            Assert.Equal(guid, sanitizedGuid);
        }

        [Fact]
        public void CheckThatInvalidStringIsChangedBySanitizing()
        {
            // Arrange
            var invalidInput = "78e2ef98-5c0b-487f-8624-443b54ff6\n8aa";

            // Act
            var sanitizedInvalidInput = Sanitize.SanitizeUserInput(invalidInput);

            // Assert
            Assert.NotEqual(invalidInput, sanitizedInvalidInput);
        }

        [Fact]
        public void CheckThatUpdateRobotQueryIsUnchangedBySanitizing()
        {
            // Arrange
            var inspectionAreaId = "d0021e3a-d26b-4029-9a97-dbaf69da06ea";
            var pose = new Pose();
            var missionId = "a15085ca-39d8-4227-bf90-8d4dae555e10";

            var query = new UpdateRobotQuery
            {
                InspectionAreaId = inspectionAreaId,
                MissionId = missionId,
            };

            // Act
            var sanitizedQuery = Sanitize.SanitizeUserInput(query);

            // Assert
            Assert.Equal(inspectionAreaId, sanitizedQuery.InspectionAreaId);
            Assert.Equal(missionId, sanitizedQuery.MissionId);
        }

        [Fact]
        public void CheckThatUpdateRobotQueryWithNullValuesIsUnchangedBySanitizing()
        {
            // Arrange
            var inspectionAreaId = "d0021e3a-d26b-4029-9a97-dbaf69da06ea";
            var pose = new Pose();
            string? missionId = null;

            var query = new UpdateRobotQuery
            {
                InspectionAreaId = inspectionAreaId,
                MissionId = missionId,
            };

            // Act
            var sanitizedQuery = Sanitize.SanitizeUserInput(query);

            // Assert
            Assert.Equal(inspectionAreaId, sanitizedQuery.InspectionAreaId);
            Assert.Equal(missionId, sanitizedQuery.MissionId);
        }

        [Fact]
        public void CheckThatUpdateRobotQueryWithInvalidValueIsChangedBySanitizing()
        {
            // Arrange
            var inspectionAreaId = "d0021e3a-d26b-4029-9a\n97-dbaf69da06ea";
            var pose = new Pose();
            var missionId = "a15085ca-39d8-4\r227-bf90-8d4dae555e10";

            var query = new UpdateRobotQuery
            {
                InspectionAreaId = inspectionAreaId,
                MissionId = missionId,
            };

            // Act
            var sanitizedQuery = Sanitize.SanitizeUserInput(query);

            // Assert
            Assert.NotEqual(inspectionAreaId, sanitizedQuery.InspectionAreaId);
            Assert.NotEqual(missionId, sanitizedQuery.MissionId);
        }
    }
}
