namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>
/// Border component - Container with configurable border and optional shadow
/// Similar to MAUI Border control
/// </summary>
public class Border : Rayo.Core.CompositeView<Border>
{
    private VisualElement? _content;

    #region Background
    [PaintProperty]
    public new Brush Background
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region BorderBrush
    [PaintProperty]
    public Brush BorderBrush
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(100, 100, 100);
    #endregion

    #region BorderThickness
    [LayoutProperty]
    public Thickness BorderThickness
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Thickness(1);
    #endregion

    #region CornerRadius
    [PaintProperty]
    public CornerRadius CornerRadius
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new CornerRadius(0);
    #endregion

    #region Padding
    [LayoutProperty]
    public new Thickness Padding
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Thickness(0);
    #endregion

    #region Shadow
    [PaintProperty]
    public Shadow? Shadow
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = null;
    #endregion

    [LayoutProperty]
    public VisualElement? Content
    {
        get => _content;
        set => this.SetProperty(ref _content, value, () =>
        {
            if (_content != null)
            {
                RemoveChild(_content);
            }
            _content = value;
            if (_content != null)
            {
                AddChild(_content);
            }
        });
    }

    public Border() { }

    public Border(VisualElement content)
    {
        Content = content;
    }

    // Returns the extra space (left, top, right, bottom) that the shadow needs outside the visual rect.
    private (float L, float T, float R, float B) ShadowExtent()
    {
        if (Shadow == null || !Shadow.IsVisible) return (0, 0, 0, 0);
        float blur = Shadow.BlurRadius;
        return (
            MathF.Max(0, -Shadow.OffsetX) + blur,
            MathF.Max(0, -Shadow.OffsetY) + blur,
            MathF.Max(0,  Shadow.OffsetX) + blur,
            MathF.Max(0,  Shadow.OffsetY) + blur
        );
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        var (sl, st, sr, sb) = ShadowExtent();

        float paddingH = Padding.Left + Padding.Right + BorderThickness.Left + BorderThickness.Right + sl + sr;
        float paddingV = Padding.Top + Padding.Bottom + BorderThickness.Top + BorderThickness.Bottom + st + sb;

        float contentWidth  = availableWidth  - paddingH;
        float contentHeight = availableHeight - paddingV;

        if (_content != null)
        {
            _content.Measure(contentWidth, contentHeight);
            DesiredWidth  = _content.DesiredWidth  + paddingH;
            DesiredHeight = _content.DesiredHeight + paddingV;
        }
        else
        {
            DesiredWidth  = Width  > 0 ? Width  : paddingH;
            DesiredHeight = Height > 0 ? Height : paddingV;
        }

        if (Width  > 0) DesiredWidth  = Width;
        if (Height > 0) DesiredHeight = Height;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        if (_content != null)
        {
            var (sl, st, sr, sb) = ShadowExtent();

            float contentX      = x + Padding.Left + BorderThickness.Left + sl;
            float contentY      = y + Padding.Top  + BorderThickness.Top  + st;
            float contentWidth  = width  - Padding.Left - Padding.Right  - BorderThickness.Left - BorderThickness.Right  - sl - sr;
            float contentHeight = height - Padding.Top  - Padding.Bottom - BorderThickness.Top  - BorderThickness.Bottom - st - sb;

            _content.Arrange(contentX, contentY, contentWidth, contentHeight);
        }
    }

    public override void Render(IRenderer renderer)
    {
        var (sl, st, sr, sb) = ShadowExtent();

        // Visual rect: the element's box excluding shadow space
        float x = ComputedX + sl;
        float y = ComputedY + st;
        float w = ComputedWidth  - sl - sr;
        float h = ComputedHeight - st - sb;
        float radius = CornerRadius.TopLeft;

        // Draw shadow around the visual rect
        if (Shadow != null && Shadow.IsVisible)
        {
            Shadow.Render(renderer, x, y, w, h, CornerRadius);
        }

        // Draw background
        var bgColor = Background.PrimaryColor;
        if (bgColor.A > 0)
        {
            if (radius > 0)
                renderer.DrawRoundedRect(x, y, w, h, radius, bgColor);
            else
                renderer.DrawRect(x, y, w, h, bgColor);
        }

        // Draw border
        if (BorderBrush.PrimaryColor.A > 0 && (BorderThickness.Left > 0 || BorderThickness.Top > 0 ||
            BorderThickness.Right > 0 || BorderThickness.Bottom > 0))
        {
            DrawBorder(renderer, x, y, w, h, radius, bgColor);
        }

        // Content is rendered automatically as a child
    }

