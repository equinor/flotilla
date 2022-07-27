using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable
namespace Api.Database.Models
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

        public string Logs { get; set; }

        public float BatteryLevel { get; set; }

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

        public virtual Pose Pose { get; set; }

        public string IsarUri
        {
            get
            {
                string host = Host;
                if (host == "0.0.0.0")
                    host = "localhost";
                return $"http://{host}:{Port}";
            }
        }
        public Robot()
        {
            this.Name = "defaultId";
            this.Model = "defaultModel";
            SerialNumber = "defaultSerialNumber";
            Status = RobotStatus.Busy;
            Enabled = false;
            Host = "localhost";
            Logs = "logs";
            Port = 3000;
            Pose = new Pose();
        }
    }
    public enum RobotStatus
    {
        Available,
        Busy,
        Offline
    }
}

