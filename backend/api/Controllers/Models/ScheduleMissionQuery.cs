﻿namespace Api.Controllers.Models
{
    public class ScheduleMissionQuery
    {
        public string RobotId { get; set; } = string.Empty;
        public DateTime? DesiredStartTime { get; set; }
    }
}
