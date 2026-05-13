using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Services.Models;
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
            string? taskDescription = null
        )
        {
            InspectionType = sensorType;
            InspectionTarget = target;
            VideoDuration = videoDuration;
            AnalysisTypes = analysisTypes;
            TaskDescription = taskDescription;
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
    }
}
