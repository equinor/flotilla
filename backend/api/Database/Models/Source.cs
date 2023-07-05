using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class Source
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        public string SourceId { get; set; }

        [Required]
        public MissionSourceType Type { get; set; }
    }

    public enum MissionSourceType
    {
        Echo, Custom
    }
}
