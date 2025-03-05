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
    public XYPosition() { }

    public XYPosition(float x = 0, float y = 0)
    {
        X = x;
        Y = y;
    }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }
}
