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
        public StepTypeEnum StepType { get; set; }

        public InspectionTypeEnum InspectionType { get; set; }

        [Required]
        public DateTimeOffset Time { get; set; }

        [MaxLength(128)]
        public string FileLocation { get; set; }

        public enum IsarStepStatus
        {
            Successful,
            InProgress,
            NotStarted,
            Failed,
            Cancelled
        }

        public enum StepTypeEnum
        {
            DriveToPose,
            TakeImage,
            TakeVideo,
            TakeThermalImage,
            TakeThermalVideo,
            RecordAudio
        }

        public enum InspectionTypeEnum
        {
            Image,
            ThermalImage,
            Video,
            ThermalVideo,
            Audio
        }

        public static IsarStepStatus StatusFromString(string status)
        {
            return status switch
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
        public static StepTypeEnum StepTypeFromString(string sensorType)
        {
            return sensorType switch
            {
                "DriveToPose" => StepTypeEnum.DriveToPose,
                "RecordAudio" => StepTypeEnum.RecordAudio,
                "TakeImage" => StepTypeEnum.TakeImage,
                "TakeVideo" => StepTypeEnum.TakeVideo,
                "TakeThermalImage" => StepTypeEnum.TakeThermalImage,
                "TakeThermalVideo" => StepTypeEnum.TakeThermalVideo,
                _ => StepTypeEnum.TakeImage,
            };
        }
        public static InspectionTypeEnum InspectionTypeFromString(string sensorType)
        {
            return sensorType switch
            {
                "Picture" => InspectionTypeEnum.Image,
                "ThermicPicture" => InspectionTypeEnum.ThermalImage,
                "Audio" => InspectionTypeEnum.Audio,
                "TakeImage" => InspectionTypeEnum.Image,
                "TakeVideo" => InspectionTypeEnum.Video,
                "TakeThermalImage" => InspectionTypeEnum.ThermalImage,
                "TakeThermalVideo" => InspectionTypeEnum.ThermalVideo,
                _ => InspectionTypeEnum.Image,
            };
        }
    }
}
