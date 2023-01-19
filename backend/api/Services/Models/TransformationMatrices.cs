    
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
        private double c1 { get; set; }
        private double c2 { get; set; }
        private double d1 { get; set; }
        private double d2 { get; set; }

        public TransformationMatrices()
        {
            c1 = 0;
            c2 = 0;
            d1 = 0;
            d2 = 0;
        }
        public TransformationMatrices(double[] p1, double[] p2, int imageWidth, int imageHeight)
        {
            c1 = (imageWidth)/(p2[0]-p1[0]);
            c2 = (imageHeight)/(p2[1]-p1[1]);
            d1 = -(c1*p1[0]);
            d2 = -(c2*p1[1]);
        }
        public List<double[]> GetTransformationMatrices()
        {
            return new List<double[]> {new double[] {c1, c2}, new double[] {d1, d2}};
        }
    }
}
    