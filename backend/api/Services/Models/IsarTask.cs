namespace Api.Services.Models
{
    public class IsarTask(IsarTaskResponse taskResponse)
    {
        public string IsarTaskId { get; } = taskResponse.IsarTaskId;

        public IsarTaskStatus TaskStatus { get; } = IsarTaskStatus.NotStarted;

        public IList<IsarStep> Steps { get; } = taskResponse.Steps.Select(step => new IsarStep(step)).ToList();


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
                _
                  => throw new ArgumentException(
                      $"Failed to parse task status '{status}' - not supported"
                  )
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
}
