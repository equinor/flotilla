namespace Api.Controllers.Models
{
    public struct CreateInspectionAreaQuery
    {
        public string InstallationCode { get; set; }
        public string PlantCode { get; set; }
        public string Name { get; set; }

        public CreateDefaultLocalizationPose? DefaultLocalizationPose { get; set; }
    }
}
