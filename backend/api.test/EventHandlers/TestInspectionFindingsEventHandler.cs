using System;
using System.Globalization;
using Api.EventHandlers;
using Xunit;

namespace Api.Test.EventHandlers
{
    public class TestInspectionFindingsEventHandler
    {
        [Fact]
        public void TestGenerateReportFromFinding()
        {
            // Arrange
            var culture = CultureInfo.InvariantCulture;
            var finding1 = new Finding("Tag1", "Plant1", "Area1", "Finding1", DateTime.UtcNow, "Robot1");
            var finding2 = new Finding("Tag2", "Plant2", "Area2", "Finding2", DateTime.UtcNow, "Robot2");
            string expectedReport = $"Findings Report:{Environment.NewLine}- TagId: Tag1, PlantName: Plant1, AreaName: Area1, Description: Finding1, Timestamp: {DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss", culture)}, RobotName: Robot1{Environment.NewLine}- TagId: Tag2, PlantName: Plant2, AreaName: Area2, Description: Finding2, Timestamp: {DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss", culture)}, RobotName: Robot2{Environment.NewLine}";

            //Act
            string generatedReport = InspectionFindingEventHandler.GenerateReportFromFindingsReportsList([finding1, finding2]);
            Console.WriteLine(generatedReport);

            //Assert 
            Assert.Equal(expectedReport, generatedReport);
        }
    }
}
