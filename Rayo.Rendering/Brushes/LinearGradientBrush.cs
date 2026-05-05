using System.Numerics;

namespace Rayo.Rendering.Brushes;

/// <summary>
/// A brush that paints an area with a linear gradient.
/// The gradient transitions colors along a line defined by start and end points.
/// </summary>
public class LinearGradientBrush : Brush
{
    /// <summary>
    /// The starting point of the gradient (normalized 0-1 coordinates).
    /// Default is (0, 0) - top-left corner.
    /// </summary>
    public Vector2 StartPoint { get; set; } = new(0, 0);

    /// <summary>
    /// The ending point of the gradient (normalized 0-1 coordinates).
    /// Default is (1, 1) - bottom-right corner.
    /// </summary>
    public Vector2 EndPoint { get; set; } = new(1, 1);

    /// <summary>
    /// The collection of gradient stops that define the gradient colors.
    /// </summary>
    public List<GradientStop> GradientStops { get; set; } = new();

    /// <summary>
    /// Specifies how the gradient extends beyond its bounds.
    /// </summary>
    public GradientSpreadMethod SpreadMethod { get; set; } = GradientSpreadMethod.Pad;

    /// <summary>
    /// Rotation angle in degrees (clockwise).
    /// </summary>
    public float Angle { get; set; } = 0;

    public override bool IsGradient => true;

    public LinearGradientBrush() { }

    public LinearGradientBrush(params GradientStop[] stops)
    {
        GradientStops = stops.ToList();
    }

    public LinearGradientBrush(Color startColor, Color endColor)
    {
        GradientStops = new List<GradientStop>
        {
            new(startColor, 0f),
            new(endColor, 1f)
        };
    }

    public override Color PrimaryColor
    {
        get
        {
            if (GradientStops.Count == 0)
                return Color.Transparent;

            // Return the color at 50% position
            return GetColorAt(0.5f, 0.5f);
        }
    }

    public override Color GetColorAt(float normalizedX, float normalizedY)
    {
        if (GradientStops.Count == 0)
            return Color.Transparent;

        if (GradientStops.Count == 1)
            return GradientStops[0].Color.WithAlpha(GradientStops[0].Color.A * Opacity);

        // Calculate the position along the gradient line
        float t = CalculateGradientPosition(normalizedX, normalizedY);

        // Apply spread method
        t = ApplySpreadMethod(t);

        // Interpolate between gradient stops
        var color = InterpolateColor(t);
        return color.WithAlpha(color.A * Opacity);
    }

    private float CalculateGradientPosition(float x, float y)
    {
        // Apply rotation if specified
        if (Angle != 0)
        {
            float radians = Angle * MathF.PI / 180f;
            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);

            // Rotate around center (0.5, 0.5)
            float cx = x - 0.5f;
            float cy = y - 0.5f;
            x = cx * cos - cy * sin + 0.5f;
            y = cx * sin + cy * cos + 0.5f;
        }

        // Calculate projection onto gradient line
        Vector2 start = StartPoint;
        Vector2 end = EndPoint;
        Vector2 point = new(x, y);

        Vector2 gradientDirection = end - start;
        float gradientLength = gradientDirection.Length();

        if (gradientLength < 0.0001f)
            return 0f;

        Vector2 normalizedDirection = gradientDirection / gradientLength;
        Vector2 pointVector = point - start;

        float projection = Vector2.Dot(pointVector, normalizedDirection);
        return projection / gradientLength;
    }

    private float ApplySpreadMethod(float t)
    {
        switch (SpreadMethod)
        {
            case GradientSpreadMethod.Pad:
                return Math.Clamp(t, 0f, 1f);

            case GradientSpreadMethod.Repeat:
                return t - MathF.Floor(t);

            case GradientSpreadMethod.Reflect:
                float cycle = t - MathF.Floor(t);
                int cycleNum = (int)MathF.Floor(t);
                return (cycleNum % 2 == 0) ? cycle : 1f - cycle;

            default:
                return Math.Clamp(t, 0f, 1f);
        }
    }

    private Color InterpolateColor(float t)
    {
        // Sort stops by offset
        var sortedStops = GradientStops.OrderBy(s => s.Offset).ToList();

        // Find the two stops to interpolate between
        GradientStop? lower = null;
        GradientStop? upper = null;

        foreach (var stop in sortedStops)
        {
            if (stop.Offset <= t)
                lower = stop;
            if (stop.Offset >= t && upper == null)
                upper = stop;
        }

        lower ??= sortedStops[0];
        upper ??= sortedStops[^1];

        if (lower.Value.Offset == upper.Value.Offset)
            return lower.Value.Color;

        // Linear interpolation
        float localT = (t - lower.Value.Offset) / (upper.Value.Offset - lower.Value.Offset);
        localT = Math.Clamp(localT, 0f, 1f);

        return LerpColor(lower.Value.Color, upper.Value.Color, localT);
    }

    private static Color LerpColor(Color a, Color b, float t)
    {
        return new Color(
            a.R + (b.R - a.R) * t,
            a.G + (b.G - a.G) * t,
            a.B + (b.B - a.B) * t,
            a.A + (b.A - a.A) * t
        );
    }

    public override Brush WithOpacity(float opacity)
    {
        return new LinearGradientBrush
        {
            StartPoint = StartPoint,
            EndPoint = EndPoint,
            GradientStops = new List<GradientStop>(GradientStops),
            SpreadMethod = SpreadMethod,
            Angle = Angle,
            Opacity = opacity
        };
    }

    public override bool Equals(Brush? other)
    {
        if (other is not LinearGradientBrush b) return false;
        return StartPoint == b.StartPoint &&
               EndPoint == b.EndPoint &&
               SpreadMethod == b.SpreadMethod &&
               MathF.Abs(Angle - b.Angle) < 0.0001f &&
               MathF.Abs(Opacity - b.Opacity) < 0.0001f &&
               GradientStops.SequenceEqual(b.GradientStops);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + StartPoint.GetHashCode();
            hash = hash * 31 + EndPoint.GetHashCode();
            hash = hash * 31 + SpreadMethod.GetHashCode();
            hash = hash * 31 + Angle.GetHashCode();
            hash = hash * 31 + Opacity.GetHashCode();
            foreach (var stop in GradientStops)
            {
                hash = hash * 31 + stop.Color.GetHashCode();
                hash = hash * 31 + stop.Offset.GetHashCode();
            }
            return hash;
        }
    }

    // Factory methods for common gradients
    public static LinearGradientBrush Horizontal(Color start, Color end) =>
        new(start, end) { StartPoint = new(0, 0.5f), EndPoint = new(1, 0.5f) };

    public static LinearGradientBrush Vertical(Color start, Color end) =>
        new(start, end) { StartPoint = new(0.5f, 0), EndPoint = new(0.5f, 1) };

    public static LinearGradientBrush Diagonal(Color start, Color end) =>
        new(start, end) { StartPoint = new(0, 0), EndPoint = new(1, 1) };

    public static LinearGradientBrush FromAngle(float angleDegrees, params GradientStop[] stops) =>
        new(stops) { Angle = angleDegrees, StartPoint = new(0.5f, 0), EndPoint = new(0.5f, 1) };
}
