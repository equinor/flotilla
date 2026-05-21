using System.Text.Json.Serialization;
using Api.Database.Models;
using Api.Services.Models;

namespace Api.Controllers.Models;

public class TaskDefinitionResponse
{
    [JsonConstructor]
#nullable disable
    public TaskDefinitionResponse() { }

#nullable enable

    public TaskDefinitionResponse(TaskDefinition taskDefinition)
    {
        TagId = taskDefinition.TagId;
        Description = taskDefinition.Description;
        RobotPose = taskDefinition.RobotPose;
        TargetPosition = taskDefinition.TargetPosition;
        ZoomDescription = taskDefinition.ZoomDescription;
        AnalysisTypes = taskDefinition.AnalysisTypes;
        SensorType = taskDefinition.SensorType;
        VideoDuration = taskDefinition.VideoDuration;
    }

    public string? TagId { get; set; }

    public string? Description { get; set; }

    public Pose RobotPose { get; set; }

    public Position TargetPosition { get; set; }

    public IsarZoomDescription? ZoomDescription { get; set; }

    public IList<AnalysisType> AnalysisTypes { get; set; }

    public SensorType SensorType { get; set; }

    public float? VideoDuration { get; set; }
}
