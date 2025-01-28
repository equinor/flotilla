using Api.Database.Models;

namespace Api.Controllers.Models
{
    public class UpdateRobotQuery
    {
        public Pose? Pose { get; set; }

        public string? MissionId { get; set; }
    }
}
