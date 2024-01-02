#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class UpdateRobotQuery
    {
        public string? InstallationId { get; set; }

        public string? AreaId { get; set; }

        public float? MinAllowedPressureLevel { get; set; }

        public float? MinAllowedBatteryLevel { get; set; }

        public Pose? Pose { get; set; }

        public string? MissionId { get; set; }
    }
}
