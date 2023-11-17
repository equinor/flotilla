namespace Api.Services.Models
{
    public class IsarStep(IsarStepResponse stepResponse)
    {
        public string IsarStepId { get; } = stepResponse.IsarStepId;

        public IsarStepStatus StepStatus { get; } = IsarStepStatus.NotStarted;

        public IsarStepType StepType { get; } = StepTypeFromString(stepResponse.Type);


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
                "MoveArm" => IsarStepType.MoveArm,
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
        RecordAudio,
        MoveArm
    }
}
