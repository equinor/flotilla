using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

#nullable disable
namespace Api.Database.Models
{
    [Owned]
    public class MissionMap
    {
        [Required]
        public string MapName { get; set; }

        [Required]
        public Boundary Boundary { get; set; }

        [Required]
        public TransformationMatrices TransformationMatrices { get; set; }

        public MissionMap()
        {
            MapName = "Unavailable";
            Boundary = new Boundary();
            TransformationMatrices = new TransformationMatrices();
        }
    }

    [Owned]
    public class Boundary
    {
        [Required]
        public double X1 { get; set; }

        [Required]
        public double Y1 { get; set; }

        [Required]
        public double X2 { get; set; }

        [Required]
        public double Y2 { get; set; }

        public Boundary()
        {
            X1 = 0;
            Y1 = 0;
            X2 = 0;
            Y2 = 0;
        }

        public Boundary(double x1, double y1, double x2, double y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }

        public List<double[]> AsMatrix()
        {
            return new List<double[]> { new double[] { X1, Y1 }, new double[] { X2, Y2 } };
        }
    }
}
