using System;
using System.Collections.Generic;
using Api.EventHandlers;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Api.Test.EventHandlers
{
    public class TestInspectionFindingEventHandler
    {
        [Fact]
        public void TestGenerateAdaptiveCard()
        {
            // Arrange
            int numberOfFindings = 2;
            var findingsReports = new List<Finding>
            {
                new("Tag1", "Plant1", ["InspectionGroup1"], "Finding1", DateTime.UtcNow),
                new("Tag2", "Plant1", ["InspectoinGroup2"], "Finding2", DateTime.UtcNow),
            };

            // Act
            string adaptiveCardJson = InspectionFindingEventHandler.GenerateAdaptiveCard(
                "Rapport",
                numberOfFindings,
                findingsReports
            );

            // Assert
            Assert.NotNull(adaptiveCardJson);
            Assert.NotEmpty(adaptiveCardJson);
            JToken.Parse(adaptiveCardJson);
        }
    }
}
