using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable
namespace Api.Database.Models
{
    public class Mission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(450)]
        public string Description { get; set; }

        [MaxLength(450)]
        public string StatusReason { get; set; }

        [MaxLength(1000)]
        public string Comment { get; set; }

        [MaxLength(200)]
        public string AssetCode { get; set; }

        [Required]
        public virtual Robot Robot { get; set; }

        [MaxLength(200)]
        public string IsarMissionId { get; set; }

        [Required]
        [MaxLength(200)]
        public int EchoMissionId { get; set; }

        private MissionStatus _missionStatus;

        [Required]
        public MissionStatus MissionStatus
        {
            get { return _missionStatus; }
            set
            {
                _missionStatus = value;
                if (IsCompleted)
                    EndTime = DateTimeOffset.UtcNow;
            }
        }

        [Required]
        public MissionMap Map { get; set; }

        [Required]
        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        public TimeSpan EstimatedDuration { get; set; }

        [Required]
        public IList<IsarTask> Tasks { get; set; }

        private IList<PlannedTask> _plannedTasks;

        // The planned tasks are always returned ordered by their order field
        [Required]
        public IList<PlannedTask> PlannedTasks
        {
            get { return _plannedTasks.OrderBy(t => t.PlanOrder).ToList(); }
            set { _plannedTasks = value; }
        }

        public bool IsCompleted =>
            new[]
            {
                MissionStatus.Aborted,
                MissionStatus.Cancelled,
                MissionStatus.Successful,
                MissionStatus.PartiallySuccessful,
                MissionStatus.Failed
            }.Contains(_missionStatus);

#nullable enable
        public IsarTask? ReadIsarTaskById(string isarTaskId)
        {
            return Tasks.FirstOrDefault(
                task => task.IsarTaskId.Equals(isarTaskId, StringComparison.Ordinal)
            );
        }

#nullable disable

        public static MissionStatus MissionStatusFromString(string status)
        {
            return status switch
            {
                "successful" => MissionStatus.Successful,
                "not_started" => MissionStatus.Pending,
                "in_progress" => MissionStatus.Ongoing,
                "failed" => MissionStatus.Failed,
                "cancelled" => MissionStatus.Cancelled,
                "paused" => MissionStatus.Paused,
                "partially_successful" => MissionStatus.PartiallySuccessful,
                _
                  => throw new ArgumentException(
                      $"Failed to parse mission status '{status}' as it's not supported"
                  )
            };
        }

        public void CalculateEstimatedDuration()
        {
            double robotVelocity = 1.5 * 1000 / 60; // km/t => m/min
            double efficiencyFactor = 0.20;
            double inspectionTime = 2; // min/tag
            int assumedXYMetersFromFirst = 20;

            double distance = 0;
            int numberOfTags = 0;
            var prevPosition = new Position(PlannedTasks.First().Pose.Position.X + assumedXYMetersFromFirst, PlannedTasks.First().Pose.Position.Y + assumedXYMetersFromFirst, PlannedTasks.First().Pose.Position.Z);
            foreach (var plannedTask in PlannedTasks)
            {
                numberOfTags += plannedTask.Inspections.Count;
                var currentPosition = plannedTask.Pose.Position;
                distance += Math.Abs(currentPosition.X - prevPosition.X) + Math.Abs(currentPosition.Y - prevPosition.Y);
                prevPosition = currentPosition;
            }
            int estimate = (int)((distance / (robotVelocity * efficiencyFactor)) + (numberOfTags * inspectionTime));
            EstimatedDuration = TimeSpan.FromMinutes(estimate);
        }
    }

    public enum MissionStatus
    {
        Pending,
        Ongoing,
        Paused,
        Aborted,
        Cancelled,
        Failed,
        Successful,
        PartiallySuccessful
    }
}
