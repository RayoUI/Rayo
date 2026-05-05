namespace Rayo.Core;

using System;
using Rayo.Rendering;
using Rayo.Rendering.Graphics.VectorGraphics;
using CornerRadius = Rayo.CornerRadius;

/// <summary>
/// Represents a shadow effect that can be applied to UI elements.
/// </summary>
public readonly struct Shadow : IEquatable<Shadow>
{
    /// <summary>
    /// No shadow.
    /// </summary>
    public static readonly Shadow None = new(Color.Transparent, 0, 0, 0);

    /// <summary>
    /// Small subtle shadow.
    /// </summary>
    public static readonly Shadow Small = new(new Color(0, 0, 0, 40), 1, 1, 2);

    /// <summary>
    /// Medium shadow for cards and elevated elements.
    /// </summary>
    public static readonly Shadow Medium = new(new Color(0, 0, 0, 50), 2, 2, 4);

    /// <summary>
    /// Large shadow for modals and floating elements.
    /// </summary>
    public static readonly Shadow Large = new(new Color(0, 0, 0, 60), 4, 4, 8);

    /// <summary>
    /// Extra large shadow for dropdowns and popovers.
    /// </summary>
    public static readonly Shadow XLarge = new(new Color(0, 0, 0, 70), 6, 6, 12);

    /// <summary>
    /// Shadow color with alpha for transparency.
    /// </summary>
    public Color Color { get; }

    /// <summary>
    /// Horizontal offset of the shadow.
    /// </summary>
    public float OffsetX { get; }

    /// <summary>
    /// Vertical offset of the shadow.
    /// </summary>
    public float OffsetY { get; }

    /// <summary>
    /// Blur radius of the shadow. 0 = sharp edge, higher = more blur.
    /// </summary>
    public float BlurRadius { get; }

    /// <summary>
    /// Spread radius - expands or contracts the shadow.
    /// Positive values expand, negative values contract.
    /// </summary>
    public float SpreadRadius { get; }

    /// <summary>
    /// Whether the shadow is visible (has non-transparent color and some offset or blur).
    /// </summary>
    public bool IsVisible => Color.A > 0 && (OffsetX != 0 || OffsetY != 0 || BlurRadius > 0 || SpreadRadius != 0);

    /// <summary>
    /// Creates a new shadow with specified parameters.
    /// </summary>
    public Shadow(Color color, float offsetX, float offsetY, float blurRadius = 0, float spreadRadius = 0)
    {
        Color = color;
        OffsetX = offsetX;
        OffsetY = offsetY;
        BlurRadius = blurRadius;
        SpreadRadius = spreadRadius;
    }

    /// <summary>
    /// Creates a shadow with equal X and Y offset.
    /// </summary>
    public Shadow(Color color, float offset, float blurRadius = 0)
        : this(color, offset, offset, blurRadius, 0)
    {
    }

    /// <summary>
    /// Creates a copy of this shadow with a different color.
    /// </summary>
    public Shadow WithColor(Color color) => new(color, OffsetX, OffsetY, BlurRadius, SpreadRadius);

    /// <summary>
    /// Creates a copy of this shadow with different offsets.
    /// </summary>
    public Shadow WithOffset(float offsetX, float offsetY) => new(Color, offsetX, offsetY, BlurRadius, SpreadRadius);

    /// <summary>
    /// Creates a copy of this shadow with equal X and Y offset.
    /// </summary>
    public Shadow WithOffset(float offset) => new(Color, offset, offset, BlurRadius, SpreadRadius);

    /// <summary>
    /// Creates a copy of this shadow with a different blur radius.
    /// </summary>
    public Shadow WithBlur(float blurRadius) => new(Color, OffsetX, OffsetY, blurRadius, SpreadRadius);

    /// <summary>
    /// Creates a copy of this shadow with a different spread radius.
    /// </summary>
    public Shadow WithSpread(float spreadRadius) => new(Color, OffsetX, OffsetY, BlurRadius, spreadRadius);

    /// <summary>
    /// Creates a copy of this shadow with a different opacity (0-255 or 0-1).
    /// </summary>
    public Shadow WithOpacity(float opacity)
    {
        byte alpha = opacity <= 1 ? (byte)(opacity * 255) : (byte)opacity;
        return new(new Color(Color.R, Color.G, Color.B, alpha), OffsetX, OffsetY, BlurRadius, SpreadRadius);
    }

    /// <summary>
    /// Renders this shadow using the provided renderer around a rounded rectangle.
    /// </summary>
    public void Render(IRenderer renderer, float x, float y, float width, float height, CornerRadius cornerRadius)
    {
        if (!IsVisible)
        {
            return;
        }

        float blur = Math.Max(0, BlurRadius);
        float spread = SpreadRadius;
        float expansion = spread + blur;

        float shadowX = x + OffsetX - expansion;
        float shadowY = y + OffsetY - expansion;
        float shadowWidth = width + expansion * 2;
        float shadowHeight = height + expansion * 2;

        float topLeft = Math.Max(0, cornerRadius.TopLeft + expansion);
        float topRight = Math.Max(0, cornerRadius.TopRight + expansion);
        float bottomRight = Math.Max(0, cornerRadius.BottomRight + expansion);
        float bottomLeft = Math.Max(0, cornerRadius.BottomLeft + expansion);

        var path = VectorPath.RoundedRectangle(
            shadowX,
            shadowY,
            shadowWidth,
            shadowHeight,
            topLeft,
            topRight,
            bottomRight,
            bottomLeft);

        var color = Color;
        if (blur > 0)
        {
            float falloff = Math.Clamp(1f - (blur / (blur + 16f)), 0.25f, 1f);
            color = color.WithAlpha(color.A * falloff);
        }

        renderer.DrawPath(path, color);
    }

    public bool Equals(Shadow other)
    {
        return Color.Equals(other.Color) &&
               OffsetX == other.OffsetX &&
               OffsetY == other.OffsetY &&
               BlurRadius == other.BlurRadius &&
               SpreadRadius == other.SpreadRadius;
    }

    public override bool Equals(object? obj) => obj is Shadow other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Color, OffsetX, OffsetY, BlurRadius, SpreadRadius);

    public static bool operator ==(Shadow left, Shadow right) => left.Equals(right);

    public static bool operator !=(Shadow left, Shadow right) => !left.Equals(right);

    public override string ToString() =>
        $"Shadow({Color}, Offset=({OffsetX}, {OffsetY}), Blur={BlurRadius}, Spread={SpreadRadius})";
}
