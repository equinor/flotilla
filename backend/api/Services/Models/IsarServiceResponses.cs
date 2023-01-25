using System.ComponentModel.DataAnnotations;
using Api.Database.Models;

namespace Api.Services.Models
{
    public class IsarServiceStartMissionResponse
    {
        [Required]
        public string IsarMissionId { get; set; }

        [Required]
        public DateTimeOffset StartTime { get; set; }

        [Required]
        public IList<IsarTask> Tasks { get; set; }

        public IsarServiceStartMissionResponse(
            string isarMissionId,
            DateTimeOffset startTime,
            IList<IsarTask> tasks
        )
        {
            IsarMissionId = isarMissionId;
            StartTime = startTime;
            Tasks = tasks;
        }
    }
}
