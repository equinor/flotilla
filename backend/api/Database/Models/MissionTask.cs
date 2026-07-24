using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Api.Services.Models;
using Microsoft.EntityFrameworkCore;
#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class MissionTask
    {
        // ReSharper disable once NotNullOrRequiredMemberIsNotInitialized
        public MissionTask() { }

        // Creates a copy of the provided task
        public MissionTask(MissionTask copy)
        {
            TaskOrder = copy.TaskOrder;
            TagId = copy.TagId;
            Description = copy.Description;
            RobotPose = new Pose(copy.RobotPose);
            Status = TaskStatus.NotStarted;
            IsarZoomDescription = copy.IsarZoomDescription;
            AnalysisTypes = copy.AnalysisTypes;
            SensorType = copy.SensorType;
            TargetPosition = copy.TargetPosition;
            VideoDuration = copy.VideoDuration;
            AcousticInspectionMetadata = copy.AcousticInspectionMetadata;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string IsarInspectionId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public int TaskOrder { get; set; }

        [MaxLength(200)]
        public string? TagId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public Pose RobotPose { get; set; }

        public IList<AnalysisType>? AnalysisTypes { get; set; } = [];

        private TaskStatus _status;

        [Required]
        public TaskStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                if (IsCompleted && EndTime is null)
                {
                    EndTime = DateTime.UtcNow;
                }

                if (_status is TaskStatus.InProgress && StartTime is null)
                {
                    StartTime = DateTime.UtcNow;
                }
            }
        }

        public bool IsCompleted =>
            _status
                is TaskStatus.Cancelled
                    or TaskStatus.Successful
                    or TaskStatus.Failed
                    or TaskStatus.PartiallySuccessful;

        public DateTime? StartTime { get; private set; }

        public DateTime? EndTime { get; private set; }

        public IsarZoomDescription? IsarZoomDescription { get; set; }

        public SensorType SensorType { get; set; }

        public Position TargetPosition { get; set; }

        public float? VideoDuration { get; set; }

        public AcousticInspectionMetadata? AcousticInspectionMetadata { get; set; }

        public string? ErrorDescription { get; set; }

        public bool IsSupportedSensorType(IList<RobotCapabilitiesEnum> capabilities)
        {
            return SensorType switch
            {
                SensorType.Image => capabilities.Contains(RobotCapabilitiesEnum.take_image),
                SensorType.ThermalImage => capabilities.Contains(
                    RobotCapabilitiesEnum.take_thermal_image
                ),
                SensorType.Video => capabilities.Contains(RobotCapabilitiesEnum.take_video),
                SensorType.ThermalVideo => capabilities.Contains(
                    RobotCapabilitiesEnum.take_thermal_video
                ),
                SensorType.CO2Measurement => capabilities.Contains(
                    RobotCapabilitiesEnum.take_co2_measurement
                ),
                SensorType.Audio => capabilities.Contains(RobotCapabilitiesEnum.record_audio),
                SensorType.AcousticMeasurement => capabilities.Contains(
                    RobotCapabilitiesEnum.take_acoustic_measurement
                ),
                _ => false,
            };
        }

        public void UpdateStatus(IsarTaskStatus isarStatus)
        {
            Status = isarStatus switch
            {
                IsarTaskStatus.NotStarted => TaskStatus.NotStarted,
                IsarTaskStatus.InProgress => TaskStatus.InProgress,
                IsarTaskStatus.Successful => TaskStatus.Successful,
                IsarTaskStatus.PartiallySuccessful => TaskStatus.PartiallySuccessful,
                IsarTaskStatus.Cancelled => TaskStatus.Cancelled,
                IsarTaskStatus.Paused => TaskStatus.Paused,
                IsarTaskStatus.Failed => TaskStatus.Failed,
                _ => throw new ArgumentException($"ISAR Task status '{isarStatus}' not supported"),
            };
        }
    }

    public enum TaskStatus
    {
        Successful,
        PartiallySuccessful,
        NotStarted,
        InProgress,
        Failed,
        Cancelled,
        Paused,
    }

    public enum SensorType
    {
        Image,
        ThermalImage,
        Video,
        ThermalVideo,
        Audio,
        CO2Measurement,
        AcousticMeasurement,
    }

    public enum AcousticDetectionType
    {
        [JsonStringEnumMemberName("leak")]
        Leak,
    }

    [Owned]
    public class AcousticInspectionMetadata : IValidatableObject
    {
        public const float MaxAcousticFrequencyHz = 100_000f;

        [JsonConstructor]
        public AcousticInspectionMetadata(
            float frequencyFrom,
            float frequencyTo,
            float snrValueThreshold,
            AcousticDetectionType detectionType
        )
        {
            FrequencyFrom = frequencyFrom;
            FrequencyTo = frequencyTo;
            SnrValueThreshold = snrValueThreshold;
            DetectionType = detectionType;
        }

        [Required]
        [Range(0f, MaxAcousticFrequencyHz)]
        public float FrequencyFrom { get; set; }

        [Required]
        [Range(0f, MaxAcousticFrequencyHz)]
        public float FrequencyTo { get; set; }

        [Required]
        public float SnrValueThreshold { get; set; }

        [Required]
        public AcousticDetectionType DetectionType { get; set; }

        public Roi? Roi { get; set; }

        public AcousticInspectionMetadata(AcousticInspectionMetadata copy)
            : this(copy.FrequencyFrom, copy.FrequencyTo, copy.SnrValueThreshold, copy.DetectionType)
        {
            Roi = copy.Roi is null ? null : new Roi(copy.Roi);
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (FrequencyFrom >= FrequencyTo)
            {
                yield return new ValidationResult(
                    $"{nameof(FrequencyFrom)} must be less than {nameof(FrequencyTo)}.",
                    [nameof(FrequencyFrom), nameof(FrequencyTo)]
                );
            }
        }
    }

    [Owned]
    public class Roi
    {
        [JsonConstructor]
        public Roi(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        [Required]
        [Range(0, int.MaxValue)]
        public int X { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Y { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Width { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Height { get; set; }

        public Roi(Roi copy)
            : this(copy.X, copy.Y, copy.Width, copy.Height) { }
    }
}
