using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json.Serialization;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;
namespace Api.Services.Models
{
    /// <summary>
    ///     The input ISAR expects as a mission description in the /schedule/start-mission endpoint
    /// </summary>
    public struct IsarMissionDefinition
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("tasks")]
        public List<IsarTaskDefinition> Tasks { get; set; }

        [JsonPropertyName("start_pose")]
        public IsarPose? StartPose { get; set; } = null;

        [JsonPropertyName("dock")]
        public bool? Dock { get; set; } = null;

        [JsonPropertyName("undock")]
        public bool? Undock { get; set; } = null;

        public IsarMissionDefinition(List<IsarTaskDefinition> tasks)
        {
            Name = null;
            Tasks = tasks;
        }

        public IsarMissionDefinition(MissionRun missionRun, bool includeStartPose = false, string? mapName = null)
        {
            Name = missionRun.Name;
            Tasks = missionRun.Tasks.Select(task => new IsarTaskDefinition(task, missionRun, mapName)).ToList();
            StartPose = includeStartPose && missionRun.InspectionArea.DefaultLocalizationPose != null ? new IsarPose(missionRun.InspectionArea.DefaultLocalizationPose.Pose) : null;
            Undock = includeStartPose && missionRun.InspectionArea.DefaultLocalizationPose != null && missionRun.InspectionArea.DefaultLocalizationPose.DockingEnabled;
            Dock = missionRun.InspectionArea.DefaultLocalizationPose != null && missionRun.InspectionArea.DefaultLocalizationPose.DockingEnabled && missionRun.IsReturnHomeMission();
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

        public IsarTaskDefinition(MissionTask missionTask, MissionRun missionRun, string? mapName = null)
        {
            Id = missionTask.IsarTaskId;
            Type = MissionTask.ConvertMissionTaskTypeToIsarTaskType(missionTask.Type);
            Pose = new IsarPose(missionTask.RobotPose);
            Tag = missionTask.TagId;
            Zoom = missionTask.IsarZoomDescription;

            if (missionTask.Inspection != null) Inspection = new IsarInspectionDefinition(missionTask.Inspection, missionRun, mapName);
        }
    }

    public struct IsarInspectionDefinition
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("inspection_target")]
        public IsarPosition? InspectionTarget { get; set; }

        [JsonPropertyName("duration")]
        public float? Duration { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string?>? Metadata { get; set; }

        public IsarInspectionDefinition(Inspection inspection, MissionRun missionRun, string? mapName = null)
        {
            Type = inspection.InspectionType.ToString();
            InspectionTarget = inspection.InspectionTarget != null ? new IsarPosition(
                inspection.InspectionTarget.X,
                inspection.InspectionTarget.Y,
                inspection.InspectionTarget.Z,
                "asset"
            ) : null;
            Duration = inspection.VideoDuration;
            Metadata = new Dictionary<string, string?>
            {
                { "map", mapName },
                { "description", missionRun.Description },
                { "estimated_duration", missionRun.EstimatedDuration?.ToString("D", CultureInfo.InvariantCulture) },
                { "asset_code", missionRun.InstallationCode },
                { "mission_name", missionRun.Name },
                { "status_reason", missionRun.StatusReason },
                { "analysis_type", inspection.AnalysisType?.ToString() }
            };
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
        public IsarPosition Position { get; } = new IsarPosition(pose.Position.X, pose.Position.Y, pose.Position.Z, "asset");

        [JsonPropertyName("orientation")]
        public IsarOrientation Orientation { get; } = new IsarOrientation(
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
