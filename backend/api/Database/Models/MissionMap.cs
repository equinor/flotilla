#nullable disable
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Api.Database.Models
{
    [Owned]
    public class MissionMap
    {
        public MissionMap()
        {
            MapName = "DefaultMapName";
            Boundary = new Boundary();
            TransformationMatrices = new TransformationMatrices();
        }

        [Required]
        [MaxLength(200)]
        public string MapName { get; set; }

        [Required]
        public Boundary Boundary { get; set; }

        [Required]
        public TransformationMatrices TransformationMatrices { get; set; }
    }

    [Owned]
    public class Boundary
    {
        public Boundary()
        {
            X1 = 0;
            Y1 = 0;
            X2 = 0;
            Y2 = 0;
            Z1 = 0;
            Z2 = 0;
        }

        public Boundary(double x1, double y1, double x2, double y2, double z1, double z2)
        {
            X1 = Math.Min(x1, x2);
            X2 = Math.Max(x1, x2);
            Y1 = Math.Min(y1, y2);
            Y2 = Math.Max(y1, y2);
            Z1 = Math.Min(z1, z2);
            Z2 = Math.Max(z1, z2);
        }

        [Required]
        public double X1 { get; set; }

        [Required]
        public double X2 { get; set; }

        [Required]
        public double Y1 { get; set; }

        [Required]
        public double Y2 { get; set; }

        [Required]
        public double Z1 { get; set; }

        [Required]
        public double Z2 { get; set; }

        public List<double[]> As2DMatrix()
        {
            return new List<double[]> { new[] { X1, Y1 }, new[] { X2, Y2 } };
        }
    }
}
