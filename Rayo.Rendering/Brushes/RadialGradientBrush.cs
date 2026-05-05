using System.Numerics;

namespace Rayo.Rendering.Brushes;

/// <summary>
/// A brush that paints an area with a radial gradient.
/// The gradient radiates outward from a center point.
/// </summary>
public class RadialGradientBrush : Brush
{
    /// <summary>
    /// The center point of the gradient (normalized 0-1 coordinates).
    /// Default is (0.5, 0.5) - center.
    /// </summary>
    public Vector2 Center { get; set; } = new(0.5f, 0.5f);

    /// <summary>
    /// The origin point of the gradient (normalized 0-1 coordinates).
    /// This allows for off-center gradient origins (spotlight effect).
    /// Default is same as Center.
    /// </summary>
    public Vector2 Origin { get; set; } = new(0.5f, 0.5f);

    /// <summary>
    /// The horizontal radius of the gradient (normalized 0-1).
    /// Default is 0.5 (half the width).
    /// </summary>
    public float RadiusX { get; set; } = 0.5f;

    /// <summary>
    /// The vertical radius of the gradient (normalized 0-1).
    /// Default is 0.5 (half the height).
    /// </summary>
    public float RadiusY { get; set; } = 0.5f;

    /// <summary>
    /// The collection of gradient stops that define the gradient colors.
    /// </summary>
    public List<GradientStop> GradientStops { get; set; } = new();

    /// <summary>
    /// Specifies how the gradient extends beyond its bounds.
    /// </summary>
    public GradientSpreadMethod SpreadMethod { get; set; } = GradientSpreadMethod.Pad;

    public override bool IsGradient => true;

    public RadialGradientBrush() { }

    public RadialGradientBrush(params GradientStop[] stops)
    {
        GradientStops = stops.ToList();
    }

    public RadialGradientBrush(Color centerColor, Color edgeColor)
    {
        GradientStops = new List<GradientStop>
        {
            new(centerColor, 0f),
            new(edgeColor, 1f)
        };
    }

    public override Color PrimaryColor
    {
        get
        {
            if (GradientStops.Count == 0)
                return Color.Transparent;

            // Return the center color
            return GetColorAt(Center.X, Center.Y);
        }
    }

    public override Color GetColorAt(float normalizedX, float normalizedY)
    {
        if (GradientStops.Count == 0)
            return Color.Transparent;

        if (GradientStops.Count == 1)
            return GradientStops[0].Color.WithAlpha(GradientStops[0].Color.A * Opacity);

        // Calculate distance from gradient origin (or center if not specified)
        Vector2 origin = Origin;

        // Calculate normalized distance considering ellipse radii
        float dx = (normalizedX - origin.X) / RadiusX;
        float dy = (normalizedY - origin.Y) / RadiusY;
        float t = MathF.Sqrt(dx * dx + dy * dy);

        // Apply spread method
        t = ApplySpreadMethod(t);

        // Interpolate between gradient stops
        var color = InterpolateColor(t);
        return color.WithAlpha(color.A * Opacity);
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
        return new RadialGradientBrush
        {
            Center = Center,
            Origin = Origin,
            RadiusX = RadiusX,
            RadiusY = RadiusY,
            GradientStops = new List<GradientStop>(GradientStops),
            SpreadMethod = SpreadMethod,
            Opacity = opacity
        };
    }

    public override bool Equals(Brush? other)
    {
        if (other is not RadialGradientBrush b) return false;
        return Center == b.Center &&
               Origin == b.Origin &&
               MathF.Abs(RadiusX - b.RadiusX) < 0.0001f &&
               MathF.Abs(RadiusY - b.RadiusY) < 0.0001f &&
               SpreadMethod == b.SpreadMethod &&
               MathF.Abs(Opacity - b.Opacity) < 0.0001f &&
               GradientStops.SequenceEqual(b.GradientStops);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Center.GetHashCode();
            hash = hash * 31 + Origin.GetHashCode();
            hash = hash * 31 + RadiusX.GetHashCode();
            hash = hash * 31 + RadiusY.GetHashCode();
            hash = hash * 31 + SpreadMethod.GetHashCode();
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
    public static RadialGradientBrush Circular(Color center, Color edge) =>
        new(center, edge);

    public static RadialGradientBrush Spotlight(Color center, Color edge, Vector2 spotlightOrigin) =>
        new(center, edge) { Origin = spotlightOrigin };

    public static RadialGradientBrush Elliptical(Color center, Color edge, float radiusX, float radiusY) =>
        new(center, edge) { RadiusX = radiusX, RadiusY = radiusY };
}
