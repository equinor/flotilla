﻿namespace Api.Controllers.Models
{
    public class MissionDefinitionQueryStringParameters : QueryStringParameters
    {
        public MissionDefinitionQueryStringParameters()
        {
            // Default order is installation code
            OrderBy = "InstallationCode installationCode";
        }

        /// <summary>
        /// Filter for the installation code of the mission
        /// </summary>
        public string? InstallationCode { get; set; }

        /// <summary>
        /// Filter for the inspection area of the mission
        /// </summary>
        public string? InspectionArea { get; set; }

        /// <summary>
        /// The search parameter for the mission name
        /// </summary>
        public string? NameSearch { get; set; }

        /// <summary>
        /// The search parameter for the mission source id
        /// </summary>
        public string? SourceId { get; set; }
    }
}
