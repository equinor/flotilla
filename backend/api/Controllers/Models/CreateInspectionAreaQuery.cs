using Api.Database.Models;
using Api.Services.Models;

namespace Api.Controllers.Models
{
    public struct CreateInspectionAreaQuery
    {
        public string InstallationCode { get; set; }
        public string PlantCode { get; set; }
        public string Name { get; set; }
        public AreaPolygon? AreaPolygon { get; set; }
    }
}
