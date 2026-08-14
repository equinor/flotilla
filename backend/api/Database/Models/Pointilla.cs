namespace Api.Services.Models
{
    /// <summary>
    ///     The input ISAR expects as a mission description in the /schedule/start-mission endpoint
    /// </summary>
    public struct PointillaMapResponse
    {
        public string PlantCode { get; set; }
        public int FloorId { get; set; }
        public string? Label { get; set; }
        public int XMin { get; set; }
        public int XMax { get; set; }
        public int YMin { get; set; }
        public int YMax { get; set; }
        public int ZoomMin { get; set; }
        public int ZoomMax { get; set; }
        public int TileSize { get; set; }
    }
}
