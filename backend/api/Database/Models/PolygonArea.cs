using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Api.Database.Models;

[Owned]
public class AreaPolygon
{
    [JsonPropertyName("zmin")]
    [Required]
    public double ZMin { get; set; }

    [JsonPropertyName("zmax")]
    [Required]
    public double ZMax { get; set; }

    [JsonPropertyName("positions")]
    [Required]
    public List<PolygonPoint> Positions { get; set; } = [];
}

[Owned]
public class PolygonPoint
{
    public PolygonPoint() { }

    public PolygonPoint(float x = 0, float y = 0)
    {
        X = x;
        Y = y;
    }

    [JsonPropertyName("x")]
    [Required]
    public double X { get; set; }

    [JsonPropertyName("y")]
    [Required]
    public double Y { get; set; }
}
