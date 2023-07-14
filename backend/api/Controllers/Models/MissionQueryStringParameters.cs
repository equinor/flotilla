﻿using Api.Database.Models;

namespace Api.Controllers.Models
{
    public class MissionQueryStringParameters : QueryStringParameters
    {
        public MissionQueryStringParameters()
        {
            // Default order is desired start time
            OrderBy = "DesiredStartTime desc";
        }

        /// <summary>
        /// Filter for current status of the mission equal to any of Statuses
        /// </summary>
        public List<MissionStatus>? Statuses { get; set; }

        /// <summary>
        /// Filter for the asset code of the mission
        /// </summary>
        public string? AssetCode { get; set; }

        /// <summary>
        /// Filter for the robot id of the robot assigned to the mission
        /// </summary>
        public string? RobotId { get; set; }

        /// <summary>
        /// Filter for the robot model type of the robot assigned to the mission
        /// </summary>
        public RobotType? RobotModelType { get; set; }

        /// <summary>
        /// The search parameter for the mission name
        /// </summary>
        public string? NameSearch { get; set; }

        /// <summary>
        /// The search parameter for the name of the robot assigned to the mission
        /// </summary>
        public string? RobotNameSearch { get; set; }

        /// <summary>
        /// The search parameter for a tag in the mission
        /// </summary>
        public string? TagSearch { get; set; }

        #region Time Filters

        /// <summary>
        /// Filter for Start Time in epoch time format
        /// </summary>
        public long MinStartTime { get; set; }

        /// /// <summary>
        /// Filter for Start Time in epoch time format
        /// </summary>
        public long MaxStartTime { get; set; } = DateTimeOffset.MaxValue.ToUnixTimeSeconds();

        /// <summary>
        /// Filter for Start Time in epoch time format
        /// </summary>
        public long MinEndTime { get; set; }

        /// /// <summary>
        /// Filter for Start Time in epoch time format
        /// </summary>
        public long MaxEndTime { get; set; } = DateTimeOffset.MaxValue.ToUnixTimeSeconds();

        /// <summary>
        /// Filter for Start Time in epoch time format
        /// </summary>
        public long MinDesiredStartTime { get; set; }

        /// /// <summary>
        /// Filter for Start Time in epoch time format
        /// </summary>
        public long MaxDesiredStartTime { get; set; } = DateTimeOffset.MaxValue.ToUnixTimeSeconds();

        #endregion Time Filters

    }
}
