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
            Documentation = [];
            IsarId = "defaultIsarId";
            Name = "defaultId";
            SerialNumber = "defaultSerialNumber";
            Status = RobotStatus.Offline;
            IsarConnected = true;
            Deprecated = false;
            Host = "localhost";
            Port = 3000;
        }

        public Robot(
            CreateRobotQuery createQuery,
            Installation installation,
            RobotModel model,
            string? inspectionAreaId = null
        )
        {
            var documentation = new List<DocumentInfo>();
            foreach (var documentQuery in createQuery.Documentation)
            {
                var document = new DocumentInfo
                {
                    Name = documentQuery.Name,
                    Url = documentQuery.Url,
                };
                documentation.Add(document);
            }

            IsarId = createQuery.IsarId;
            Name = createQuery.Name;
            SerialNumber = createQuery.SerialNumber;
            CurrentInstallation = installation;
            CurrentInspectionAreaId = inspectionAreaId;
            Documentation = documentation;
            Host = createQuery.Host;
            Port = createQuery.Port;
            IsarConnected = true;
            Deprecated = false;
            RobotCapabilities = createQuery.RobotCapabilities;
            Status = createQuery.Status;
            Model = model;
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

        [Required]
        public Installation CurrentInstallation { get; set; }

        public string? CurrentInspectionAreaId { get; set; }

        public static bool IsStatusThatCanReceiveMissions(RobotStatus status)
        {
            var RobotStatusesWhereRobotCanStartMission = new[]
            {
                RobotStatus.Available,
                RobotStatus.Home,
                RobotStatus.ReturningHome,
                RobotStatus.ReturnHomePaused,
            };
            return RobotStatusesWhereRobotCanStartMission.Contains(status);
        }

        public IList<DocumentInfo> Documentation { get; set; }

        [Required]
        [MaxLength(200)]
        public string Host { get; set; }

        [Required]
        public int Port { get; set; }

        public IList<RobotCapabilitiesEnum>? RobotCapabilities { get; set; }

        [Required]
        public bool IsarConnected { get; set; }

        [Required]
        public bool Deprecated { get; set; }

        [Required]
        public RobotStatus Status { get; set; }

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
        Home,
        Offline,
        BlockedProtectiveStop,
        ReturningHome,
        ReturnHomePaused,
        Paused,
        UnknownStatus,
        InterventionNeeded,
        Recharging,
        Lockdown,
        GoingToLockdown,
        GoingToRecharging,
        Maintenance,
    }

    public enum RobotCapabilitiesEnum
    {
        take_thermal_image,
        take_image,
        take_video,
        take_thermal_video,
        take_co2_measurement,
        record_audio,
    }

    public enum BatteryState
    {
        Normal,
        Charging,
    }
}
