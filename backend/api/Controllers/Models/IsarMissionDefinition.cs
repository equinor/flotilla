using System.Text.Json.Serialization;
using Api.Database.Models;
using Api.Utilities;
namespace Api.Controllers.Models
{
    /// <summary>
    /// The input ISAR expects as a mission description in the /schedule/start-mission endpoint
    /// </summary>
    public struct IsarMissionDefinition
    {
        [JsonPropertyName("tasks")]
        public List<IsarTaskDefinition> Tasks { get; set; }

        public IsarMissionDefinition(List<IsarTaskDefinition> tasks)
        {
            Tasks = tasks;
        }
    }

    public struct IsarInspectionDefinition
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("inspection_target")]
        public IsarPosition InspectionTarget { get; set; }

        [JsonPropertyName("analysis_types")]
        public string? AnalysisTypes { get; set; }

        [JsonPropertyName("duration")]
        public float? Duration { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }

        public IsarInspectionDefinition(PlannedInspection plannedInspection, PlannedTask plannedTask, Mission mission)
        {
            Type = plannedInspection.InspectionType.ToString();
            InspectionTarget = new IsarPosition(
                plannedTask.TagPosition.X,
                plannedTask.TagPosition.Y,
                plannedTask.TagPosition.Z,
                "asset"
            );
            AnalysisTypes = plannedInspection.AnalysisTypes;
            Duration = plannedInspection.TimeInSeconds;
            var metadata = new Dictionary<string, string>
            {
                { "map", mission.Map.MapName },
                { "description", mission.Description },
                { "estimated_duration", mission.EstimatedDuration.ToString() },
                { "asset_code", mission.AssetCode },
                { "mission_name", mission.Name },
                { "status_reason", mission.StatusReason }
            };
            Metadata = metadata;
        }
    }

    public struct IsarTaskDefinition
    {
        [JsonPropertyName("pose")]
        public IsarPose Pose { get; set; }

        [JsonPropertyName("tag")]
        public string Tag { get; set; }

        [JsonPropertyName("inspections")]
        public List<IsarInspectionDefinition> Inspections { get; set; }

        public IsarTaskDefinition(PlannedTask plannedTask, Mission mission)
        {
            Pose = new IsarPose(plannedTask.Pose);
            Tag = plannedTask.TagId;
            var isarInspections = new List<IsarInspectionDefinition>();
            foreach (var inspection in plannedTask.Inspections)
            {
                isarInspections.Add(new IsarInspectionDefinition(inspection, plannedTask, mission));
            }
            Inspections = isarInspections;
        }
    }

    public struct IsarOrientation
    {
        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("z")]
        public float Z { get; set; }

        [JsonPropertyName("w")]
        public float W { get; set; }

        [JsonPropertyName("frame_name")]
        public string FrameName { get; set; }

        public IsarOrientation(float x, float y, float z, float w, string frameName)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
            FrameName = frameName;
        }
    }

    public struct IsarPosition
    {
        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("z")]
        public float Z { get; set; }

        [JsonPropertyName("frame_name")]
        public string FrameName { get; set; }

        public IsarPosition(float x, float y, float z, string frameName)
        {
            X = x;
            Y = y;
            Z = z;
            FrameName = frameName;
        }
    }

    public struct IsarPose
    {
        [JsonPropertyName("position")]
        public IsarPosition Position { get; set; }

        [JsonPropertyName("orientation")]
        public IsarOrientation Orientation { get; set; }

        [JsonPropertyName("frame_name")]
        public string FrameName { get; set; }

        public IsarPose(IsarPosition position, IsarOrientation orientation, string frameName)
        {
            Position = position;
            Orientation = orientation;
            FrameName = frameName;
        }

        public IsarPose(PredefinedPose predefinedPose)
        {
            Position = new IsarPosition(
                predefinedPose.Pose.Position.X,
                predefinedPose.Pose.Position.Y,
                predefinedPose.Pose.Position.Z,
                predefinedPose.Pose.Frame
            );
            Orientation = new IsarOrientation(
                predefinedPose.Pose.Orientation.X,
                predefinedPose.Pose.Orientation.Y,
                predefinedPose.Pose.Orientation.Z,
                predefinedPose.Pose.Orientation.W,
                predefinedPose.Pose.Frame
            );
            FrameName = predefinedPose.Pose.Frame;
        }
        public IsarPose(Pose pose)
        {
            Position = new IsarPosition(pose.Position.X, pose.Position.Y, pose.Position.Z, pose.Frame);
            Orientation = new IsarOrientation(pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W, pose.Frame);
            FrameName = pose.Frame;
        }
    }
}
