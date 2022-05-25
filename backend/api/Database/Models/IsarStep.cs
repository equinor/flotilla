using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable
namespace Api.Models
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

    public enum StepType
    {
        DriveToPose,
        TakeImage,
        TakeThermalImage,
        Audio
    }

    public class SelectStepType
    {
        public static StepType From(string sensorType)
        {
            return sensorType switch
            {
                "DriveToPose" => StepType.DriveToPose,
                "Audio" => StepType.Audio,
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
        public static InspectionType From(string sensorType)
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
