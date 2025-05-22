namespace Api.Services.Models
{
    public class IsarTask(IsarTaskResponse taskResponse)
    {
        public string IsarTaskId { get; } = taskResponse.IsarTaskId;

        public string? IsarInspectionId { get; } = taskResponse.IsarInspectionId;

        public IsarTaskStatus TaskStatus { get; } = IsarTaskStatus.NotStarted;

        public IsarTaskType TaskType { get; } = TaskTypeFromString(taskResponse.TaskType);

        public static IsarTaskStatus StatusFromString(string status)
        {
            return status switch
            {
                "successful" => IsarTaskStatus.Successful,
                "partially_successful" => IsarTaskStatus.PartiallySuccessful,
                "not_started" => IsarTaskStatus.NotStarted,
                "in_progress" => IsarTaskStatus.InProgress,
                "failed" => IsarTaskStatus.Failed,
                "cancelled" => IsarTaskStatus.Cancelled,
                "paused" => IsarTaskStatus.Paused,
                _ => throw new ArgumentException(
                    $"Failed to parse task status '{status}' - not supported"
                ),
            };
        }

        public static IsarTaskType TaskTypeFromString(string isarClassName)
        {
            return isarClassName switch
            {
                "record_audio" => IsarTaskType.RecordAudio,
                "take_image" => IsarTaskType.TakeImage,
                "take_video" => IsarTaskType.TakeVideo,
                "take_thermal_image" => IsarTaskType.TakeThermalImage,
                "take_thermal_video" => IsarTaskType.TakeThermalVideo,
                "take_co2_measurement" => IsarTaskType.TakeCO2Measurement,
                "return_to_home" => IsarTaskType.ReturnToHome,
                "move_arm" => IsarTaskType.MoveArm,
                _ => throw new ArgumentException(
                    $"Failed to parse step type '{isarClassName}' - not supported"
                ),
            };
        }
    }

    public enum IsarTaskStatus
    {
        Successful,
        PartiallySuccessful,
        NotStarted,
        InProgress,
        Failed,
        Cancelled,
        Paused,
    }

    public enum IsarTaskType
    {
        ReturnToHome,
        TakeImage,
        TakeVideo,
        TakeThermalImage,
        TakeThermalVideo,
        TakeCO2Measurement,
        RecordAudio,
        MoveArm,
    }
}
