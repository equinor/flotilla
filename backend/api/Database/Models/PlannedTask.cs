using Api.Controllers.Models;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace Api.Database.Models
{
    [Owned]
    public class PlannedTask
    {
        public string TagId { get; set; }

        public Uri URL { get; set; }

        public IList<PlannedInspection> Inspections { get; set; }

        public PlannedTask()
        {
            Inspections = new List<PlannedInspection>();
        }

        public PlannedTask(EchoTag echoTag)
        {
            Inspections = echoTag.Inspections
                .Select(inspection => new PlannedInspection(inspection))
                .ToList();
            URL = echoTag.URL;
            TagId = echoTag.TagId;
        }
    }
}
