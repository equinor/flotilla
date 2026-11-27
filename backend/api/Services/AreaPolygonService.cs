using Api.Database.Models;
using Api.Utilities;

namespace Api.Services
{
    public interface IAreaPolygonService
    {
        public bool MissionTasksAreInsideAreaPolygon(
            List<TaskDefinition> missionTasks,
            AreaPolygon? areaPolygon
        );

        public bool IsPositionInsidePolygon(
            List<PolygonPoint> polygon,
            Position position,
            double zMin,
            double zMax
        );

        public bool DoPolygonsOverlap(AreaPolygon polygonA, AreaPolygon polygonB);
    }

    public class AreaPolygonService() : IAreaPolygonService
    {
        private const double Epsilon = 1e-9;

        public bool MissionTasksAreInsideAreaPolygon(
            List<TaskDefinition> missionTasks,
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
            if (position.Z < zMin || position.Z > zMax)
            {
                return false;
            }

            return IsPointInsidePolygon2D(polygon, position.X, position.Y);
        }

        public bool DoPolygonsOverlap(AreaPolygon polygonA, AreaPolygon polygonB)
        {
            var ptsA = polygonA.Positions;
            var ptsB = polygonB.Positions;

            if (ptsA.Count < 3 || ptsB.Count < 3)
            {
                return false;
            }

            if (polygonA.ZMax < polygonB.ZMin || polygonB.ZMax < polygonA.ZMin)
            {
                return false;
            }

            if (!BoundingBoxesOverlap(ptsA, ptsB))
            {
                return false;
            }

            for (int i = 0; i < ptsA.Count; i++)
            {
                var a1 = ptsA[i];
                var a2 = ptsA[(i + 1) % ptsA.Count];

                for (int j = 0; j < ptsB.Count; j++)
                {
                    var b1 = ptsB[j];
                    var b2 = ptsB[(j + 1) % ptsB.Count];

                    if (SegmentsIntersect(a1, a2, b1, b2))
                    {
                        return true;
                    }
                }
            }

            // No edges intersect; overlap is still possible if one polygon is completely inside the other
            return IsPointInsidePolygon2D(ptsB, ptsA[0].X, ptsA[0].Y)
                || IsPointInsidePolygon2D(ptsA, ptsB[0].X, ptsB[0].Y);
        }

        private static bool IsPointInsidePolygon2D(List<PolygonPoint> polygon, double x, double y)
        {
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

        private static bool BoundingBoxesOverlap(List<PolygonPoint> a, List<PolygonPoint> b)
        {
            var minXA = a.Min(p => p.X);
            var maxXA = a.Max(p => p.X);
            var minYA = a.Min(p => p.Y);
            var maxYA = a.Max(p => p.Y);

            var minXB = b.Min(p => p.X);
            var maxXB = b.Max(p => p.X);
            var minYB = b.Min(p => p.Y);
            var maxYB = b.Max(p => p.Y);

            return maxXA >= minXB - Epsilon
                && maxXB >= minXA - Epsilon
                && maxYA >= minYB - Epsilon
                && maxYB >= minYA - Epsilon;
        }

        private static bool SegmentsIntersect(
            PolygonPoint p1,
            PolygonPoint p2,
            PolygonPoint q1,
            PolygonPoint q2
        )
        {
            var o1 = Cross(p1, p2, q1);
            var o2 = Cross(p1, p2, q2);
            var o3 = Cross(q1, q2, p1);
            var o4 = Cross(q1, q2, p2);

            if (
                ((o1 > Epsilon && o2 < -Epsilon) || (o1 < -Epsilon && o2 > Epsilon))
                && ((o3 > Epsilon && o4 < -Epsilon) || (o3 < -Epsilon && o4 > Epsilon))
            )
            {
                return true;
            }

            return (Math.Abs(o1) <= Epsilon && IsPointOnSegment(p1, p2, q1))
                || (Math.Abs(o2) <= Epsilon && IsPointOnSegment(p1, p2, q2))
                || (Math.Abs(o3) <= Epsilon && IsPointOnSegment(q1, q2, p1))
                || (Math.Abs(o4) <= Epsilon && IsPointOnSegment(q1, q2, p2));
        }

        private static double Cross(PolygonPoint a, PolygonPoint b, PolygonPoint c) =>
            (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

        private static bool IsPointOnSegment(PolygonPoint a, PolygonPoint b, PolygonPoint p) =>
            p.X >= Math.Min(a.X, b.X) - Epsilon
            && p.X <= Math.Max(a.X, b.X) + Epsilon
            && p.Y >= Math.Min(a.Y, b.Y) - Epsilon
            && p.Y <= Math.Max(a.Y, b.Y) + Epsilon;
    }
}
