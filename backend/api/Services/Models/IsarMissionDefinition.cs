using System.Globalization;
using System.Text.Json.Serialization;
using Api.Database.Models;
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

        [JsonPropertyName("start_pose")]
        public IsarPose? StartPose { get; set; } = null;

        [JsonPropertyName("dock")]
        public bool? Dock { get; set; } = null;

        [JsonPropertyName("undock")]
        public bool? Undock { get; set; } = null;

        public IsarMissionDefinition(List<IsarTaskDefinition> tasks)
        {
            Id = null;
            Name = null;
            Tasks = tasks;
        }

        public IsarMissionDefinition(MissionRun missionRun, bool includeStartPose = false)
        {
            Id = missionRun.IsarMissionId;
            Name = missionRun.Name;
            Tasks = missionRun.Tasks.Select(task => new IsarTaskDefinition(task, missionRun)).ToList();
            StartPose = includeStartPose && missionRun.Area.Deck.DefaultLocalizationPose != null ? new IsarPose(missionRun.Area.Deck.DefaultLocalizationPose.Pose) : null;
            Undock = includeStartPose && missionRun.Area.Deck.DefaultLocalizationPose != null && missionRun.Area.Deck.DefaultLocalizationPose.DockingEnabled;
            Dock = missionRun.Area.Deck.DefaultLocalizationPose != null && missionRun.Area.Deck.DefaultLocalizationPose.DockingEnabled && missionRun.IsReturnHomeMission();
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

        public IsarTaskDefinition(MissionTask missionTask, MissionRun missionRun)
        {
            Id = missionTask.IsarTaskId;
            Type = MissionTask.ConvertMissionTaskTypeToIsarTaskType(missionTask.Type);
            Pose = new IsarPose(missionTask.RobotPose);
            Tag = missionTask.TagId;
            if (missionTask.Inspection != null) Inspection = new IsarInspectionDefinition(missionTask.Inspection, missionRun);
        }
    }

    public struct IsarInspectionDefinition
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("inspection_target")]
        public IsarPosition? InspectionTarget { get; set; }

        [JsonPropertyName("duration")]
        public float? Duration { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string?>? Metadata { get; set; }

        public IsarInspectionDefinition(Inspection inspection, MissionRun missionRun)
        {
            Id = inspection.Id;
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
                { "map", missionRun.Map?.MapName },
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
}
