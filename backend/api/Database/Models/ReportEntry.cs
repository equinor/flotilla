using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable
namespace Api.Models
{
    public class ReportEntry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public string Id { get; set; }

        [Required]
        public virtual Report Report { get; set; }

        public string TagId { get; set; }

        [Required]
        public ReportEntryStatus ReportEntryStatus { get; set; }

        [Required]
        public InspectionType InspectionType { get; set; }

        [Required]
        public DateTimeOffset Time { get; set; }

        [MaxLength(128)]
        [Required]
        public string FileLocation { get; set; }
    }

    public enum ReportEntryStatus
    {
        Completed, Failed
    }

    public enum InspectionType
    {
        Image, ThermalImage, Audio
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
