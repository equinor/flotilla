namespace Api.Controllers.Models
{
    public struct CreateInspectionGroupQuery
    {
        public string InstallationCode { get; set; }
        public string Name { get; set; }

        public CreateDefaultLocalizationPose? DefaultLocalizationPose { get; set; }
    }
}
