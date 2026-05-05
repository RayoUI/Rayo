using System.Numerics;

namespace Rayo.Rendering.Brushes;

/// <summary>
/// A brush that paints an area with a conic (angular/sweep) gradient.
/// The gradient sweeps around a center point like a color wheel.
/// </summary>
public class ConicGradientBrush : Brush
{
    /// <summary>
    /// The center point of the gradient (normalized 0-1 coordinates).
    /// Default is (0.5, 0.5) - center.
    /// </summary>
    public Vector2 Center { get; set; } = new(0.5f, 0.5f);

    /// <summary>
    /// The starting angle in degrees (0 = right, 90 = down, 180 = left, 270 = up).
    /// Default is 0 (starting from the right).
    /// </summary>
    public float Angle { get; set; } = 0;

    /// <summary>
    /// The collection of gradient stops that define the gradient colors.
    /// Offset 0 corresponds to the start angle, offset 1 wraps back to start.
    /// </summary>
    public List<GradientStop> GradientStops { get; set; } = new();

    public override bool IsGradient => true;

    public ConicGradientBrush() { }

    public ConicGradientBrush(params GradientStop[] stops)
    {
        GradientStops = stops.ToList();
    }

    /// <summary>
    /// Creates a simple two-color conic gradient.
    /// </summary>
    public ConicGradientBrush(Color startColor, Color endColor)
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

            return GetColorAt(0.5f, 0.5f);
        }
    }

    public override Color GetColorAt(float normalizedX, float normalizedY)
    {
        if (GradientStops.Count == 0)
            return Color.Transparent;

        if (GradientStops.Count == 1)
            return GradientStops[0].Color.WithAlpha(GradientStops[0].Color.A * Opacity);

        // Calculate angle from center to point
        float dx = normalizedX - Center.X;
        float dy = normalizedY - Center.Y;

        // Get angle in degrees (0-360)
        float angle = MathF.Atan2(dy, dx) * 180f / MathF.PI;
        angle = (angle - Angle + 360) % 360; // Adjust for start angle

        // Normalize to 0-1
        float t = angle / 360f;

        // Interpolate between gradient stops
        var color = InterpolateColor(t);
        return color.WithAlpha(color.A * Opacity);
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

        // Handle wrap-around for conic gradients
        if (lower == null)
            lower = sortedStops[^1]; // Use last stop
        if (upper == null)
            upper = sortedStops[0]; // Wrap to first stop

        if (lower.Value.Offset == upper.Value.Offset)
            return lower.Value.Color;

        // Linear interpolation
        float range = upper.Value.Offset - lower.Value.Offset;
        if (range <= 0) range = 1; // Handle wrap-around

        float localT = (t - lower.Value.Offset) / range;
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
        return new ConicGradientBrush
        {
            Center = Center,
            Angle = Angle,
            GradientStops = new List<GradientStop>(GradientStops),
            Opacity = opacity
        };
    }

    public override bool Equals(Brush? other)
    {
        if (other is not ConicGradientBrush b) return false;
        return Center == b.Center &&
               MathF.Abs(Angle - b.Angle) < 0.0001f &&
               MathF.Abs(Opacity - b.Opacity) < 0.0001f &&
               GradientStops.SequenceEqual(b.GradientStops);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Center.GetHashCode();
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

    // Factory methods

    /// <summary>
    /// Creates a color wheel gradient with all hues.
    /// </summary>
    public static ConicGradientBrush ColorWheel()
    {
        return new ConicGradientBrush
        {
            GradientStops = new List<GradientStop>
            {
                new(Color.Red, 0f),
                new(Color.Yellow, 1f/6f),
                new(Color.Green, 2f/6f),
                new(Color.Cyan, 3f/6f),
                new(Color.Blue, 4f/6f),
                new(Color.Magenta, 5f/6f),
                new(Color.Red, 1f)
            }
        };
    }

    /// <summary>
    /// Creates a sweep gradient from one color to another around 360 degrees.
    /// </summary>
    public static ConicGradientBrush Sweep(Color from, Color to, float startAngle = 0)
    {
        return new ConicGradientBrush(from, to) { Angle = startAngle };
    }
}
