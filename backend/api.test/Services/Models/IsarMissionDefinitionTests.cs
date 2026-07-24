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
            var task = new MissionTask
            {
                TaskOrder = 0,
                RobotPose = new Pose(),
                Status = TaskStatus.NotStarted,
                SensorType = SensorType.Image,
                TargetPosition = new Position(0, 0, 0),
                AnalysisTypes =
                [
                    AnalysisType.Fencilla,
                    AnalysisType.CLOE,
                    AnalysisType.ThermalReading,
                    AnalysisType.CO2,
                ],
                VideoDuration = null,
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
        }

        [Fact]
        public void AcousticIsOmittedWhenMetadataIsNull()
        {
            var task = new MissionTask
            {
                TaskOrder = 0,
                RobotPose = new Pose(),
                Status = TaskStatus.NotStarted,
                SensorType = SensorType.Image,
                TargetPosition = new Position(0, 0, 0),
                AnalysisTypes = [],
                VideoDuration = null,
            };

            var json = JsonSerializer.Serialize(new IsarInspectionDefinition(task));

            Assert.DoesNotContain("acoustic", json);
        }

        [Fact]
        public void AcousticSerialisesWithSnakeCaseFieldNamesAndLowercaseDetectionType()
        {
            var task = new MissionTask
            {
                TaskOrder = 0,
                RobotPose = new Pose(),
                Status = TaskStatus.NotStarted,
                SensorType = SensorType.AcousticMeasurement,
                TargetPosition = new Position(0, 0, 0),
                AnalysisTypes = [],
                VideoDuration = null,
                AcousticInspectionMetadata = new AcousticInspectionMetadata(
                    100f,
                    200f,
                    3.0f,
                    AcousticDetectionType.Leak
                ),
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
            var task = new MissionTask
            {
                TaskOrder = 0,
                RobotPose = new Pose(),
                Status = TaskStatus.NotStarted,
                SensorType = SensorType.AcousticMeasurement,
                TargetPosition = new Position(0, 0, 0),
                AnalysisTypes = [],
                VideoDuration = null,
                AcousticInspectionMetadata = new AcousticInspectionMetadata(
                    100f,
                    200f,
                    3.0f,
                    AcousticDetectionType.Leak
                ),
            };

            var json = JsonSerializer.Serialize(new IsarInspectionDefinition(task));

            Assert.DoesNotContain("\"roi\"", json);
        }

        [Fact]
        public void RoiSerialisesWithSnakeCaseFields()
        {
            var task = new MissionTask
            {
                TaskOrder = 0,
                RobotPose = new Pose(),
                Status = TaskStatus.NotStarted,
                SensorType = SensorType.AcousticMeasurement,
                TargetPosition = new Position(0, 0, 0),
                AnalysisTypes = [],
                VideoDuration = null,
                AcousticInspectionMetadata = new AcousticInspectionMetadata(
                    100f,
                    200f,
                    3.0f,
                    AcousticDetectionType.Leak
                )
                {
                    Roi = new Roi(760, 400, 133, 160),
                },
            };

            var json = JsonSerializer.Serialize(new IsarInspectionDefinition(task));

            Assert.Contains("\"roi\":{\"x\":760,\"y\":400,\"width\":133,\"height\":160}", json);
        }
    }
}
