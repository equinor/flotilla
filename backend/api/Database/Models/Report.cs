using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable
namespace Api.Database.Models
{
    public class Report
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public string Id { get; set; }

        public string AssetCode { get; set; }

        [Required]
        public Robot Robot { get; set; }

        [MaxLength(128)]
        [Required]
        public string IsarMissionId { get; set; }

        [MaxLength(128)]
        [Required]
        public int EchoMissionId { get; set; }

        [MaxLength(128)]
        public string Log { get; set; }

        [Required]
        public ReportStatus ReportStatus { get; set; }

        [Required]
        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        [Required]
        public virtual IList<IsarTask> Tasks { get; set; }
    }

    public enum ReportStatus
    {
        Successful,
        NotStarted,
        InProgress,
        Failed,
        Cancelled,
        Paused
    }

    public static class ReportStatusMethods
    {
        public static ReportStatus FromString(string status)
        {
            return status switch
            {
                "completed" => ReportStatus.Successful,
                "not_started" => ReportStatus.NotStarted,
                "in_progress" => ReportStatus.InProgress,
                "failed" => ReportStatus.Failed,
                "cancelled" => ReportStatus.Cancelled,
                "paused" => ReportStatus.Paused,
                _
                    => throw new ArgumentException(
                        $"Failed to parse report status {status} as it's not supported"
                    )
            };
        }
    }
}
