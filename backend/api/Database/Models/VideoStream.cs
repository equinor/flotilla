#nullable disable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Database.Models
{
    public class VideoStream
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public string Id { get; set; }

        public string RobotId { get; set; }

        [MaxLength(64)]
        [Required]
        public string Name { get; set; }

        [MaxLength(128)]
        [Required]
        public string Url { get; set; }
    }
}
