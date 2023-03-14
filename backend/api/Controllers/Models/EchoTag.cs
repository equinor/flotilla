#nullable disable
using Api.Database.Models;

namespace Api.Controllers.Models
{
    public class EchoTag
    {
        public int Id { get; set; }

        public string TagId { get; set; }

        public int PlanOrder { get; set; }

        public int PoseId { get; set; }

        public Pose Pose { get; set; }

        public Uri URL { get; set; }

        public virtual IList<EchoInspection> Inspections { get; set; }
    }
}
