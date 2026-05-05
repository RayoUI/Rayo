using System.Numerics;

namespace Rayo.Rendering.Graphics.VectorGraphics;

/// <summary>
/// Converts vector paths into triangulated geometry for rendering.
/// </summary>
public class PathTessellator
{
    private const int DefaultSegments = 32;

    /// <summary>
    /// Tessellates the fill of a vector path into a list of triangles.
    /// </summary>
    public static List<Vector2> TessellateFill(VectorPath path, int segments = DefaultSegments)
    {
        var points = FlattenPath(path, segments);
        return TriangulatePolygon(points);
    }

    /// <summary>
    /// Tessellates the stroke (outline) of a vector path with a specified thickness.
    /// </summary>
    public static List<Vector2> TessellateStroke(VectorPath path, float thickness, int segments = DefaultSegments)
    {
        var points = FlattenPath(path, segments);
        return GenerateStroke(points, thickness);
    }

    /// <summary>
    /// Robust fill tessellation using algorithms similar to Skia.
    /// Produces much smoother results for small curves.
    /// </summary>
    public static List<Vector2> TessellateFillAdvanced(VectorPath path, int segments = DefaultSegments)
    {
        var points = AdvancedPathTessellator.TesselatePath(path, segments);
        return TriangulatePolygon(points);
    }

    /// <summary>
    /// Robust stroke tessellation using algorithms similar to Skia.
    /// Produces much smoother results for small edges.
    /// </summary>
    public static List<Vector2> TessellateStrokeAdvanced(VectorPath path, float thickness, int segments = DefaultSegments)
    {
        return AdvancedPathTessellator.TessellateStrokeAdvanced(path, thickness, segments);
    }

    private static List<Vector2> FlattenPath(VectorPath path, int segments)
    {
        var points = new List<Vector2>();
        Vector2 currentPoint = Vector2.Zero;
        Vector2 startPoint = Vector2.Zero;

        foreach (var cmd in path.Commands)
        {
            switch (cmd.Type)
            {
                case PathCommandType.MoveTo:
                    currentPoint = cmd.Point;
                    startPoint = cmd.Point;
                    points.Add(currentPoint);
                    break;

                case PathCommandType.LineTo:
                    points.Add(cmd.Point);
                    currentPoint = cmd.Point;
                    break;

                case PathCommandType.QuadraticBezierTo:
                    var quadPoints = TessellateQuadraticBezier(currentPoint, cmd.ControlPoint1, cmd.Point, segments);
                    points.AddRange(quadPoints);
                    currentPoint = cmd.Point;
                    break;

                case PathCommandType.CubicBezierTo:
                    var cubicPoints = TessellateCubicBezier(currentPoint, cmd.ControlPoint1, cmd.ControlPoint2, cmd.Point, segments);
                    points.AddRange(cubicPoints);
                    currentPoint = cmd.Point;
                    break;

                case PathCommandType.ArcTo:
                    var arcPoints = TessellateArc(currentPoint, cmd.Point, cmd.Radius, cmd.StartAngle, cmd.SweepAngle, segments);
                    points.AddRange(arcPoints);
                    currentPoint = cmd.Point;
                    break;

                case PathCommandType.Close:
                    if (Vector2.Distance(currentPoint, startPoint) > 0.001f)
                    {
                        points.Add(startPoint);
                    }
                    currentPoint = startPoint;
                    break;
            }
        }

        return points;
    }

