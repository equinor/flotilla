using Api.Database.Models;

namespace Api.Controllers.Models
{
    public struct CreateExclusionAreaQuery
    {
        public string InstallationCode { get; set; }
        public string PlantCode { get; set; }
        public string? Name { get; set; }
        public AreaPolygon AreaPolygon { get; set; }
    }
}
