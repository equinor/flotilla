using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable
namespace Api.Models
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
    }
}
