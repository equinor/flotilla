namespace Api.Controllers.Models
{
    public class MissionStatisticsResponse
    {
        /// <summary>
        /// Completed mission runs in the time window (Successful,
        /// PartiallySuccessful, Failed, Aborted or Cancelled). In-flight runs
        /// (Pending, Ongoing, Paused, Queued) are excluded.
        /// </summary>
        public int Total { get; set; }

        public int Successful { get; set; }

        public int PartiallySuccessful { get; set; }

        public int Failed { get; set; }

        public int Aborted { get; set; }

        public int Cancelled { get; set; }

        /// <summary>
        /// Fraction (0-1) of completed runs that were Successful or
        /// PartiallySuccessful.
        /// </summary>
        public double SuccessRate { get; set; }
    }
}
