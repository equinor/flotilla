using System.ComponentModel.DataAnnotations;
using Api.Database.Models;
using Api.Services.Models;

namespace Api.Controllers.Models
{
    public struct TaskQuery : IValidatableObject
    {
#nullable disable
        public TaskQuery() { }

#nullable enable
        public TaskQuery(TaskDefinition def)
        {
            TagId = def.TagId;
            Description = def.Description;
            RobotPose = def.RobotPose;
            TargetPosition = def.TargetPosition;
            ZoomDescription = def.ZoomDescription;
            SensorType = def.SensorType;
            AnalysisTypes = def.AnalysisTypes;
            VideoDuration = def.VideoDuration;
            AcousticInspectionMetadata = def.AcousticInspectionMetadata;
        }

        public string? TagId { get; set; }

        public string? Description { get; set; }

        public Pose RobotPose { get; set; }

        public Position TargetPosition { get; set; }

        public IsarZoomDescription? ZoomDescription { get; set; }

        public SensorType SensorType { get; set; }

        public IList<AnalysisType> AnalysisTypes { get; set; }

        public float? VideoDuration { get; set; }

        public AcousticInspectionMetadata? AcousticInspectionMetadata { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (SensorType == SensorType.AcousticMeasurement && AcousticInspectionMetadata is null)
            {
                yield return new ValidationResult(
                    $"{nameof(AcousticInspectionMetadata)} is required when {nameof(SensorType)} is {nameof(SensorType.AcousticMeasurement)}.",
                    [nameof(AcousticInspectionMetadata), nameof(SensorType)]
                );
            }
            else if (
                SensorType != SensorType.AcousticMeasurement
                && AcousticInspectionMetadata is not null
            )
            {
                yield return new ValidationResult(
                    $"{nameof(AcousticInspectionMetadata)} can only be set when {nameof(SensorType)} is {nameof(SensorType.AcousticMeasurement)}.",
                    [nameof(AcousticInspectionMetadata), nameof(SensorType)]
                );
            }
        }
    }

    public struct CreateMissionQuery
    {
        public string InstallationCode { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public List<TaskQuery> Tasks { get; set; }
    }
}
