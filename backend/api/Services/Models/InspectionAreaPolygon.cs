using System.Text.Json.Serialization;

namespace Api.Services.Models;

public class InspectionAreaPolygon
{
    [JsonPropertyName("zmin")]
    public double ZMin { get; set; }

    [JsonPropertyName("zmax")]
    public double ZMax { get; set; }

    [JsonPropertyName("positions")]
    public List<XYPosition> Positions { get; set; } = [];
}

public class XYPosition
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }
}
