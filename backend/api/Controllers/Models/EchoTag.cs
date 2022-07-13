#nullable disable
namespace Api.Controllers.Models
{
    public class EchoTag
    {
        public int Id { get; set; }

        public string TagId { get; set; }

        public Uri URL { get; set; }

        public virtual IList<EchoInspection> Inspections { get; set; }
    }
}
