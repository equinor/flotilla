using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Controllers.Models;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace Api.Database.Models
{
    [Owned]
    public class PlannedTask
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [MaxLength(64)]
        public string TagId { get; set; }

        [MaxLength(64)]
        public Uri URL { get; set; }

        public Position TagPosition { get; set; }

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
