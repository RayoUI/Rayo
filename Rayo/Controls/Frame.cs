namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Rendering.Graphics.VectorGraphics;

/// <summary>
/// Frame container similar to MAUI Frame - wraps a single content element with background, border, and padding.
/// Inherits from ContentView (single child) following MAUI architecture.
/// </summary>
public class Frame : ContentView<Frame>, IPointerHandler
{
    // =========================================================================
    // FRAME-SPECIFIC PROPERTIES
    // =========================================================================

    #region Border
    private Brush _borderColor = Color.Transparent;
    private float _borderWidth = 0;

    [PaintProperty]
    public Brush BorderColor
    {
        get => _borderColor;
        set => this.SetProperty(ref _borderColor, value);
    }

    [LayoutProperty]
    public float BorderWidth
    {
        get => _borderWidth;
        set => this.SetProperty(ref _borderWidth, value);
    }
    #endregion


    // =========================================================================
    // CONSTRUCTORS
    // =========================================================================

    public Frame()
    {
        // Frame stretches to fill available space by default (like MAUI Frame)
        // This makes Frame behave well in layouts like HStack/VStack
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        
        // IMPORTANT: Do NOT set Width/Height here explicitly
        // Let them remain at their default values so HasExplicitWidth/Height stay false
        // This allows layouts to properly detect Frame as a stretch child
    }

    public Frame(VisualElement content) : this()
    {
        Content = content;
    }
    // =========================================================================
    // LAYOUT
    // =========================================================================

