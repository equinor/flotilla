# nullable disable
namespace Api.Controllers.Models
{
    public class EchoMission
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public Uri URL { get; set; }

        public virtual IList<EchoTag> Tags { get; set; }
    }
}
