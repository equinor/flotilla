using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class MissionDefinition : SortableRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        private IList<TaskDefinition> _tasks;

        [Required]
        public List<TaskDefinition> Tasks
        {
            get => _tasks != null ? [.. _tasks.OrderBy(t => t.Index)] : [];
            set => _tasks = value;
        }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        public string InstallationCode { get; set; }

        [Required]
        public InspectionArea InspectionArea { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        [Column(TypeName = "bigint")]
        public TimeSpan? InspectionFrequency { get; set; }

        public AutoScheduleFrequency? AutoScheduleFrequency { get; set; }

        public virtual MissionRun? LastSuccessfulRun { get; set; }

        public bool IsDeprecated { get; set; }
    }
}
