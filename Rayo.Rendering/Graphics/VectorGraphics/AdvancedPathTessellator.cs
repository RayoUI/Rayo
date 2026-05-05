using System.Numerics;

namespace Rayo.Rendering.Graphics.VectorGraphics;

/// <summary>
/// Advanced tessellator with robust algorithms similar to Skia and MAUI Graphics.
/// Implements adaptive subdivision and high-precision curve approximation.
/// </summary>
public class AdvancedPathTessellator
{
    /// <summary>
    /// Default tolerance in pixels for curve approximation.
    /// Similar to Skia: ~0.25 pixels of maximum allowed error.
    /// </summary>
    private const float DefaultTolerance = 0.25f;

    /// <summary>
    /// Maximum recursion depth in adaptive subdivision.
    /// Prevents infinite recursion in pathological curves.
    /// </summary>
    private const int MaxRecursionDepth = 16;

    /// <summary>
    /// Adaptive tessellation of a complete vector path.
    /// </summary>
    public static List<Vector2> TesselatePath(VectorPath path, int baseSegments = 32)
    {
        var points = FlattenPathAdvanced(path, baseSegments);
        return points;
    }

    /// <summary>
    /// Stroke tessellation (outline) with robust handling of joins and caps.
    /// </summary>
    public static List<Vector2> TessellateStrokeAdvanced(VectorPath path, float thickness, int baseSegments = 32)
    {
        var points = FlattenPathAdvanced(path, baseSegments);
        return GenerateStrokeAdvanced(points, thickness);
    }

    /// <summary>
    /// Flattens a vector path with high-precision adaptive tessellation.
    /// </summary>
    private static List<Vector2> FlattenPathAdvanced(VectorPath path, int baseSegments)
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
                    // Adaptive subdivision for quadratic B�zier
                    var quadPoints = TessellateQuadraticBezierAdaptive(
                        currentPoint, 
                        cmd.ControlPoint1, 
                        cmd.Point, 
                        DefaultTolerance,
                        0
                    );
                    points.AddRange(quadPoints);
                    currentPoint = cmd.Point;
                    break;

                case PathCommandType.CubicBezierTo:
                    // Adaptive subdivision for cubic B�zier
                    var cubicPoints = TessellateCubicBezierAdaptive(
                        currentPoint,
                        cmd.ControlPoint1,
                        cmd.ControlPoint2,
                        cmd.Point,
                        DefaultTolerance,
                        0
                    );
                    points.AddRange(cubicPoints);
                    currentPoint = cmd.Point;
                    break;

                case PathCommandType.ArcTo:
                    // High-precision arc tessellation
                    var arcPoints = TessellateArcAdvanced(
                        currentPoint,
                        cmd.Point,
                        cmd.Radius,
                        cmd.StartAngle,
                        cmd.SweepAngle,
                        DefaultTolerance
                    );
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

    /// <summary>
    /// Adaptive tessellation of quadratic B�zier using subdivision.
    /// Similar to Skia's algorithm.
    /// </summary>
    private static List<Vector2> TessellateQuadraticBezierAdaptive(
        Vector2 p0, Vector2 p1, Vector2 p2,
        float tolerance, int depth)
    {
        var points = new List<Vector2>();

        // Calculate the deviation of the parabola (distance of the control point to the straight line)
        float deviation = CalculateQuadraticDeviation(p0, p1, p2);

        // If the deviation is small, use direct approximation
        if (deviation < tolerance || depth >= MaxRecursionDepth)
        {
            points.Add(p2);
            return points;
        }

        // Divide the curve into two halves and process recursively
        Vector2 p01 = Vector2.Lerp(p0, p1, 0.5f);
        Vector2 p12 = Vector2.Lerp(p1, p2, 0.5f);
        Vector2 pMid = Vector2.Lerp(p01, p12, 0.5f);

        // First half
        points.AddRange(TessellateQuadraticBezierAdaptive(p0, p01, pMid, tolerance, depth + 1));

        // Second half
        points.AddRange(TessellateQuadraticBezierAdaptive(pMid, p12, p2, tolerance, depth + 1));

        return points;
    }

