using Api.Database.Models;

#pragma warning disable CS8618
namespace Api.Controllers.Models
{
    public class InspectionDto
    {
        public InspectionDto(Inspection inspection)
        {
            Id = inspection.Id;
            IsarStepId = inspection.IsarStepId;
            Status = inspection.Status;
            IsCompleted = inspection.IsCompleted;
            InspectionType = inspection.InspectionType;
            VideoDuration = inspection.VideoDuration;
            AnalysisTypes = inspection.AnalysisTypes;
            InspectionUrl = inspection.InspectionUrl;
            Error = inspection.GetError();
            StartTime = inspection.StartTime;
            EndTime = inspection.EndTime;
        }

        public string Id { get; set; }

        public string? IsarStepId { get; set; }

        public InspectionStatus Status { get; set; }

        public bool IsCompleted { get; set; }

        public InspectionType InspectionType { get; set; }

        public float? VideoDuration { get; set; }

        public string? AnalysisTypes { get; set; }

        public string? InspectionUrl { get; set; }

        public string? Error { get; set; }

        public DateTimeOffset? StartTime { get; private set; }

        public DateTimeOffset? EndTime { get; private set; }
    }
}
