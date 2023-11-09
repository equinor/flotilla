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
        }

        public Inspection(EchoInspection echoInspection)
        {
            InspectionType = echoInspection.InspectionType;
            VideoDuration = echoInspection.TimeInSeconds;
            Status = InspectionStatus.NotStarted;
        }

        public Inspection(CustomInspectionQuery inspectionQuery)
        {
            InspectionType = inspectionQuery.InspectionType;
            VideoDuration = inspectionQuery.VideoDuration;
            AnalysisType = inspectionQuery.AnalysisType;
            Status = InspectionStatus.NotStarted;
        }

        // Creates a blank deepcopy of the provided inspection
        public Inspection(Inspection copy)
        {
            Id = "";
            IsarStepId = "";
            Status = copy.Status;
            InspectionType = copy.InspectionType;
            VideoDuration = copy.VideoDuration;
            AnalysisType = copy.AnalysisType;
            InspectionUrl = copy.InspectionUrl;
        }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [MaxLength(200)]
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string? IsarStepId { get; private set; } = Guid.NewGuid().ToString();

        [Required]
        public InspectionStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                if (IsCompleted && EndTime is null) { EndTime = DateTime.UtcNow; }

                if (_status is InspectionStatus.InProgress && StartTime is null) { StartTime = DateTime.UtcNow; }
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

        public List<InspectionFindings> InspectionFindings { get; set; }

        public void UpdateWithIsarInfo(IsarStep isarStep)
        {
            UpdateStatus(isarStep.StepStatus);
            InspectionType = isarStep.StepType switch
            {
                IsarStepType.RecordAudio => InspectionType.Audio,
                IsarStepType.TakeImage => InspectionType.Image,
                IsarStepType.TakeThermalImage => InspectionType.ThermalImage,
                IsarStepType.TakeVideo => InspectionType.Video,
                IsarStepType.TakeThermalVideo => InspectionType.ThermalVideo,
                _
                    => throw new ArgumentException(
                        $"ISAR step type '{isarStep.StepType}' not supported for inspections"
                    )
            };
        }

        public void UpdateStatus(IsarStepStatus isarStatus)
        {
            Status = isarStatus switch
            {
                IsarStepStatus.NotStarted => InspectionStatus.NotStarted,
                IsarStepStatus.InProgress => InspectionStatus.InProgress,
                IsarStepStatus.Successful => InspectionStatus.Successful,
                IsarStepStatus.Cancelled => InspectionStatus.Cancelled,
                IsarStepStatus.Failed => InspectionStatus.Failed,
                _
                    => throw new ArgumentException(
                        $"ISAR step status '{isarStatus}' not supported for inspection status"
                    )
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
