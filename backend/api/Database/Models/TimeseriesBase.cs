using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class TimeseriesBase
    {
        [Required]
        public DateTimeOffset Time { get; set; }

        [Required]
        [ForeignKey(nameof(Robot))]
        public string RobotId { get; set; }

        public string? MissionId { get; set; }
    }
}
