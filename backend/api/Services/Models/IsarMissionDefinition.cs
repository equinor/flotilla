using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Api.Database.Models.TaskStatus;

namespace Api.Services.Models
{
    /// <summary>
    ///     The input ISAR expects as a mission description in the /schedule/start-mission endpoint
    /// </summary>
    public struct IsarMissionDefinition
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("tasks")]
        public List<IsarTaskDefinition> Tasks { get; set; }

        public IsarMissionDefinition(List<IsarTaskDefinition> tasks)
        {
            Name = null;
            Tasks = tasks;
        }

        public IsarMissionDefinition(MissionRun missionRun)
        {
            Id = missionRun.Id;
            Name = missionRun.Name;
            // Filtering on status to remove completed tasks in case it is a resumed mission
            Tasks =
            [
                .. missionRun
                    .Tasks.Where(task =>
                        task.Status != TaskStatus.Successful && task.Status != TaskStatus.Failed
                    )
                    .Select(task => new IsarTaskDefinition(task)),
            ];
        }
    }

    public struct IsarStopMissionDefinition
    {
        [JsonPropertyName("mission_id")]
        public string? MissionId { get; set; }

        public IsarStopMissionDefinition(string? missionId)
        {
            MissionId = missionId;
        }
    }

    public struct IsarTaskDefinition
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("pose")]
        public IsarPose Pose { get; set; }

        [JsonPropertyName("tag")]
        public string? Tag { get; set; }

        [JsonPropertyName("inspection")]
        public IsarInspectionDefinition? Inspection { get; set; }

        [JsonPropertyName("zoom")]
        public IsarZoomDescription? Zoom { get; set; }

        public IsarTaskDefinition(MissionTask missionTask)
        {
            Id = missionTask.Id;
            Type = MissionTask.GetIsarInspectionTaskType();
            Pose = new IsarPose(missionTask.RobotPose);
            Tag = missionTask.TagId;
            Zoom = missionTask.IsarZoomDescription;

            if (missionTask.Inspection != null)
                Inspection = new IsarInspectionDefinition(missionTask);
        }
    }

    public struct IsarInspectionDefinition
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("inspection_target")]
        public IsarPosition? InspectionTarget { get; set; }

        [JsonPropertyName("inspection_description")]
        public string? InspectionDescription { get; set; }

        [JsonPropertyName("duration")]
        public float? Duration { get; set; }

        public IsarInspectionDefinition(MissionTask missionTask)
        {
            var inspection = missionTask.Inspection!;
            Type = inspection.InspectionType.ToString();
            InspectionTarget =
                inspection.InspectionTarget != null
                    ? new IsarPosition(
                        inspection.InspectionTarget.X,
                        inspection.InspectionTarget.Y,
                        inspection.InspectionTarget.Z,
                        "asset"
                    )
                    : null;
            InspectionDescription = missionTask.Description;
            Duration = inspection.VideoDuration;
        }
    }

    public readonly struct IsarOrientation(float x, float y, float z, float w, string frameName)
    {
        [JsonPropertyName("x")]
        public float X { get; } = x;

        [JsonPropertyName("y")]
        public float Y { get; } = y;

        [JsonPropertyName("z")]
        public float Z { get; } = z;

        [JsonPropertyName("w")]
        public float W { get; } = w;

        [JsonPropertyName("frame_name")]
        public string FrameName { get; } = frameName;
    }

    public readonly struct IsarPosition(float x, float y, float z, string frameName)
    {
        [JsonPropertyName("x")]
        public float X { get; } = x;

        [JsonPropertyName("y")]
        public float Y { get; } = y;

        [JsonPropertyName("z")]
        public float Z { get; } = z;

        [JsonPropertyName("frame_name")]
        public string FrameName { get; } = frameName;
    }

    public readonly struct IsarPose(Pose pose)
    {
        [JsonPropertyName("position")]
        public IsarPosition Position { get; } =
            new IsarPosition(pose.Position.X, pose.Position.Y, pose.Position.Z, "asset");

        [JsonPropertyName("orientation")]
        public IsarOrientation Orientation { get; } =
            new IsarOrientation(
                pose.Orientation.X,
                pose.Orientation.Y,
                pose.Orientation.Z,
                pose.Orientation.W,
                "asset"
            );

        [JsonPropertyName("frame_name")]
        public string FrameName { get; } = "asset";
    }

    [Owned]
    public class IsarZoomDescription(double objectWidth, double objectHeight)
    {
        [Required]
        [JsonPropertyName("objectWidth")]
        public double ObjectWidth { get; set; } = objectWidth;

        [Required]
        [JsonPropertyName("objectHeight")]
        public double ObjectHeight { get; set; } = objectHeight;
    }
}
