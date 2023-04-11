using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class Robot
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        [MaxLength(200)]
        public string IsarId { get; set; }

        [Required]
        public RobotModel Model { get; set; }

        [Required]
        [MaxLength(200)]
        public string SerialNumber { get; set; }

        [MaxLength(200)]
        public string Logs { get; set; }

        public float BatteryLevel { get; set; }

        public float? PressureLevel { get; set; }

        public IList<VideoStream> VideoStreams { get; set; }

        [Required]
        [MaxLength(200)]
        public string Host { get; set; }

        [Required]
        public int Port { get; set; }

        [Required]
        public bool Enabled { get; set; }

        [Required]
        public RobotStatus Status { get; set; }

        [Required]
        public Pose Pose { get; set; }

        public string? CurrentMissionId { get; set; }

        public string IsarUri
        {
            get
            {
                const string Method = "http";
                string host = Host;
                if (host == "0.0.0.0")
                    host = "localhost";

                return $"{Method}://{host}:{Port}";
            }
        }

        public Robot()
        {
            VideoStreams = new List<VideoStream>();
            IsarId = "defaultIsarId";
            Name = "defaultId";
            Model = RobotModel.Turtlebot;
            SerialNumber = "defaultSerialNumber";
            Status = RobotStatus.Offline;
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
        Offline,
        Deprecated,
    }
}
