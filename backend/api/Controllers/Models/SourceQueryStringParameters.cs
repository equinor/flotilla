namespace Api.Controllers.Models
{
    public class SourceQueryStringParameters : QueryStringParameters
    {
        /// <summary>
        /// The search parameter for the task string
        /// </summary>
        public string? TaskNameSearch { get; set; }
    }
}
