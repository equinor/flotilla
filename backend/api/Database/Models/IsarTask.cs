using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable
namespace Api.Database.Models
{
    public class IsarTask
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public string Id { get; set; }

        [Required]
        public string IsarTaskId { get; set; }

        [Required]
        public virtual Report Report { get; set; }

        public string TagId { get; set; }

        [Required]
        public IsarTaskStatus TaskStatus { get; set; }

        [Required]
        public DateTimeOffset Time { get; set; }

        [Required]
        public virtual IList<IsarStep> Steps { get; set; }
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

    public static class IsarTaskStatusMethods
    {
        public static IsarTaskStatus FromString(string status) =>
            status switch
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
                      $"Failed to parse report status {status} as it's not supported"
                  )
            };
    }
}
