using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Api.Database.Models
{
    [Keyless]
    public class RobotPressureTimeseries : TimeseriesBase
    {
        [Required]
        public float Pressure { get; set; }
    }
}
