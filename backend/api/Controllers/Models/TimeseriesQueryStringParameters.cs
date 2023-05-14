namespace Api.Controllers.Models
{
    public class TimeseriesQueryStringParameters : QueryStringParameters
    {
        /// <summary>
        /// Filter for the mission which was running at the time of logging the timeseries
        /// </summary>
        public string? MissionId { get; set; }

        /// <summary>
        /// Filter for the robot id to which the timeseries belong to
        /// </summary>
        public string? RobotId { get; set; }

        /// <summary>
        /// Filter for min time in epoch time format
        /// </summary>
        public long MinTime { get; set; }

        /// /// <summary>
        /// Filter for max time in epoch time format
        /// </summary>
        public long MaxTime { get; set; } = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
    }
}
