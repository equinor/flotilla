using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Controllers.Models;
using Api.Services.Models;
#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class Inspection
    {
        public Inspection()
        {
            InspectionType = InspectionType.Image;
            InspectionTarget = new Position();
        }

        public Inspection(
            InspectionType inspectionType,
            float? videoDuration,
            Position inspectionTarget,
            string? inspectionTargetName
        )
        {
            InspectionType = inspectionType;
            VideoDuration = videoDuration;
            InspectionTarget = inspectionTarget;
            InspectionTargetName = inspectionTargetName;
        }

        public Inspection(CustomInspectionQuery inspectionQuery)
        {
            InspectionType = inspectionQuery.InspectionType;
            InspectionTarget = inspectionQuery.InspectionTarget;
            VideoDuration = inspectionQuery.VideoDuration;
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
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string IsarInspectionId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public Position InspectionTarget { get; set; }

        public string? InspectionTargetName { get; set; }

        [Required]
        public InspectionType InspectionType { get; set; }

        public float? VideoDuration { get; set; }

        [MaxLength(250)]
        public string? InspectionUrl { get; set; }

        public void UpdateWithIsarInfo(IsarTask isarTask)
        {
            if (isarTask.IsarInspectionId != null)
            {
                IsarInspectionId = isarTask.IsarInspectionId;
            }
        }

        public bool IsSupportedInspectionType(IList<RobotCapabilitiesEnum> capabilities)
        {
            return InspectionType switch
            {
                InspectionType.Image => capabilities.Contains(RobotCapabilitiesEnum.take_image),
                InspectionType.ThermalImage => capabilities.Contains(
                    RobotCapabilitiesEnum.take_thermal_image
                ),
                InspectionType.Video => capabilities.Contains(RobotCapabilitiesEnum.take_video),
                InspectionType.ThermalVideo => capabilities.Contains(
                    RobotCapabilitiesEnum.take_thermal_video
                ),
                InspectionType.CO2Measurement => capabilities.Contains(
                    RobotCapabilitiesEnum.take_co2_measurement
                ),
                InspectionType.Audio => capabilities.Contains(RobotCapabilitiesEnum.record_audio),
                _ => false,
            };
        }
    }

    public enum InspectionType
    {
        Image,
        ThermalImage,
        Video,
        ThermalVideo,
        Audio,
        CO2Measurement,
    }
}
