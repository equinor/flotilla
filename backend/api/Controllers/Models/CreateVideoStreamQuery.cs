namespace Api.Controllers.Models
{
    public struct CreateVideoStreamQuery
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public string Type { get; set; }
    }
}
