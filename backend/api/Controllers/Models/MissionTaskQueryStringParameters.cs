using Api.Database.Models;
using TaskStatus = Api.Database.Models.TaskStatus;

namespace Api.Controllers.Models
{
    public class MissionTaskQueryStringParameters : QueryStringParameters
    {
        public MissionTaskQueryStringParameters()
        {
            OrderBy = "TaskOrder";
        }

        /// <summary>
        /// Filter for tasks where AnalysisTypes contains any of the given values
        /// </summary>
        public List<AnalysisType>? AnalysisTypes { get; set; }

        /// <summary>
        /// Filter for current status of the task equal to any of Statuses
        /// </summary>
        public List<TaskStatus>? Statuses { get; set; }

        /// <summary>
        /// Filter for tasks where the inspection type equals any of the given values
        /// </summary>
        public List<SensorType>? InspectionTypes { get; set; }

        /// <summary>
        /// Filter for the installation code of the mission run the task belongs to
        /// </summary>
        public string? InstallationCode { get; set; }

        /// <summary>
        /// Filter for tasks belonging to a specific mission run
        /// </summary>
        public string? MissionRunId { get; set; }

        /// <summary>
        /// Search parameter for the tag id of the task
        /// </summary>
        public string? TagSearch { get; set; }

        #region Time Filters

        /// <summary>
        /// Filter for Start Time in epoch time format
        /// </summary>
        public long MinStartTime { get; set; }

        /// <summary>
        /// Filter for Start Time in epoch time format
        /// </summary>
        public long MaxStartTime { get; set; } = DateTimeOffset.MaxValue.ToUnixTimeSeconds();

        /// <summary>
        /// Filter for End Time in epoch time format
        /// </summary>
        public long MinEndTime { get; set; }

        /// <summary>
        /// Filter for End Time in epoch time format
        /// </summary>
        public long MaxEndTime { get; set; } = DateTimeOffset.MaxValue.ToUnixTimeSeconds();

        #endregion Time Filters
    }
}
