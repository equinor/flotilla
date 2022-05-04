using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable
namespace Api.Models
{
    public class Robot
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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
        [Required]
        public RobotStatus Status { get; set; }
    }



    public enum RobotStatus
    {
        Available, Busy, Offline
    }
}
