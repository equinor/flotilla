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

            if (
                (SensorType == SensorType.Video || SensorType == SensorType.ThermalVideo)
                && VideoDuration is null
            )
            {
                yield return new ValidationResult(
                    $"{nameof(VideoDuration)} is required when {nameof(SensorType)} is {SensorType}.",
                    [nameof(VideoDuration), nameof(SensorType)]
                );
            }
        }
    }

    public struct CreateMissionQuery : IValidatableObject
    {
        public string InstallationCode { get; set; }

        public string Name { get; set; }

        public bool IsAdHoc { get; set; }

        [MaxLength(200)]
        public string? RobotMissionId { get; set; }

        public string? InspectionAreaId { get; set; }

        public List<TaskQuery> Tasks { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (RobotMissionId is not null)
            {
                if (string.IsNullOrWhiteSpace(RobotMissionId))
                    yield return new ValidationResult(
                        "RobotMissionId must not be blank.",
                        [nameof(RobotMissionId)]
                    );
                if (Tasks is null || Tasks.Count != 0)
                    yield return new ValidationResult(
                        "Robot-stored missions require an empty tasks list.",
                        [nameof(Tasks)]
                    );
                if (string.IsNullOrWhiteSpace(InspectionAreaId))
                    yield return new ValidationResult(
                        "Robot-stored missions require an inspection area.",
                        [nameof(InspectionAreaId)]
                    );
            }
            else
            {
                if (Tasks is null || Tasks.Count == 0)
                    yield return new ValidationResult(
                        "A mission definition must contain at least one task.",
                        [nameof(Tasks)]
                    );
                if (InspectionAreaId is not null)
                    yield return new ValidationResult(
                        "Inspection missions determine their area from their tasks.",
                        [nameof(InspectionAreaId)]
                    );
            }
        }
    }
}
