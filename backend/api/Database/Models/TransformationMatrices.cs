using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

#nullable disable
namespace Api.Database.Models
{
    [Owned]
    public class TransformationMatrices
    {
        // In order to get a pixel coordinate P={p1, p2} from an Echo coordinate E={e1, e2}, we need to
        // perform the transformation:
        // P = CE + D
        [Required]
        public double C1 { get; set; }

        [Required]
        public double C2 { get; set; }

        [Required]
        public double D1 { get; set; }

        [Required]
        public double D2 { get; set; }

        public TransformationMatrices()
        {
            C1 = 0;
            C2 = 0;
            D1 = 0;
            D2 = 0;
        }

        public TransformationMatrices(double[] p1, double[] p2, int imageWidth, int imageHeight)
        {
            C1 = imageWidth / (p2[0] - p1[0]);
            C2 = imageHeight / (p2[1] - p1[1]);
            D1 = -(C1 * p1[0]);
            D2 = -(C2 * p1[1]);
        }

        public List<double[]> AsMatrix()
        {
            return new List<double[]> { new double[] { C1, C2 }, new double[] { D1, D2 } };
        }
    }
}
