using System.Text.Json.Serialization;
using Api.Database.Models;
using Api.Utilities;

namespace Api.Services.Models
{
    /// <summary>
    /// The input ISAR expects as a mission description in the /schedule/start-mission endpoint
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
            Id = null;
            Name = null;
            Tasks = tasks;
        }

        public IsarMissionDefinition(MissionRun missionRun)
        {
            Id = missionRun.IsarMissionId;
            Name = missionRun.Name;
            Tasks = missionRun.Tasks.Select(task => new IsarTaskDefinition(task, missionRun)).ToList();
        }
    }

    public struct IsarTaskDefinition
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("pose")]
        public IsarPose Pose { get; set; }

        [JsonPropertyName("tag")]
        public string? Tag { get; set; }

        [JsonPropertyName("inspections")]
        public List<IsarInspectionDefinition> Inspections { get; set; }

        public IsarTaskDefinition(MissionTask missionTask, MissionRun missionRun)
        {
            Id = missionTask.IsarTaskId;
            Pose = new IsarPose(missionTask.RobotPose);
            Tag = missionTask.TagId;
            var isarInspections = new List<IsarInspectionDefinition>();
            foreach (var inspection in missionTask.Inspections)
            {
                isarInspections.Add(new IsarInspectionDefinition(inspection, missionTask, missionRun));
            }
            Inspections = isarInspections;
        }
    }

    public struct IsarInspectionDefinition
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("inspection_target")]
        public IsarPosition InspectionTarget { get; set; }

        [JsonPropertyName("analysis_types")]
        public string? AnalysisTypes { get; set; }

        [JsonPropertyName("duration")]
        public float? Duration { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string?>? Metadata { get; set; }

        public IsarInspectionDefinition(Inspection inspection, MissionTask task, MissionRun missionRun)
        {
            Id = inspection.IsarStepId;
            Type = inspection.InspectionType.ToString();
            InspectionTarget = new IsarPosition(
                task.InspectionTarget.X,
                task.InspectionTarget.Y,
                task.InspectionTarget.Z,
                "asset"
            );
            AnalysisTypes = inspection.AnalysisTypes;
            Duration = inspection.VideoDuration;
            var metadata = new Dictionary<string, string?>
            {
                { "map", missionRun.MapMetadata?.MapName },
                { "description", missionRun.Description },
                { "estimated_duration", missionRun.EstimatedDuration.ToString() },
                { "asset_code", missionRun.AssetCode },
                { "mission_name", missionRun.Name },
                { "status_reason", missionRun.StatusReason }
            };
            Metadata = metadata;
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
                "asset"
            );
            Orientation = new IsarOrientation(
                predefinedPose.Pose.Orientation.X,
                predefinedPose.Pose.Orientation.Y,
                predefinedPose.Pose.Orientation.Z,
                predefinedPose.Pose.Orientation.W,
                "asset"
            );
            FrameName = "asset";
        }

        public IsarPose(Pose pose)
        {
            Position = new IsarPosition(pose.Position.X, pose.Position.Y, pose.Position.Z, "asset");
            Orientation = new IsarOrientation(
                pose.Orientation.X,
                pose.Orientation.Y,
                pose.Orientation.Z,
                pose.Orientation.W,
                "asset"
            );
            FrameName = "asset";
        }
    }
}
