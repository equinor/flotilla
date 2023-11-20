﻿namespace Api.Database.Models
{
    public class SourceResponse(Source source, IList<MissionTask> tasks)
    {
        public string Id { get; } = source.Id;

        public string SourceId { get; } = source.SourceId;

        public MissionSourceType Type { get; } = source.Type;

        public IList<MissionTask> Tasks = tasks;
    }
}
