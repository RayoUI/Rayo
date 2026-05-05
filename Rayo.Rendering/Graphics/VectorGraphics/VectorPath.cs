using System.Numerics;

namespace Rayo.Rendering.Graphics.VectorGraphics;

/// <summary>
/// Represents a command in a vector path.
/// </summary>
public enum PathCommandType
{
    MoveTo,
    LineTo,
    QuadraticBezierTo,
    CubicBezierTo,
    ArcTo,
    Close
}

/// <summary>
/// Represents an individual command in a path.
/// </summary>
public readonly struct PathCommand
{
    public PathCommandType Type { get; init; }
    public Vector2 Point { get; init; }
 public Vector2 ControlPoint1 { get; init; }
    public Vector2 ControlPoint2 { get; init; }
    public float Radius { get; init; }
    public float StartAngle { get; init; }
    public float SweepAngle { get; init; }
    public bool LargeArc { get; init; }
    public bool Clockwise { get; init; }

    public static PathCommand MoveTo(Vector2 point) => new()
{
        Type = PathCommandType.MoveTo,
        Point = point
    };

    public static PathCommand LineTo(Vector2 point) => new()
    {
        Type = PathCommandType.LineTo,
    Point = point
    };

    public static PathCommand QuadraticBezierTo(Vector2 controlPoint, Vector2 endPoint) => new()
  {
        Type = PathCommandType.QuadraticBezierTo,
        ControlPoint1 = controlPoint,
  Point = endPoint
    };

    public static PathCommand CubicBezierTo(Vector2 controlPoint1, Vector2 controlPoint2, Vector2 endPoint) => new()
    {
        Type = PathCommandType.CubicBezierTo,
     ControlPoint1 = controlPoint1,
ControlPoint2 = controlPoint2,
        Point = endPoint
    };

    public static PathCommand ArcTo(Vector2 endPoint, float radius, float startAngle, float sweepAngle, bool largeArc = false, bool clockwise = true) => new()
    {
        Type = PathCommandType.ArcTo,
      Point = endPoint,
     Radius = radius,
        StartAngle = startAngle,
      SweepAngle = sweepAngle,
     LargeArc = largeArc,
        Clockwise = clockwise
    };

    public static PathCommand Close() => new()
  {
        Type = PathCommandType.Close
    };
}

/// <summary>
/// Representa un path vectorial completo
/// </summary>
public class VectorPath
{
    private readonly List<PathCommand> _commands = new();
    private Vector2 _currentPoint;
    private Vector2 _startPoint;

    public IReadOnlyList<PathCommand> Commands => _commands;

    public VectorPath MoveTo(float x, float y)
    {
        var point = new Vector2(x, y);
        _currentPoint = point;
    _startPoint = point;
        _commands.Add(PathCommand.MoveTo(point));
        return this;
    }

    public VectorPath LineTo(float x, float y)
    {
        var point = new Vector2(x, y);
        _commands.Add(PathCommand.LineTo(point));
        _currentPoint = point;
        return this;
    }

    public VectorPath QuadraticBezierTo(float controlX, float controlY, float endX, float endY)
{
      var controlPoint = new Vector2(controlX, controlY);
   var endPoint = new Vector2(endX, endY);
    _commands.Add(PathCommand.QuadraticBezierTo(controlPoint, endPoint));
        _currentPoint = endPoint;
        return this;
    }

    public VectorPath CubicBezierTo(float cp1X, float cp1Y, float cp2X, float cp2Y, float endX, float endY)
    {
        var cp1 = new Vector2(cp1X, cp1Y);
        var cp2 = new Vector2(cp2X, cp2Y);
        var endPoint = new Vector2(endX, endY);
    _commands.Add(PathCommand.CubicBezierTo(cp1, cp2, endPoint));
        _currentPoint = endPoint;
        return this;
    }

    public VectorPath ArcTo(float endX, float endY, float radius, float startAngle, float sweepAngle, bool largeArc = false, bool clockwise = true)
    {
        var endPoint = new Vector2(endX, endY);
        _commands.Add(PathCommand.ArcTo(endPoint, radius, startAngle, sweepAngle, largeArc, clockwise));
    _currentPoint = endPoint;
     return this;
    }

public VectorPath Close()
    {
      _commands.Add(PathCommand.Close());
        _currentPoint = _startPoint;
        return this;
    }

    public static VectorPath Rectangle(float x, float y, float width, float height)
    {
        return new VectorPath()
            .MoveTo(x, y)
            .LineTo(x + width, y)
       .LineTo(x + width, y + height)
            .LineTo(x, y + height)
            .Close();
    }

    public static VectorPath RoundedRectangle(float x, float y, float width, float height, float radius)
    {
        return RoundedRectangle(x, y, width, height, radius, radius, radius, radius);
    }