    /// <summary>
    /// Adaptive tessellation of cubic B�zier using subdivision.
    /// Similar to Skia's algorithm.
    /// </summary>
    private static List<Vector2> TessellateCubicBezierAdaptive(
        Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3,
        float tolerance, int depth)
    {
        var points = new List<Vector2>();

        // Calculate the maximum deviation of the control points
        float deviation = CalculateCubicDeviation(p0, p1, p2, p3);

        // If the deviation is small, use direct approximation
        if (deviation < tolerance || depth >= MaxRecursionDepth)
        {
            points.Add(p3);
            return points;
        }

        // Divide the cubic B�zier curve into two halves
        Vector2 p01 = Vector2.Lerp(p0, p1, 0.5f);
        Vector2 p12 = Vector2.Lerp(p1, p2, 0.5f);
        Vector2 p23 = Vector2.Lerp(p2, p3, 0.5f);

        Vector2 p012 = Vector2.Lerp(p01, p12, 0.5f);
        Vector2 p123 = Vector2.Lerp(p12, p23, 0.5f);

        Vector2 pMid = Vector2.Lerp(p012, p123, 0.5f);

        // First half
        points.AddRange(TessellateCubicBezierAdaptive(p0, p01, p012, pMid, tolerance, depth + 1));

        // Second half
        points.AddRange(TessellateCubicBezierAdaptive(pMid, p123, p23, p3, tolerance, depth + 1));

        return points;
    }

    /// <summary>
    /// Adaptive tessellation of circular arcs.
    /// Uses the line approximation algorithm.
    /// </summary>
    private static List<Vector2> TessellateArcAdvanced(
        Vector2 start, Vector2 end, float radius,
        float startAngle, float sweepAngle,
        float tolerance)
    {
        var points = new List<Vector2>();

        // Calculate the number of segments based on tolerance
        // Formula derived from chord approximation:
        // error = radius * (1 - cos(angle/2))
        // tolerance = radius * (1 - cos(angle/segments/2))
        // angle/segments = 2 * acos(1 - tolerance/radius)

        float halfAnglePerSegment = MathF.Acos(
            MathF.Max(-1.0f, MathF.Min(1.0f, 1.0f - tolerance / Math.Max(radius, tolerance)))
        );

        int segments = Math.Max(1, (int)MathF.Ceiling(MathF.Abs(sweepAngle) / halfAnglePerSegment));

        Vector2 center = CalculateArcCenter(start, end, radius, sweepAngle > 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = startAngle + sweepAngle * i / segments;
            float x = center.X + MathF.Cos(angle) * radius;
            float y = center.Y + MathF.Sin(angle) * radius;
            points.Add(new Vector2(x, y));
        }

        return points;
    }

    /// <summary>
    /// Calculates the maximum deviation of a quadratic B�zier curve from its straight line.
    /// </summary>
    private static float CalculateQuadraticDeviation(Vector2 p0, Vector2 p1, Vector2 p2)
    {
        // The maximum deviation of a quadratic B�zier is 1/4 of the distance
        // from the control point to the straight line connecting the endpoints

        // Distance from point p1 to the line p0-p2
        Vector2 lineDir = p2 - p0;
        float lineLengthSq = Vector2.Dot(lineDir, lineDir);

        if (lineLengthSq < 0.001f)
            return Vector2.Distance(p1, p0);

        float t = Vector2.Dot(p1 - p0, lineDir) / lineLengthSq;
        t = MathF.Max(0, MathF.Min(1, t));

        Vector2 closest = p0 + t * lineDir;
        return Vector2.Distance(p1, closest) * 0.25f;  // Quadratic factor
    }