    private void DrawBorder(IRenderer renderer, float x, float y, float w, float h, float radius, Color bgColor)
    {
        float borderWidth = BorderThickness.Left; // Simplified: using uniform border

        var borderColor = BorderBrush.PrimaryColor;
        if (radius > 0)
        {
            // Draw outer rounded rect
            renderer.DrawRoundedRect(x, y, w, h, radius, borderColor);
            // Draw inner rect with background to create border effect
            if (bgColor.A > 0 || borderWidth > 0)
            {
                renderer.DrawRoundedRect(
                    x + borderWidth,
                    y + borderWidth,
                    w - borderWidth * 2,
                    h - borderWidth * 2,
                    Math.Max(0, radius - borderWidth),
                    bgColor.A > 0 ? bgColor : Color.Transparent);
            }
        }
        else
        {
            // Draw border as rectangles
            // Top
            renderer.DrawRect(x, y, w, BorderThickness.Top, borderColor);
            // Bottom
            renderer.DrawRect(x, y + h - BorderThickness.Bottom, w, BorderThickness.Bottom, borderColor);
            // Left
            renderer.DrawRect(x, y + BorderThickness.Top, BorderThickness.Left, h - BorderThickness.Top - BorderThickness.Bottom, borderColor);
            // Right
            renderer.DrawRect(x + w - BorderThickness.Right, y + BorderThickness.Top, BorderThickness.Right, h - BorderThickness.Top - BorderThickness.Bottom, borderColor);
        }
    }
}

/// <summary>
/// Shadow configuration for Border and Card controls
/// </summary>
public class Shadow
{
    /// <summary>
    /// A shadow with no visible effect
    /// </summary>
    public static Shadow None => new Shadow { IsVisible = false };

    public Color Color { get; set; } = new Color(0, 0, 0, 128);
    public float OffsetX { get; set; } = 2;
    public float OffsetY { get; set; } = 2;
    public float BlurRadius { get; set; } = 8;
    public bool IsVisible { get; set; } = true;

    public Shadow() { }

    public Shadow(Color color, float offsetX = 2, float offsetY = 2, float blurRadius = 8)
    {
        Color = color;
        OffsetX = offsetX;
        OffsetY = offsetY;
        BlurRadius = blurRadius;
    }

    /// <summary>
    /// Renders the shadow effect
    /// </summary>
    public void Render(IRenderer renderer, float x, float y, float width, float height, CornerRadius cornerRadius)
    {
        if (!IsVisible) return;
        float maxBlur = Math.Max(0f, BlurRadius);
        float radius = cornerRadius.TopLeft;

        if (maxBlur <= 0f)
        {
            float shadowX = x + OffsetX;
            float shadowY = y + OffsetY;
            if (radius > 0)
            {
                renderer.DrawRoundedRect(shadowX, shadowY, width, height, radius, Color);
            }
            else
            {
                renderer.DrawRect(shadowX, shadowY, width, height, Color);
            }
            return;
        }

        int layers = Math.Clamp((int)MathF.Ceiling(maxBlur * 1.5f), 12, 40);
        float invLayers = 1f / layers;

        for (int i = layers; i >= 1; i--)
        {
            float t = i * invLayers;
            float expand = maxBlur * t;

            float falloff = 1f - t;
            float alpha = Color.A * falloff * falloff * invLayers * 2.5f;

            var shadowColor = Color.WithAlpha(Math.Clamp(alpha, 0f, Color.A));

            float shadowX = x + OffsetX - expand * 0.5f;
            float shadowY = y + OffsetY - expand * 0.5f;
            float shadowW = width + expand;
            float shadowH = height + expand;
            float shadowTopLeft = Math.Max(0f, cornerRadius.TopLeft + expand * 0.5f);
            float shadowTopRight = Math.Max(0f, cornerRadius.TopRight + expand * 0.5f);
            float shadowBottomRight = Math.Max(0f, cornerRadius.BottomRight + expand * 0.5f);
            float shadowBottomLeft = Math.Max(0f, cornerRadius.BottomLeft + expand * 0.5f);

            var path = Rayo.Rendering.Graphics.VectorGraphics.VectorPath.RoundedRectangle(
                shadowX,
                shadowY,
                shadowW,
                shadowH,
                shadowTopLeft,
                shadowTopRight,
                shadowBottomRight,
                shadowBottomLeft);

            renderer.DrawPath(path, shadowColor);
        }
    }

    public static Shadow Default => new Shadow();
    public static Shadow Subtle => new Shadow(new Color(0, 0, 0, 64), 1, 1, 4);
    public static Shadow Strong => new Shadow(new Color(0, 0, 0, 180), 4, 4, 16);
    public static Shadow Colored(Color color) => new Shadow(color);
}
