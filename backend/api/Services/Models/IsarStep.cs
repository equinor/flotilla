namespace Api.Services.Models
{
    public class IsarStep
    {
        public string IsarStepId { get; set; }

        public IsarStepStatus StepStatus { get; set; }

        public IsarStepType StepType { get; set; }

        public IsarStep(IsarStepResponse stepResponse)
        {
            IsarStepId = stepResponse.IsarStepId;
            StepType = StepTypeFromString(stepResponse.Type);
            StepStatus = IsarStepStatus.NotStarted;
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
                      $"Failed to parse step status '{status}' - not supported"
                  )
            };
        }

        public static IsarStepType StepTypeFromString(string isarClassName)
        {
            return isarClassName switch
            {
                "DriveToPose" => IsarStepType.DriveToPose,
                "RecordAudio" => IsarStepType.RecordAudio,
                "TakeImage" => IsarStepType.TakeImage,
                "TakeVideo" => IsarStepType.TakeVideo,
                "TakeThermalImage" => IsarStepType.TakeThermalImage,
                "TakeThermalVideo" => IsarStepType.TakeThermalVideo,
                "Localize" => IsarStepType.Localize,
                _
                  => throw new ArgumentException(
                      $"Failed to parse step type '{isarClassName}' - not supported"
                  )
            };
        }
    }

    public enum IsarStepStatus
    {
        Successful,
        InProgress,
        NotStarted,
        Failed,
        Cancelled
    }

    public enum IsarStepType
    {
        DriveToPose,
        Localize,
        TakeImage,
        TakeVideo,
        TakeThermalImage,
        TakeThermalVideo,
        RecordAudio
    }
}
