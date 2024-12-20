namespace Api.Controllers.Models
{
    public struct InspectionFindingQuery
    {
        public DateTime InspectionDate { get; set; }

        public string Finding { get; set; }
    }
}
