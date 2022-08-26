using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable
namespace Api.Database.Models
{
    [Owned]
    public class IsarTask
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        public string IsarTaskId { get; set; }

        [Required]
        public virtual Mission Mission { get; set; }

        public string TagId { get; set; }

        [Required]
        public IsarTaskStatus TaskStatus { get; set; }

        [Required]
        public DateTimeOffset Time { get; set; }

        [Required]
        public IList<IsarStep> Steps { get; set; }

#nullable enable
        public IsarStep? ReadIsarStepById(string isarStepId)
        {
            return Steps.FirstOrDefault(
                step => step.IsarStepId.Equals(isarStepId, StringComparison.Ordinal)
            );
        }
#nullable disable
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
        public static IsarTaskStatus FromString(string status)
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
                      $"Failed to parse mission status {status} as it's not supported"
                  )
            };
        }
    }
}
