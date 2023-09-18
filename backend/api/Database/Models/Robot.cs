using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Controllers.Models;
#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class Robot
    {

        public Robot()
        {
            VideoStreams = new List<VideoStream>();
            IsarId = "defaultIsarId";
            Name = "defaultId";
            SerialNumber = "defaultSerialNumber";
            CurrentInstallation = "defaultAsset";
            Status = RobotStatus.Offline;
            Enabled = false;
            Host = "localhost";
            Port = 3000;
            Pose = new Pose();
        }

        public Robot(CreateRobotQuery createQuery)
        {
            var videoStreams = new List<VideoStream>();
            foreach (var videoStreamQuery in createQuery.VideoStreams)
            {
                var videoStream = new VideoStream
                {
                    Name = videoStreamQuery.Name,
                    Url = videoStreamQuery.Url,
                    Type = videoStreamQuery.Type
                };
                videoStreams.Add(videoStream);
            }

            IsarId = createQuery.IsarId;
            Name = createQuery.Name;
            SerialNumber = createQuery.SerialNumber;
            CurrentInstallation = createQuery.CurrentInstallation;
            CurrentArea = createQuery.CurrentArea;
            VideoStreams = videoStreams;
            Host = createQuery.Host;
            Port = createQuery.Port;
            Enabled = createQuery.Enabled;
            Status = createQuery.Status;
            Pose = new Pose();
        }
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
        public virtual RobotModel Model { get; set; }

        [Required]
        [MaxLength(200)]
        public string SerialNumber { get; set; }

        public string CurrentInstallation { get; set; }

        public Area? CurrentArea { get; set; }

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
        public bool MissionQueueFrozen { get; set; }

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
                {
                    host = "localhost";
                }

                return $"{Method}://{host}:{Port}";
            }
        }
    }

    public enum RobotStatus
    {
        Available,
        Busy,
        Offline,
        Deprecated
    }
}