    public static VectorPath RoundedRectangle(float x, float y, float width, float height, float topLeft, float topRight, float bottomRight, float bottomLeft)
    {
        var path = new VectorPath();
        
        // Clamp radii to ensure they don't overlap
        // This is a simplified clamping, ideally we should scale them proportionally if they exceed width/height
        float maxRadius = Math.Min(width / 2, height / 2);
        
        float rTL = Math.Min(topLeft, maxRadius);
        float rTR = Math.Min(topRight, maxRadius);
        float rBR = Math.Min(bottomRight, maxRadius);
        float rBL = Math.Min(bottomLeft, maxRadius);

        // Start from top-left corner (after the curve)
        path.MoveTo(x + rTL, y);
        
        // Top edge
        path.LineTo(x + width - rTR, y);
        
        // Top-right corner
        if (rTR > 0)
            path.ArcTo(x + width, y + rTR, rTR, -MathF.PI / 2, MathF.PI / 2);
        else
            path.LineTo(x + width, y);
            
        // Right edge
        path.LineTo(x + width, y + height - rBR);
        
        // Bottom-right corner
        if (rBR > 0)
            path.ArcTo(x + width - rBR, y + height, rBR, 0, MathF.PI / 2);
        else
            path.LineTo(x + width, y + height);
            
        // Bottom edge
        path.LineTo(x + rBL, y + height);
        
        // Bottom-left corner
        if (rBL > 0)
            path.ArcTo(x, y + height - rBL, rBL, MathF.PI / 2, MathF.PI / 2);
        else
            path.LineTo(x, y + height);
            
        // Left edge
        path.LineTo(x, y + rTL);
        
        // Top-left corner
        if (rTL > 0)
            path.ArcTo(x + rTL, y, rTL, MathF.PI, MathF.PI / 2);
        else
            path.LineTo(x, y);
        
        return path.Close();
    }

    public static VectorPath Circle(float cx, float cy, float radius)
    {
        return Ellipse(cx, cy, radius, radius);
    }

    public static VectorPath Ellipse(float cx, float cy, float radiusX, float radiusY)
    {
        var path = new VectorPath();
        float kappa = 0.5522847498f;
        float ox = radiusX * kappa;
    float oy = radiusY * kappa;

        path.MoveTo(cx - radiusX, cy);
   path.CubicBezierTo(cx - radiusX, cy - oy, cx - ox, cy - radiusY, cx, cy - radiusY);
        path.CubicBezierTo(cx + ox, cy - radiusY, cx + radiusX, cy - oy, cx + radiusX, cy);
        path.CubicBezierTo(cx + radiusX, cy + oy, cx + ox, cy + radiusY, cx, cy + radiusY);
        path.CubicBezierTo(cx - ox, cy + radiusY, cx - radiusX, cy + oy, cx - radiusX, cy);
  
        return path.Close();
    }

    public static VectorPath RegularPolygon(float cx, float cy, float radius, int sides, float rotation = 0)
    {
        if (sides < 3)
            throw new ArgumentException("Polygon must have at least 3 sides", nameof(sides));

        var path = new VectorPath();
        float angleStep = MathF.PI * 2 / sides;

        for (int i = 0; i < sides; i++)
        {
   float angle = rotation + i * angleStep;
        float x = cx + MathF.Cos(angle) * radius;
            float y = cy + MathF.Sin(angle) * radius;

   if (i == 0)
          path.MoveTo(x, y);
        else
       path.LineTo(x, y);
        }

        return path.Close();
    }

    public static VectorPath Star(float cx, float cy, float outerRadius, float innerRadius, int points, float rotation = 0)
    {
        if (points < 3)
          throw new ArgumentException("Star must have at least 3 points", nameof(points));

    var path = new VectorPath();
  float angleStep = MathF.PI / points;

        for (int i = 0; i < points * 2; i++)
        {
 float angle = rotation + i * angleStep;
            float radius = (i % 2 == 0) ? outerRadius : innerRadius;
       float x = cx + MathF.Cos(angle) * radius;
       float y = cy + MathF.Sin(angle) * radius;

        if (i == 0)
        path.MoveTo(x, y);
      else
                path.LineTo(x, y);
        }

        return path.Close();
    }

    public (float minX, float minY, float maxX, float maxY) GetBounds()
    {
    if (_commands.Count == 0)
      return (0, 0, 0, 0);

        float minX = float.MaxValue;
        float minY = float.MaxValue;
  float maxX = float.MinValue;
        float maxY = float.MinValue;

        foreach (var cmd in _commands)
        {
            if (cmd.Type == PathCommandType.Close)
    continue;

       UpdateBounds(ref minX, ref minY, ref maxX, ref maxY, cmd.Point);

   if (cmd.Type == PathCommandType.QuadraticBezierTo)
         {
    UpdateBounds(ref minX, ref minY, ref maxX, ref maxY, cmd.ControlPoint1);
     }
        else if (cmd.Type == PathCommandType.CubicBezierTo)
     {
       UpdateBounds(ref minX, ref minY, ref maxX, ref maxY, cmd.ControlPoint1);
    UpdateBounds(ref minX, ref minY, ref maxX, ref maxY, cmd.ControlPoint2);
            }
        }

        return (minX, minY, maxX, maxY);
    }

    private void UpdateBounds(ref float minX, ref float minY, ref float maxX, ref float maxY, Vector2 point)
    {
        minX = Math.Min(minX, point.X);
        minY = Math.Min(minY, point.Y);
     maxX = Math.Max(maxX, point.X);
        maxY = Math.Max(maxY, point.Y);
    }
}
