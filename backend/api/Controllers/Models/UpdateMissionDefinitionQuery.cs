namespace Api.Controllers.Models
{
    public struct UpdateMissionDefinitionQuery
    {
        /// <summary>
        /// Change the comment describing the mission definition
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Change the display name
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Change the inspection frequency
        /// </summary>
        public TimeSpan? InspectionFrequency { get; set; }
    }
}
