using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
            MapName = "Error";
            Boundary = new Boundary();
            TransformationMatrices = new TransformationMatrices();
        }
    }

    [Owned]
    public class Boundary 
    {
        [Required]
        double x1 { get; set; }

        [Required]
        double y1 { get; set; }

        [Required]
        double x2 { get; set; }

        [Required]
        double y2 { get; set; }

        public Boundary()
        {
            x1 = 0;
            y1 = 0;
            x2 = 0;
            y2 = 0;
        }
        public Boundary(double _x1, double _y1, double _x2, double _y2)
        {
            x1 = _x1;
            y1 = _y1;
            x2 = _x2;
            y2 = _y2;
        }

        public List<double[]> getBoundary()
        {
            return (new List<double[]>{new double[]{x1, y1}, new double[]{x2, y2}});
        }
    }
}

