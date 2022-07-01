using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable
namespace Api.Database.Models
{
    public class IsarStep
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public string Id { get; set; }

        [Required]
        public string IsarStepId { get; set; }

        [Required]
        public virtual IsarTask Task { get; set; }

        public string TagId { get; set; }

        [Required]
        public IsarStepStatus StepStatus { get; set; }

        [Required]
        public StepType StepType { get; set; }

        public InspectionType InspectionType { get; set; }

        [Required]
        public DateTimeOffset Time { get; set; }

        [MaxLength(128)]
        public string FileLocation { get; set; }
    }

    public enum IsarStepStatus
    {
        Successful,
        InProgress,
        NotStarted,
        Failed,
        Cancelled
    }

    public static class IsarStepStatusMethods
    {
        public static IsarStepStatus FromString(string status) =>
            status switch
            {
                "successful" => IsarStepStatus.Successful,
                "not_started" => IsarStepStatus.NotStarted,
                "in_progress" => IsarStepStatus.InProgress,
                "failed" => IsarStepStatus.Failed,
                "cancelled" => IsarStepStatus.Cancelled,
                _
                    => throw new ArgumentException(
                        $"Failed to parse report status {status} as it's not supported"
                    )
            };
    }

    public enum StepType
    {
        DriveToPose,
        TakeImage,
        TakeThermalImage,
        RecordAudio
    }

    public class SelectStepType
    {
        public static StepType FromSensorTypeAsString(string sensorType)
        {
            return sensorType switch
            {
                "DriveToPose" => StepType.DriveToPose,
                "RecordAudio" => StepType.RecordAudio,
                "TakeImage" => StepType.TakeImage,
                "TakeThermalImage" => StepType.TakeThermalImage,
                _ => StepType.TakeImage,
            };
        }
    }

    public enum InspectionType
    {
        Image,
        ThermalImage,
        Audio
    }

    public class SelectInspectionType
    {
        public static InspectionType FromSensorTypeAsString(string sensorType)
        {
            return sensorType switch
            {
                "Picture" => InspectionType.Image,
                "ThermicPicture" => InspectionType.ThermalImage,
                "Audio" => InspectionType.Audio,
                "TakeImage" => InspectionType.Image,
                "TakeThermalImage" => InspectionType.ThermalImage,
                _ => InspectionType.Image,
            };
        }
    }
}
