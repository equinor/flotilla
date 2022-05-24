using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable
namespace Api.Models
{
    public class Step
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public string Id { get; set; }

        [Required]
        public string IsarStepId { get; set; }

        [Required]
        public virtual Task task { get; set; }

        public string TagId { get; set; }

        [Required]
        public StepStatus StepStatus { get; set; }

        [Required]
        public StepType StepType { get; set; }

        public InspectionType InspectionType { get; set; }

        [Required]
        public DateTimeOffset Time { get; set; }

        [MaxLength(128)]
        public string FileLocation { get; set; }
    }

    public enum StepStatus
    {
        Successful,
        InProgress,
        NotStarted,
        Failed,
        Cancelled
    }

    public enum StepType
    {
        Motion,
        Inspection
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
                _ => InspectionType.Image,
            };
        }
    }
}