    private static List<Vector2> TessellateQuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, int segments)
    {
        var points = new List<Vector2>();

        // Dynamically scale segment count based on curvature.
        // For very curved or small segments, use more subdivisions.
        float dist = Vector2.Distance(p0, p2);
        float controlDist = Vector2.Distance(p1, (p0 + p2) / 2);
        float curvature = controlDist / Math.Max(dist, 1.0f);
        int adaptiveSegments = Math.Max(segments, (int)(curvature * segments * 2));

        for (int i = 1; i <= adaptiveSegments; i++)
        {
            float t = (float)i / adaptiveSegments;
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            var point = uu * p0 + 2 * u * t * p1 + tt * p2;
            points.Add(point);
        }

        return points;
    }

    private static List<Vector2> TessellateCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int segments)
    {
        var points = new List<Vector2>();

        // Dynamically scale segment count based on curvature.
        float dist = Vector2.Distance(p0, p3);
        float controlDist = MathF.Max(
            Vector2.Distance(p1, (p0 + p3) / 2),
            Vector2.Distance(p2, (p0 + p3) / 2)
        );
        float curvature = controlDist / Math.Max(dist, 1.0f);
        int adaptiveSegments = Math.Max(segments, (int)(curvature * segments * 2));

        for (int i = 1; i <= adaptiveSegments; i++)
        {
            float t = (float)i / adaptiveSegments;
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float ttt = tt * t;
            float uuu = uu * u;

            var point = uuu * p0 + 3 * uu * t * p1 + 3 * u * tt * p2 + ttt * p3;
            points.Add(point);
        }

        return points;
    }

    private static List<Vector2> TessellateArc(Vector2 start, Vector2 end, float radius, float startAngle, float sweepAngle, int segments)
    {
        var points = new List<Vector2>();
        Vector2 center = CalculateArcCenter(start, end, radius, sweepAngle > 0);

        // Dynamically compute segment count from radius and sweep angle.
        // Formula: segments = max(3, ceil(radius * |sweep| / tolerance))
        // where tolerance is the maximum allowed pixel deviation.
        float tolerance = 0.5f;
        float angleRadians = MathF.Abs(sweepAngle);
        int calculatedSegments = Math.Max(3, (int)MathF.Ceiling(radius * angleRadians / tolerance));
        int finalSegments = Math.Max(calculatedSegments, segments);

        for (int i = 1; i <= finalSegments; i++)
        {
            float angle = startAngle + sweepAngle * i / finalSegments;
            float x = center.X + MathF.Cos(angle) * radius;
            float y = center.Y + MathF.Sin(angle) * radius;
            points.Add(new Vector2(x, y));
        }

        return points;
    }

    private static Vector2 CalculateArcCenter(Vector2 start, Vector2 end, float radius, bool clockwise)
    {
        Vector2 mid = (start + end) / 2;
        Vector2 dir = Vector2.Normalize(end - start);
        Vector2 perp = new Vector2(-dir.Y, dir.X);

        if (!clockwise)
            perp = -perp;

        float d = Vector2.Distance(start, end) / 2;
        float h = MathF.Sqrt(Math.Max(0, radius * radius - d * d));

        return mid + perp * h;
    }

    private static List<Vector2> TriangulatePolygon(List<Vector2> points)
    {
        if (points.Count < 3)
            return new List<Vector2>();

        var triangles = new List<Vector2>();
        var indices = new List<int>();

        for (int i = 0; i < points.Count; i++)
            indices.Add(i);

        while (indices.Count > 3)
        {
            bool earFound = false;

            for (int i = 0; i < indices.Count; i++)
            {
                int prev = indices[(i - 1 + indices.Count) % indices.Count];
                int curr = indices[i];
                int next = indices[(i + 1) % indices.Count];

                if (IsEar(points, indices, prev, curr, next))
                {
                    triangles.Add(points[prev]);
                    triangles.Add(points[curr]);
                    triangles.Add(points[next]);

                    indices.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }

            if (!earFound)
                break;
        }

        if (indices.Count == 3)
        {
            triangles.Add(points[indices[0]]);
            triangles.Add(points[indices[1]]);
            triangles.Add(points[indices[2]]);
        }

        return triangles;
    }

    private static bool IsEar(List<Vector2> points, List<int> indices, int prev, int curr, int next)
    {
        var p0 = points[prev];
        var p1 = points[curr];
        var p2 = points[next];

        float cross = CrossProduct(p0, p1, p2);
        if (cross < 0)
            return false;

        for (int i = 0; i < indices.Count; i++)
        {
            int idx = indices[i];
            if (idx == prev || idx == curr || idx == next)
                continue;

            if (PointInTriangle(points[idx], p0, p1, p2))
                return false;
        }

        return true;
    }

    private static float CrossProduct(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = CrossProduct(p, a, b);
        float d2 = CrossProduct(p, b, c);
        float d3 = CrossProduct(p, c, a);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    private static List<Vector2> GenerateStroke(List<Vector2> points, float thickness)
    {
        if (points.Count < 2)
            return new List<Vector2>();

        var triangles = new List<Vector2>();
        float halfThickness = thickness / 2;

        // Detect if the path is closed (first point == last point).
        bool isClosed = points.Count > 2 && Vector2.Distance(points[0], points[^1]) < 0.001f;

        // Remove the duplicated last point to simplify closed-path logic.
        if (isClosed && points.Count > 2)
        {
            points = new List<Vector2>(points);
            points.RemoveAt(points.Count - 1);
        }

        // Generate stroke quads with miter joins.
        int segmentCount = isClosed ? points.Count : points.Count - 1;

        for (int i = 0; i < segmentCount; i++)
        {
            var p1 = points[i];
            var p2 = points[(i + 1) % points.Count];

            Vector2 dir = Vector2.Normalize(p2 - p1);
            Vector2 normal = new Vector2(-dir.Y, dir.X);

            var v1 = p1 + normal * halfThickness;  // Top left
            var v2 = p1 - normal * halfThickness;  // Bottom left
            var v3 = p2 - normal * halfThickness;  // Bottom right
            var v4 = p2 + normal * halfThickness;  // Top right

            triangles.Add(v1);
            triangles.Add(v2);
            triangles.Add(v3);

            triangles.Add(v1);
            triangles.Add(v3);
            triangles.Add(v4);

            // Add miter join at each vertex (except the last for open paths).
            bool shouldAddJoin = isClosed || i < segmentCount - 1;

            if (shouldAddJoin)
            {
                // Get the next point (wrapping around for closed paths).
                var p3 = points[(i + 2) % points.Count];

                Vector2 nextDir = Vector2.Normalize(p3 - p2);
                Vector2 nextNormal = new Vector2(-nextDir.Y, nextDir.X);

                // Calculate miter join vertices for both sides.
                var miterTop = CalculateMiterJoin(p2, normal, nextNormal, halfThickness);
                var miterBottom = CalculateMiterJoin(p2, -normal, -nextNormal, halfThickness);

                var v5 = p2 + nextNormal * halfThickness;  // Top of next segment
                var v6 = p2 - nextNormal * halfThickness;  // Bottom of next segment

                // Triangle 1: v4 (top current) -> miterTop -> miterBottom
                triangles.Add(v4);
                triangles.Add(miterTop);
                triangles.Add(miterBottom);

                // Triangle 2: v4 (top current) -> miterBottom -> v3 (bottom current)
                triangles.Add(v4);
                triangles.Add(miterBottom);
                triangles.Add(v3);

                // Triangle 3: miterTop -> v5 (top next) -> v6 (bottom next)
                triangles.Add(miterTop);
                triangles.Add(v5);
                triangles.Add(v6);

                // Triangle 4: miterTop -> v6 (bottom next) -> miterBottom
                triangles.Add(miterTop);
                triangles.Add(v6);
                triangles.Add(miterBottom);
            }
        }

        return triangles;
    }

    /// <summary>
    /// Calculates the miter join point between two segments.
    /// Falls back to a bevel join when the miter would exceed the limit.
    /// </summary>
    private static Vector2 CalculateMiterJoin(Vector2 point, Vector2 normal1, Vector2 normal2, float halfThickness)
    {
        // Bisector direction between the two normals.
        Vector2 miterDir = Vector2.Normalize(normal1 + normal2);

        // Nearly anti-parallel normals (straight line) — no miter needed.
        float dot = Vector2.Dot(normal1, normal2);
        if (dot < -0.99f)
            return point + normal1 * halfThickness;

        float miterLength = halfThickness / MathF.Max(0.001f, Vector2.Dot(miterDir, normal1));

        // Clamp miter length to avoid spikes at sharp corners.
        const float miterLimit = 10f;
        if (MathF.Abs(miterLength) > halfThickness * miterLimit)
            return point + normal1 * halfThickness;

        return point + miterDir * miterLength;
    }
}
