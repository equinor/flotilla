using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable
namespace Api.Models
{
    public class Task
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
        public TaskStatus TaskStatus { get; set; }

        [Required]
        public DateTimeOffset Time { get; set; }

        [Required]
        public virtual IList<Step> steps { get; set; }
    }

    public enum TaskStatus
    {
        Successful,
        PartiallySuccessful,
        NotStarted,
        InProgress,
        Failed,
        Cancelled,
    }
}
