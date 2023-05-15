using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Api.Database.Models
{
    [Keyless]
    public class RobotBatteryTimeseries : TimeseriesBase
    {
        [Required]
        public float BatteryLevel { get; set; }
    }
}
