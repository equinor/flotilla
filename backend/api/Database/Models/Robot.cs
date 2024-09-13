﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Controllers.Models;
#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class Robot
    {
        public Robot()
        {
            Documentation = new List<DocumentInfo>();
            VideoStreams = new List<VideoStream>();
            IsarId = "defaultIsarId";
            Name = "defaultId";
            SerialNumber = "defaultSerialNumber";
            Status = RobotStatus.Offline;
            IsarConnected = true;
            Deprecated = false;
            Host = "localhost";
            Port = 3000;
            Pose = new Pose();
        }

        public Robot(CreateRobotQuery createQuery, Installation installation, RobotModel model, Area? area = null)
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

            var documentation = new List<DocumentInfo>();
            foreach (var documentQuery in createQuery.Documentation)
            {
                var document = new DocumentInfo
                {
                    Name = documentQuery.Name,
                    Url = documentQuery.Url
                };
                documentation.Add(document);
            }

            IsarId = createQuery.IsarId;
            Name = createQuery.Name;
            SerialNumber = createQuery.SerialNumber;
            CurrentInstallation = installation;
            CurrentArea = area;
            Documentation = documentation;
            VideoStreams = videoStreams;
            Host = createQuery.Host;
            Port = createQuery.Port;
            IsarConnected = true;
            Deprecated = false;
            RobotCapabilities = createQuery.RobotCapabilities;
            Status = createQuery.Status;
            Pose = new Pose();
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

        public Area? CurrentArea { get; set; }

        public float BatteryLevel { get; set; }

        public float? PressureLevel { get; set; }

        public bool IsRobotPressureTooLow()
        {
            if (Model.LowerPressureWarningThreshold == null) { return false; }
            return PressureLevel == null || Model.LowerPressureWarningThreshold >= PressureLevel;
        }

        public bool IsRobotPressureTooHigh()
        {
            if (Model.UpperPressureWarningThreshold == null) { return false; }
            return PressureLevel == null || Model.UpperPressureWarningThreshold <= PressureLevel;
        }

        public bool IsRobotBatteryTooLow()
        {
            if (Model.BatteryWarningThreshold == null) { return false; }
            return Model.BatteryWarningThreshold >= BatteryLevel;
        }

        public IList<DocumentInfo> Documentation { get; set; }

        public IList<VideoStream> VideoStreams { get; set; }

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
        public bool MissionQueueFrozen { get; set; }

        [Required]
        public RobotStatus Status { get; set; }

        [Required]
        public RobotFlotillaStatus FlotillaStatus { get; set; } = RobotFlotillaStatus.Normal;

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
        Blocked,
    }

    public enum RobotFlotillaStatus
    {
        Normal,
        SafeZone,
        Recharging,
    }

    public enum RobotCapabilitiesEnum
    {
        take_thermal_image,
        take_image,
        take_video,
        take_thermal_video,
        drive_to_pose,
        record_audio,
        localize,
        docking_procedure,
        return_to_home,
    }
}
