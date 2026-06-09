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

        [Fact]
        public void AcousticIsOmittedWhenMetadataIsNull()
        {
            var inspection = new Inspection(
                SensorType.Image,
                new Position(0, 0, 0),
                [],
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

            Assert.DoesNotContain("acoustic", json);
        }

        [Fact]
        public void AcousticSerialisesWithSnakeCaseFieldNamesAndLowercaseDetectionType()
        {
            var inspection = new Inspection(
                SensorType.AcousticMeasurement,
                new Position(0, 0, 0),
                [],
                videoDuration: null,
                acousticInspectionMetadata: new AcousticInspectionMetadata(
                    100f,
                    200f,
                    3.0f,
                    AcousticDetectionType.Leak
                )
            );
            var task = new MissionTask
            {
                TaskOrder = 0,
                RobotPose = new Pose(),
                Status = TaskStatus.NotStarted,
                Inspection = inspection,
            };

            var json = JsonSerializer.Serialize(new IsarInspectionDefinition(task));

            Assert.Contains("\"frequency_from\":100", json);
            Assert.Contains("\"frequency_to\":200", json);
            Assert.Contains("\"snr_value_threshold\":3", json);
            Assert.Contains("\"detection_type\":\"leak\"", json);
        }

        [Fact]
        public void RoiIsOmittedWhenNull()
        {
            var inspection = new Inspection(
                SensorType.AcousticMeasurement,
                new Position(0, 0, 0),
                [],
                videoDuration: null,
                acousticInspectionMetadata: new AcousticInspectionMetadata(
                    100f,
                    200f,
                    3.0f,
                    AcousticDetectionType.Leak
                )
            );
            var task = new MissionTask
            {
                TaskOrder = 0,
                RobotPose = new Pose(),
                Status = TaskStatus.NotStarted,
                Inspection = inspection,
            };

            var json = JsonSerializer.Serialize(new IsarInspectionDefinition(task));

            Assert.DoesNotContain("\"roi\"", json);
        }

        [Fact]
        public void RoiSerialisesWithSnakeCaseFields()
        {
            var inspection = new Inspection(
                SensorType.AcousticMeasurement,
                new Position(0, 0, 0),
                [],
                videoDuration: null,
                acousticInspectionMetadata: new AcousticInspectionMetadata(
                    100f,
                    200f,
                    3.0f,
                    AcousticDetectionType.Leak
                )
                {
                    Roi = new Roi(760, 400, 133, 160),
                }
            );
            var task = new MissionTask
            {
                TaskOrder = 0,
                RobotPose = new Pose(),
                Status = TaskStatus.NotStarted,
                Inspection = inspection,
            };

            var json = JsonSerializer.Serialize(new IsarInspectionDefinition(task));

            Assert.Contains("\"roi\":{\"x\":760,\"y\":400,\"width\":133,\"height\":160}", json);
        }
    }
}
