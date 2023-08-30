using Api.Database.Models;

namespace Api.Controllers.Models
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
        /// Filter for the area of the mission
        /// </summary>
        public string? Area { get; set; }

        /// <summary>
        /// The search parameter for the mission name
        /// </summary>
        public string? NameSearch { get; set; }

        /// <summary>
        /// The search parameter for the mission source type
        /// </summary>
        public MissionSourceType? SourceType { get; set; }
    }
}
