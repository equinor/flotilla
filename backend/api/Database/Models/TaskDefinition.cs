using System.ComponentModel.DataAnnotations;
using Api.Controllers.Models;
using Api.Services.Models;
using Microsoft.EntityFrameworkCore;
#pragma warning disable CS8618
namespace Api.Database.Models
{
    public enum AnalysisType
    {
        Fencilla,
        CLOE,
        ThermalReading,
        CO2,
    }

    [Owned]
    public class TaskDefinition
    {
        public TaskDefinition() { }

        public TaskDefinition(TaskQuery taskQuery, int index)
        {
            Index = index;
            TagId = taskQuery.TagId;
            Description = taskQuery.Description;
            RobotPose = taskQuery.RobotPose;
            ZoomDescription = taskQuery.ZoomDescription;
            AnalysisTypes = taskQuery.AnalysisTypes;
            SensorType = taskQuery.SensorType;
            TargetPosition = taskQuery.TargetPosition;
            VideoDuration = taskQuery.VideoDuration;
        }

        public int Index { get; set; }

        [MaxLength(200)]
        public string? TagId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public Pose RobotPose { get; set; }

        [Required]
        public Position TargetPosition { get; set; }

        public IsarZoomDescription? ZoomDescription { get; set; }

        public IList<AnalysisType> AnalysisTypes { get; set; } = [];

        public SensorType SensorType { get; set; }

        public float? VideoDuration { get; set; }

        public MissionTask ToMissionRunTask()
        {
            return new MissionTask
            {
                TaskOrder = this.Index,
                TagId = this.TagId,
                Description = this.Description,
                RobotPose = this.RobotPose,
                Status = TaskStatus.NotStarted,
                IsarZoomDescription = this.ZoomDescription,
                Inspection = new Inspection(
                    this.SensorType,
                    this.TargetPosition,
                    this.AnalysisTypes,
                    this.VideoDuration,
                    Description
                ),
            };
        }
    }
}
