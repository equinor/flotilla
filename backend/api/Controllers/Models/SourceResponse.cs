namespace Api.Database.Models
{
    public class SourceResponse
    {
        public string Id { get; set; }

        public string SourceId { get; set; }

        public MissionSourceType Type { get; set; }

        public IList<MissionTask> Tasks;

        public SourceResponse(Source source, IList<MissionTask> tasks)
        {
            Id = source.Id;
            SourceId = source.SourceId;
            Type = source.Type;
            Tasks = tasks;
        }
    }
}
