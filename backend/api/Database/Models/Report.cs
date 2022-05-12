using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable
namespace Api.Models
{
    public class Report
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public string Id { get; set; }

        [Required]
        public virtual Robot Robot { get; set; }

        [MaxLength(128)]
        [Required]
        public string IsarMissionId { get; set; }

        [MaxLength(128)]
        [Required]
        public string EchoMissionId { get; set; }

        [MaxLength(128)]
        [Required]
        public string Log { get; set; }

        [Required]
        public ReportStatus ReportStatus { get; set; }

        [Required]
        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        [Required]
        public virtual ICollection<ReportEntry> Entries { get; private set; }
    }

    public enum ReportStatus
    {
        InProgress,
        Completed,
        Failed
    }
}
