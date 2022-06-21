using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable
namespace Api.Database.Models
{
    public class Robot
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public string Id { get; set; }

        [MaxLength(128)]
        [Required]
        public string Name { get; set; }

        [MaxLength(128)]
        [Required]
        public string Model { get; set; }

        [MaxLength(128)]
        [Required]
        public string SerialNumber { get; set; }

        [MaxLength(128)]
        [Required]
        public string Logs { get; set; }

        public virtual IList<VideoStream> VideoStreams { get; set; }

        [MaxLength(128)]
        [Required]
        public string Host { get; set; }

        [Required]
        public int Port { get; set; }

        [Required]
        public bool Enabled { get; set; }

        [Required]
        public RobotStatus Status { get; set; }
    }

    public enum RobotStatus
    {
        Available,
        Busy,
        Offline
    }
}
