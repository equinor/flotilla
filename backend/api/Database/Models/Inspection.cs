using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Api.Services.Models;
using Microsoft.EntityFrameworkCore;
#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class Inspection
    {
        public Inspection()
        {
            InspectionType = SensorType.Image;
            InspectionTarget = new Position();
            AnalysisTypes = [];
        }

        public Inspection(
            SensorType sensorType,
            Position target,
            IList<AnalysisType> analysisTypes,
            float? videoDuration,
            string? taskDescription = null,
            AcousticInspectionMetadata? acousticInspectionMetadata = null
        )
        {
            InspectionType = sensorType;
            InspectionTarget = target;
            VideoDuration = videoDuration;
            AnalysisTypes = analysisTypes;
            TaskDescription = taskDescription;
            AcousticInspectionMetadata = acousticInspectionMetadata;
        }

        // Creates a blank deepcopy of the provided inspection
        public Inspection(Inspection copy, bool useEmptyID = false)
        {
            Id = useEmptyID ? "" : Guid.NewGuid().ToString();
            IsarInspectionId = useEmptyID ? "" : copy.IsarInspectionId;
            InspectionType = copy.InspectionType;
            VideoDuration = copy.VideoDuration;
            InspectionUrl = copy.InspectionUrl;
            InspectionTarget = new Position(copy.InspectionTarget);
            TaskDescription = copy.TaskDescription;
            AnalysisTypes = copy.AnalysisTypes;
            AcousticInspectionMetadata = copy.AcousticInspectionMetadata is not null
                ? new AcousticInspectionMetadata(copy.AcousticInspectionMetadata)
                : null;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string IsarInspectionId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public Position InspectionTarget { get; set; }

        public IList<AnalysisType>? AnalysisTypes { get; set; }

        [Required]
        public SensorType InspectionType { get; set; }

        public string? TaskDescription { get; set; }

        public AnalysisResult AnalysisResult { get; set; }

        public float? VideoDuration { get; set; }

        public AcousticInspectionMetadata? AcousticInspectionMetadata { get; set; }

        [MaxLength(250)]
        public string? InspectionUrl { get; set; }

        public void UpdateWithIsarInfo(IsarTask isarTask)
        {
            if (isarTask.IsarInspectionId != null)
            {
                IsarInspectionId = isarTask.IsarInspectionId;
            }
        }

        public bool IsSupportedSensorType(IList<RobotCapabilitiesEnum> capabilities)
        {
            return InspectionType switch
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
    public class AcousticInspectionMetadata(
        float frequencyFrom,
        float frequencyTo,
        float snrValueThreshold,
        AcousticDetectionType detectionType
    ) : IValidatableObject
    {
        public const float MaxAcousticFrequencyHz = 100_000f;

        [Required]
        [Range(0f, MaxAcousticFrequencyHz)]
        public float FrequencyFrom { get; set; } = frequencyFrom;

        [Required]
        [Range(0f, MaxAcousticFrequencyHz)]
        public float FrequencyTo { get; set; } = frequencyTo;

        [Required]
        public float SnrValueThreshold { get; set; } = snrValueThreshold;

        [Required]
        public AcousticDetectionType DetectionType { get; set; } = detectionType;

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
    public class Roi(int x, int y, int width, int height)
    {
        [Required]
        [Range(0, int.MaxValue)]
        public int X { get; set; } = x;

        [Required]
        [Range(0, int.MaxValue)]
        public int Y { get; set; } = y;

        [Required]
        [Range(1, int.MaxValue)]
        public int Width { get; set; } = width;

        [Required]
        [Range(1, int.MaxValue)]
        public int Height { get; set; } = height;

        public Roi(Roi copy)
            : this(copy.X, copy.Y, copy.Width, copy.Height) { }
    }
}