    public override void Measure(float availableWidth, float availableHeight)
    {
        const float InfiniteThreshold = float.PositiveInfinity;

        // STEP 1: Determine Frame's dimensions independently for width and height
        float frameWidth;
        float frameHeight;
        bool hadExplicitWidth = HasExplicitWidth;
        bool hadExplicitHeight = HasExplicitHeight;

        // =========================================================================
        // WIDTH CALCULATION (independent of height)
        // =========================================================================
        if (HasExplicitWidth)
        {
            // Case 1: Explicit width set via Width() or Width property
            frameWidth = Width;
        }
        else if (HorizontalAlignment == HorizontalAlignment.Stretch && availableWidth < InfiniteThreshold)
        {
            // Case 2: Stretch alignment with finite space -> use all available width
            frameWidth = availableWidth;
        }
        else
        {
            // Case 3: Size-to-content mode (non-Stretch or Stretch with infinite space)
            // Set to 0 to measure content with infinity later
            frameWidth = 0;
        }

        // =========================================================================
        // HEIGHT CALCULATION (independent of width)
        // =========================================================================
        if (HasExplicitHeight)
        {
            // Case 1: Explicit height set via Height() or Height property
            frameHeight = Height;
        }
        else if (VerticalAlignment == VerticalAlignment.Stretch && availableHeight < InfiniteThreshold)
        {
            // Case 2: Stretch alignment with finite space -> use all available height
            frameHeight = availableHeight;
        }
        else
        {
            // Case 3: Size-to-content mode (non-Stretch or Stretch with infinite space)
            // Set to 0 to measure content with infinity later
            frameHeight = 0;
        }

        // =========================================================================
        // STEP 2: Measure content with calculated constraints
        // =========================================================================
        float measuredContentWidth = 0;
        float measuredContentHeight = 0;

        if (Content != null)
        {
            // Calculate space available for content
            // When frameWidth/frameHeight is 0 (size-to-content mode), pass infinity to content
            float measureWidth = frameWidth > 0
                ? Math.Max(0, frameWidth - Padding.Horizontal - BorderWidth * 2 - Content.Margin.Horizontal)
                : float.PositiveInfinity;

            float measureHeight = frameHeight > 0
                ? Math.Max(0, frameHeight - Padding.Vertical - BorderWidth * 2 - Content.Margin.Vertical)
                : float.PositiveInfinity;

            Content.Measure(measureWidth, measureHeight);

            // Get content's measured size, handling potential infinity values
            measuredContentWidth = Content.DesiredWidth > 0 ? Content.DesiredWidth : Content.Width;
            measuredContentHeight = Content.DesiredHeight > 0 ? Content.DesiredHeight : Content.Height;

            measuredContentWidth += Content.Margin.Horizontal;
            measuredContentHeight += Content.Margin.Vertical;
            
            // CRITICAL: If content returned infinity or invalid values, use reasonable defaults
            if (float.IsInfinity(measuredContentWidth) || float.IsNaN(measuredContentWidth) || measuredContentWidth <= 0)
            {
                measuredContentWidth = 100; // Minimum default for content
            }
            if (float.IsInfinity(measuredContentHeight) || float.IsNaN(measuredContentHeight) || measuredContentHeight <= 0)
            {
                measuredContentHeight = 100; // Minimum default for content
            }
        }

        // =========================================================================
        // STEP 3: Finalize Frame size (independently for width and height)
        // =========================================================================

        if (!HasExplicitWidth && frameWidth > 0 && HorizontalAlignment == HorizontalAlignment.Stretch && Content != null)
        {
            float minWidth = measuredContentWidth + Padding.Horizontal + BorderWidth * 2;
            frameWidth = Math.Max(frameWidth, minWidth);
        }

        if (!HasExplicitHeight && frameHeight > 0 && VerticalAlignment == VerticalAlignment.Stretch && Content != null)
        {
            float minHeight = measuredContentHeight + Padding.Vertical + BorderWidth * 2;
            frameHeight = Math.Max(frameHeight, minHeight);
        }

        // WIDTH: If frameWidth is 0 (size-to-content), calculate from content
        if (frameWidth == 0)
        {
            frameWidth = Content != null
                ? measuredContentWidth + Padding.Horizontal + BorderWidth * 2
                : Padding.Horizontal + BorderWidth * 2;
        }

        // HEIGHT: If frameHeight is 0 (size-to-content), calculate from content
        if (frameHeight == 0)
        {
            frameHeight = Content != null
                ? measuredContentHeight + Padding.Vertical + BorderWidth * 2
                : Padding.Vertical + BorderWidth * 2;
        }

        // =========================================================================
        // STEP 4: Final validation - prevent infinity values
        // =========================================================================
        if (float.IsInfinity(frameWidth) || float.IsNaN(frameWidth) || frameWidth <= 0)
        {
            frameWidth = 100; // Minimum default width
        }

        if (float.IsInfinity(frameHeight) || float.IsNaN(frameHeight) || frameHeight <= 0)
        {
            frameHeight = 100; // Minimum default height
        }

        // =========================================================================
        // STEP 5: Set final size properties
        // =========================================================================
        // IMPORTANT: Set Width/Height (used by layout system) but preserve HasExplicitWidth/Height
        // so we don't accidentally mark as explicit when it wasn't
        Width = frameWidth;
        Height = frameHeight;
        HasExplicitWidth = hadExplicitWidth;
        HasExplicitHeight = hadExplicitHeight;
        DesiredWidth = frameWidth;
        DesiredHeight = frameHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        // Set computed position and size
        ComputedX = x;
        ComputedY = y;
        ComputedWidth = width;
        ComputedHeight = height;

        if (Content == null) return;

        // Calculate content area
            float contentX = x + Padding.Left + BorderWidth + Content.Margin.Left;
            float contentY = y + Padding.Top + BorderWidth + Content.Margin.Top;
            float contentWidth = Math.Max(0, width - Padding.Horizontal - BorderWidth * 2 - Content.Margin.Horizontal);
            float contentHeight = Math.Max(0, height - Padding.Vertical - BorderWidth * 2 - Content.Margin.Vertical);

        // Get content's desired size (already measured in Measure phase)
        float childWidth = Content.DesiredWidth > 0 ? Content.DesiredWidth : Content.Width;
        float childHeight = Content.DesiredHeight > 0 ? Content.DesiredHeight : Content.Height;

        // Apply content's alignment
        float finalX = contentX;
        float finalY = contentY;
        float finalWidth = childWidth;
        float finalHeight = childHeight;

        // Horizontal alignment
        switch (Content.HorizontalAlignment)
        {
            case HorizontalAlignment.Left:
                finalWidth = Math.Min(childWidth, contentWidth);
                break;

            case HorizontalAlignment.Center:
                finalWidth = Math.Min(childWidth, contentWidth);
                finalX = contentX + (contentWidth - finalWidth) / 2;
                break;

            case HorizontalAlignment.Right:
                finalWidth = Math.Min(childWidth, contentWidth);
                finalX = contentX + contentWidth - finalWidth;
                break;

            case HorizontalAlignment.Stretch:
                finalWidth = contentWidth;
                break;
        }

        // Vertical alignment
        switch (Content.VerticalAlignment)
        {
            case VerticalAlignment.Top:
                finalHeight = Math.Min(childHeight, contentHeight);
                break;

            case VerticalAlignment.Center:
                finalHeight = Math.Min(childHeight, contentHeight);
                finalY = contentY + (contentHeight - finalHeight) / 2;
                break;

            case VerticalAlignment.Bottom:
                finalHeight = Math.Min(childHeight, contentHeight);
                finalY = contentY + contentHeight - finalHeight;
                break;

            case VerticalAlignment.Stretch:
                finalHeight = contentHeight;
                break;
        }

        Content.Arrange(finalX, finalY, finalWidth, finalHeight);
    }

