using System.Text.Json;
using Api.Database.Models;
using Api.Services.Models;
using Xunit;

namespace Api.Test.Services.Models
{
    public class IsarMissionDefinitionTests
    {
        [Fact]
        public void AnalysisTypesSerialiseAsSnakeCaseSaraKeys()
        {
            var inspection = new Inspection(
                SensorType.Image,
                new Position(0, 0, 0),
                [
                    AnalysisType.Fencilla,
                    AnalysisType.CLOE,
                    AnalysisType.ThermalReading,
                    AnalysisType.CO2,
                ],
                videoDuration: null
            );
            var task = new MissionTask
            {
                TaskOrder = 0,
                RobotPose = new Pose(),
                Status = TaskStatus.NotStarted,
                Inspection = inspection,
            };

            var json = JsonSerializer.Serialize(new IsarInspectionDefinition(task));

            Assert.Contains(
                "\"analysis_types\":[\"fencilla\",\"cloe\",\"thermal-reading\",\"co2\"]",
                json
            );
        }

        [Fact]
        public void ToMissionRunTaskPopulatesAnalysisTypesOnBothMissionTaskAndInspection()
        {
            var def = new TaskDefinition
            {
                Index = 0,
                RobotPose = new Pose(),
                TargetPosition = new Position(0, 0, 0),
                SensorType = SensorType.Image,
                AnalysisTypes = [AnalysisType.Fencilla],
            };

            var task = def.ToMissionRunTask();

            Assert.Equal([AnalysisType.Fencilla], task.AnalysisTypes);
            Assert.Equal([AnalysisType.Fencilla], task.Inspection!.AnalysisTypes);
        }
    }
}
