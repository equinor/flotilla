using Api.Database.Models;

namespace Api.Services
{
    public interface IAreaPolygonService
    {
        public bool MissionTasksAreInsideAreaPolygon(
            List<MissionTask> missionTasks,
            AreaPolygon? areaPolygon
        );

        public bool IsPositionInsidePolygon(
            List<PolygonPoint> polygon,
            Position position,
            double zMin,
            double zMax
        );
    }

    public class AreaPolygonService(ILogger<IAreaPolygonService> logger) : IAreaPolygonService
    {
        public bool MissionTasksAreInsideAreaPolygon(
            List<MissionTask> missionTasks,
            AreaPolygon? areaPolygon
        )
        {
            if (areaPolygon == null)
                return true;

            foreach (var missionTask in missionTasks)
            {
                var robotPosition = missionTask.RobotPose.Position;
                if (
                    !IsPositionInsidePolygon(
                        areaPolygon.Positions,
                        robotPosition,
                        areaPolygon.ZMin,
                        areaPolygon.ZMax
                    )
                )
                {
                    logger.LogWarning(
                        "Robot position {robotPosition} is outside the inspection area polygon for task {taskId}",
                        robotPosition,
                        missionTask.Id
                    );
                    return false;
                }
            }

            return true;
        }

        public bool IsPositionInsidePolygon(
            List<PolygonPoint> polygon,
            Position position,
            double zMin,
            double zMax
        )
        {
            var x = position.X;
            var y = position.Y;
            var z = position.Z;

            if (z < zMin || z > zMax)
            {
                return false;
            }

            // Ray-casting algorithm for checking if the point is inside the polygon
            var inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var xi = polygon[i].X;
                var yi = polygon[i].Y;
                var xj = polygon[j].X;
                var yj = polygon[j].Y;

                var intersect =
                    ((yi > y) != (yj > y)) && (x < (xj - xi) * (y - yi) / (yj - yi) + xi);
                if (intersect)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
