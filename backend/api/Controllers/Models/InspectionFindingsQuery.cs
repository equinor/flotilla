namespace Api.Controllers.Models
{
    public struct InspectionFindingsQuery
    {

        public DateTime InspectionDate { get; set; }

        public string Area { get; set; }

        public string IsarStepId { get; set; }

        public string Findings { get; set; }


    }

}

