using Api.Database.Models;

namespace Api.Controllers.Models
{
    public class MissionQueryStringParameters : QueryStringParameters
    {
        /// <summary>
        /// The current status of the mission
        /// </summary>
        public MissionStatus? Status { get; set; }

        /// <summary>
        /// The asset code of the mission
        /// </summary>
        public string? AssetCode { get; set; }

        /// <summary>
        /// The search parameter for the mission name
        /// </summary>
        public string? NameSearch { get; set; }
    }
}
