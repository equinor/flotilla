using Api.Database.Models;

#nullable disable
namespace Api.Controllers.Models
{
    public class EchoTag
    {
        public string Id { get; set; }

        public string TagId { get; set; }

        public Uri URL { get; set; }

        public virtual IList<InspectionType> InspectionTypes { get; set; }
    }
}
