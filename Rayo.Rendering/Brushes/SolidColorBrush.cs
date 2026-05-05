namespace Rayo.Rendering.Brushes;

/// <summary>
/// A brush that paints an area with a solid color.
/// </summary>
public class SolidColorBrush : Brush
{
    /// <summary>
    /// The solid color used by this brush.
    /// </summary>
    public Color Color { get; set; }

    public SolidColorBrush(Color color)
    {
        Color = color;
    }

    public override Color PrimaryColor => Color.WithAlpha(Color.A * Opacity);

    public override Color GetColorAt(float normalizedX, float normalizedY)
    {
        return Color.WithAlpha(Color.A * Opacity);
    }

    public override Brush WithOpacity(float opacity)
    {
        return new SolidColorBrush(Color) { Opacity = opacity };
    }

    public override bool Equals(Brush? other)
        => other is SolidColorBrush b && Color == b.Color && MathF.Abs(Opacity - b.Opacity) < 0.0001f;

    public override int GetHashCode()
        => HashCode.Combine(Color, Opacity);

    // Implicit conversion from Color
    public static implicit operator SolidColorBrush(Color color) => new(color);

    // Static factory methods for common colors
    public static SolidColorBrush White => new(Color.White);
    public static SolidColorBrush Black => new(Color.Black);
    public static SolidColorBrush Transparent => new(Color.Transparent);
    public static SolidColorBrush Red => new(Color.Red);
    public static SolidColorBrush Green => new(Color.Green);
    public static SolidColorBrush Blue => new(Color.Blue);
    public static SolidColorBrush Gray => new(Color.Gray);
}
