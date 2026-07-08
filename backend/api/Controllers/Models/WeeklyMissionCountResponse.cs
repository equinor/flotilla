namespace Api.Controllers.Models
{
    public class WeeklyMissionCountResponse
    {
        public DateTime WeekStart { get; set; }

        public DateTime WeekEnd { get; set; }

        /// <summary>
        /// Completed mission runs with a creation time in [WeekStart, WeekEnd).
        /// </summary>
        public int Count { get; set; }
    }
}
