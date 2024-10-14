using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Controllers.Models;
using Api.Services.Models;
#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class Inspection
    {
        private InspectionStatus _status;

        public Inspection()
        {
            InspectionType = InspectionType.Image;
            Status = InspectionStatus.NotStarted;
            InspectionTarget = new Position();
        }

        public Inspection(
            InspectionType inspectionType,
            float? videoDuration,
            Position inspectionTarget,
            InspectionStatus status = InspectionStatus.NotStarted,
            AnalysisType? analysisType = null
            )
        {
            InspectionType = inspectionType;
            VideoDuration = videoDuration;
            InspectionTarget = inspectionTarget;
            AnalysisType = analysisType;
            Status = status;
        }

        public Inspection(CustomInspectionQuery inspectionQuery)
        {
            InspectionType = inspectionQuery.InspectionType;
            InspectionTarget = inspectionQuery.InspectionTarget;
            VideoDuration = inspectionQuery.VideoDuration;
            AnalysisType = inspectionQuery.AnalysisType;
            Status = InspectionStatus.NotStarted;
        }

        // Creates a blank deepcopy of the provided inspection
        public Inspection(Inspection copy, InspectionStatus? inspectionStatus = null, bool useEmptyIDs = false)
        {
            Id = useEmptyIDs ? "" : Guid.NewGuid().ToString();
            IsarTaskId = copy.IsarTaskId;
            Status = inspectionStatus ?? copy.Status;
            InspectionType = copy.InspectionType;
            VideoDuration = copy.VideoDuration;
            AnalysisType = copy.AnalysisType;
            InspectionUrl = copy.InspectionUrl;
            InspectionTarget = new Position(copy.InspectionTarget);
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        [MaxLength(200)]
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string IsarTaskId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public Position InspectionTarget { get; set; }

        [Required]
        public InspectionStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                if (IsCompleted && EndTime is null)
                {
                    EndTime = DateTime.UtcNow;
                }

                if (_status is InspectionStatus.InProgress && StartTime is null)
                {
                    StartTime = DateTime.UtcNow;
                }
            }
        }

        public bool IsCompleted =>
            _status
                is InspectionStatus.Cancelled
                or InspectionStatus.Successful
                or InspectionStatus.Failed;

        [Required]
        public InspectionType InspectionType { get; set; }

        public float? VideoDuration { get; set; }

        public AnalysisType? AnalysisType { get; set; }

        [MaxLength(250)]
        public string? InspectionUrl { get; set; }

        public DateTime? StartTime { get; private set; }

        public DateTime? EndTime { get; private set; }

        public List<InspectionFinding> InspectionFindings { get; set; }

        public void UpdateWithIsarInfo(IsarTask isarTask)
        {
            UpdateStatus(isarTask.TaskStatus);
            InspectionType = isarTask.TaskType switch
            {
                IsarTaskType.RecordAudio => InspectionType.Audio,
                IsarTaskType.TakeImage => InspectionType.Image,
                IsarTaskType.TakeThermalImage => InspectionType.ThermalImage,
                IsarTaskType.TakeVideo => InspectionType.Video,
                IsarTaskType.TakeThermalVideo => InspectionType.ThermalVideo,
                _
                    => throw new ArgumentException(
                        $"ISAR task type '{isarTask.TaskType}' not supported for inspections"
                    )
            };
            IsarTaskId = isarTask.IsarTaskId;
        }

        public void UpdateStatus(IsarTaskStatus isarStatus)
        {
            Status = isarStatus switch
            {
                IsarTaskStatus.NotStarted => InspectionStatus.NotStarted,
                IsarTaskStatus.InProgress => InspectionStatus.InProgress,
                IsarTaskStatus.Successful => InspectionStatus.Successful,
                IsarTaskStatus.Cancelled => InspectionStatus.Cancelled,
                IsarTaskStatus.Failed => InspectionStatus.Failed,
                _
                    => throw new ArgumentException(
                        $"ISAR task status '{isarStatus}' not supported for inspection status"
                    )
            };
        }

        public bool IsSupportedInspectionType(IList<RobotCapabilitiesEnum> capabilities)
        {
            return InspectionType switch
            {
                InspectionType.Image => capabilities.Contains(RobotCapabilitiesEnum.take_image),
                InspectionType.ThermalImage => capabilities.Contains(RobotCapabilitiesEnum.take_thermal_image),
                InspectionType.Video => capabilities.Contains(RobotCapabilitiesEnum.take_video),
                InspectionType.ThermalVideo => capabilities.Contains(RobotCapabilitiesEnum.take_thermal_video),
                InspectionType.Audio => capabilities.Contains(RobotCapabilitiesEnum.record_audio),
                _ => false,
            };
        }
    }

    public enum InspectionStatus
    {
        Successful,
        InProgress,
        NotStarted,
        Failed,
        Cancelled
    }

    public enum InspectionType
    {
        Image,
        ThermalImage,
        Video,
        ThermalVideo,
        Audio
    }

    public enum AnalysisType
    {
        CarSeal,
        RtjFlange
    }
}