    /// <summary>
    /// Calculates the maximum deviation of a cubic B�zier curve from its straight line.
    /// </summary>
    private static float CalculateCubicDeviation(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        // For cubic B�zier, take the maximum deviation of both control points

        // Distance from point p1 to the line p0-p3
        Vector2 lineDir = p3 - p0;
        float lineLengthSq = Vector2.Dot(lineDir, lineDir);

        if (lineLengthSq < 0.001f)
            return Math.Max(Vector2.Distance(p1, p0), Vector2.Distance(p2, p0));

        float t1 = Vector2.Dot(p1 - p0, lineDir) / lineLengthSq;
        t1 = MathF.Max(0, MathF.Min(1, t1));
        Vector2 closest1 = p0 + t1 * lineDir;
        float deviation1 = Vector2.Distance(p1, closest1);

        float t2 = Vector2.Dot(p2 - p0, lineDir) / lineLengthSq;
        t2 = MathF.Max(0, MathF.Min(1, t2));
        Vector2 closest2 = p0 + t2 * lineDir;
        float deviation2 = Vector2.Distance(p2, closest2);

        return Math.Max(deviation1, deviation2) * 0.33f;  // Cubic factor
    }

    /// <summary>
    /// Calculates the center of a circular arc.
    /// </summary>
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

    /// <summary>
    /// Generates stroke (outline) with advanced miter joins.
    /// </summary>
    private static List<Vector2> GenerateStrokeAdvanced(List<Vector2> points, float thickness)
    {
        if (points.Count < 2)
            return new List<Vector2>();

        var triangles = new List<Vector2>();
        float halfThickness = thickness / 2;

        bool isClosed = points.Count > 2 && Vector2.Distance(points[0], points[^1]) < 0.001f;

        if (isClosed && points.Count > 2)
        {
            points = new List<Vector2>(points);
            points.RemoveAt(points.Count - 1);
        }

        int segmentCount = isClosed ? points.Count : points.Count - 1;

        for (int i = 0; i < segmentCount; i++)
        {
            var p1 = points[i];
            var p2 = points[(i + 1) % points.Count];

            Vector2 dir = Vector2.Normalize(p2 - p1);
            Vector2 normal = new Vector2(-dir.Y, dir.X);

            var v1 = p1 + normal * halfThickness;
            var v2 = p1 - normal * halfThickness;
            var v3 = p2 - normal * halfThickness;
            var v4 = p2 + normal * halfThickness;

            triangles.Add(v1);
            triangles.Add(v2);
            triangles.Add(v3);

            triangles.Add(v1);
            triangles.Add(v3);
            triangles.Add(v4);

            bool shouldAddJoin = isClosed || i < segmentCount - 1;

            if (shouldAddJoin)
            {
                var p3 = points[(i + 2) % points.Count];

                Vector2 nextDir = Vector2.Normalize(p3 - p2);
                Vector2 nextNormal = new Vector2(-nextDir.Y, nextDir.X);

                var miterTop = CalculateMiterJoinAdvanced(p2, normal, nextNormal, halfThickness);
                var miterBottom = CalculateMiterJoinAdvanced(p2, -normal, -nextNormal, halfThickness);

                var v5 = p2 + nextNormal * halfThickness;
                var v6 = p2 - nextNormal * halfThickness;

                triangles.Add(v4);
                triangles.Add(miterTop);
                triangles.Add(miterBottom);

                triangles.Add(v4);
                triangles.Add(miterBottom);
                triangles.Add(v3);

                triangles.Add(miterTop);
                triangles.Add(v5);
                triangles.Add(v6);

                triangles.Add(miterTop);
                triangles.Add(v6);
                triangles.Add(miterBottom);
            }
        }

        return triangles;
    }

    /// <summary>
    /// Calculates a robust miter join with appropriate limits.
    /// </summary>
    private static Vector2 CalculateMiterJoinAdvanced(Vector2 point, Vector2 normal1, Vector2 normal2, float halfThickness)
    {
        Vector2 miterDir = Vector2.Normalize(normal1 + normal2);

        float dot = Vector2.Dot(normal1, normal2);
        if (dot < -0.99f)
            return point + normal1 * halfThickness;

        float dotProduct = Vector2.Dot(miterDir, normal1);
        if (MathF.Abs(dotProduct) < 0.001f)
            return point + normal1 * halfThickness;

        float miterLength = halfThickness / dotProduct;

        const float miterLimit = 10f;
        if (MathF.Abs(miterLength) > halfThickness * miterLimit)
            return point + normal1 * halfThickness;

        return point + miterDir * miterLength;
    }
}
