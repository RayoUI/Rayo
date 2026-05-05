using System.Numerics;

namespace Rayo.Rendering.Brushes;

/// <summary>
/// Base class for all brush types that define how shapes are filled.
/// Similar to System.Windows.Media.Brush in WPF/MAUI.
/// </summary>
public abstract class Brush : IEquatable<Brush>
{
    /// <summary>
    /// Opacity of the brush (0.0 = fully transparent, 1.0 = fully opaque).
    /// </summary>
    public float Opacity { get; set; } = 1.0f;

    /// <summary>
    /// Gets the color at a specific normalized position (0-1) within the brush.
    /// Used for sampling gradient colors at specific points.
    /// </summary>
    /// <param name="normalizedX">X position (0-1) relative to the brush bounds</param>
    /// <param name="normalizedY">Y position (0-1) relative to the brush bounds</param>
    /// <returns>The color at the specified position</returns>
    public abstract Color GetColorAt(float normalizedX, float normalizedY);

    /// <summary>
    /// Gets the primary/average color of the brush.
    /// Useful for fallback rendering or hit-testing.
    /// </summary>
    public abstract Color PrimaryColor { get; }

    /// <summary>
    /// Indicates whether this brush requires gradient rendering support.
    /// </summary>
    public virtual bool IsGradient => false;

    /// <summary>
    /// Creates a brush with modified opacity.
    /// </summary>
    public abstract Brush WithOpacity(float opacity);

    // Implicit conversion from Color to SolidColorBrush for convenience
    public abstract bool Equals(Brush? other);

    public override bool Equals(object? obj)
        => obj is Brush other && Equals(other);

    public abstract override int GetHashCode();

    public static bool operator ==(Brush? left, Brush? right)
        => Equals(left, right);

    public static bool operator !=(Brush? left, Brush? right)
        => !Equals(left, right);

    public static implicit operator Brush(Color color)
        => new SolidColorBrush(color);
}

/// <summary>
/// Defines a gradient stop with a color and offset position.
/// </summary>
public readonly struct GradientStop : IEquatable<GradientStop>
{
    /// <summary>
    /// The color at this gradient stop.
    /// </summary>
    public Color Color { get; }

    /// <summary>
    /// The offset position (0.0 to 1.0) where this color appears in the gradient.
    /// </summary>
    public float Offset { get; }

    public GradientStop(Color color, float offset)
    {
        Color = color;
        Offset = Math.Clamp(offset, 0f, 1f);
    }

    public static GradientStop At(float offset, Color color) => new(color, offset);

    public bool Equals(GradientStop other)
        => Color == other.Color && MathF.Abs(Offset - other.Offset) < 0.0001f;

    public override bool Equals(object? obj)
        => obj is GradientStop other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Color, Offset);

    public static bool operator ==(GradientStop left, GradientStop right) => left.Equals(right);
    public static bool operator !=(GradientStop left, GradientStop right) => !left.Equals(right);
}

/// <summary>
/// Specifies how a gradient extends beyond its defined area.
/// </summary>
public enum GradientSpreadMethod
{
    /// <summary>
    /// The gradient stops at the edge colors.
    /// </summary>
    Pad,

    /// <summary>
    /// The gradient repeats in the original direction.
    /// </summary>
    Repeat,

    /// <summary>
    /// The gradient repeats in alternating directions (mirrored).
    /// </summary>
    Reflect
}
