using Api.Database.Models;

namespace Api.Controllers.Models
{
    public struct CreateRobotModelQuery
    {
        /// <summary>
        /// The type of robot model
        /// </summary>
        public RobotType RobotType { get; set; }
    }
}
