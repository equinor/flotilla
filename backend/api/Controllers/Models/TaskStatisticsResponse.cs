namespace Api.Controllers.Models
{
    public class TaskStatisticsResponse
    {
        /// <summary>
        /// All tasks belonging to the completed mission runs in the window.
        /// </summary>
        public int Total { get; set; }

        public int Successful { get; set; }

        public int PartiallySuccessful { get; set; }

        /// <summary>
        /// Fraction (0-1) of tasks that were Successful or PartiallySuccessful.
        /// </summary>
        public double SuccessRate { get; set; }
    }
}
