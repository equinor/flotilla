using Api.Database.Models;

namespace Api.Controllers.Models
{
    public class SourceQueryStringParameters : QueryStringParameters
    {
        public SourceQueryStringParameters()
        {
            // Default order is mission source type
            OrderBy = "MissionSourceType type";
        }

        /// <summary>
        /// Filter based on mission source type
        /// </summary>
        public MissionSourceType? Type { get; set; }
    }
}