    // =========================================================================
    // RENDERING
    // =========================================================================

    public override void Render(IRenderer renderer)
    {
        // Calculate background area (inset by border if present)
        float bgX = ComputedX;
        float bgY = ComputedY;
        float bgWidth = ComputedWidth;
        float bgHeight = ComputedHeight;
        float bgRadiusAdjust = 0;

        if (BorderWidth > 0 && BorderColor.PrimaryColor.A > 0)
        {
            bgX += BorderWidth;
            bgY += BorderWidth;
            bgWidth -= BorderWidth * 2;
            bgHeight -= BorderWidth * 2;
            bgRadiusAdjust = BorderWidth;
        }

        // Render background
        if (Background.PrimaryColor.A > 0)
        {
            bool uniformRadius = BorderRadius.TopLeft == BorderRadius.TopRight &&
                                 BorderRadius.TopRight == BorderRadius.BottomRight &&
                                 BorderRadius.BottomRight == BorderRadius.BottomLeft;

            if (uniformRadius)
            {
                float radius = Math.Max(0, BorderRadius.TopLeft - bgRadiusAdjust);
                if (radius > 0)
                {
                    renderer.DrawRoundedRect(bgX, bgY, bgWidth, bgHeight, radius, Background);
                }
                else
                {
                    renderer.DrawRect(bgX, bgY, bgWidth, bgHeight, Background);
                }
            }
            else
            {
                var bgPath = VectorPath.RoundedRectangle(
                    bgX, bgY, bgWidth, bgHeight,
                    Math.Max(0, BorderRadius.TopLeft - bgRadiusAdjust),
                    Math.Max(0, BorderRadius.TopRight - bgRadiusAdjust),
                    Math.Max(0, BorderRadius.BottomRight - bgRadiusAdjust),
                    Math.Max(0, BorderRadius.BottomLeft - bgRadiusAdjust)
                );
                renderer.DrawPath(bgPath, Background);
            }
        }

        // Render border
        if (BorderWidth > 0 && BorderColor.PrimaryColor.A > 0)
        {
            bool uniformRadius = BorderRadius.TopLeft == BorderRadius.TopRight &&
                                 BorderRadius.TopRight == BorderRadius.BottomRight &&
                                 BorderRadius.BottomRight == BorderRadius.BottomLeft;

            if (uniformRadius)
            {
                renderer.DrawRoundedRectOutline(
                    ComputedX, ComputedY, ComputedWidth, ComputedHeight,
                    BorderRadius.TopLeft, BorderWidth, BorderColor
                );
            }
            else
            {
                float halfBorder = BorderWidth / 2f;
                var borderPath = VectorPath.RoundedRectangle(
                    ComputedX + halfBorder,
                    ComputedY + halfBorder,
                    ComputedWidth - BorderWidth,
                    ComputedHeight - BorderWidth,
                    Math.Max(0, BorderRadius.TopLeft - halfBorder),
                    Math.Max(0, BorderRadius.TopRight - halfBorder),
                    Math.Max(0, BorderRadius.BottomRight - halfBorder),
                    Math.Max(0, BorderRadius.BottomLeft - halfBorder)
                );
                renderer.DrawPathStroke(borderPath, BorderColor, BorderWidth);
            }
        }

        // Content is rendered automatically by the rendering system via GetChildren()
    }
}
