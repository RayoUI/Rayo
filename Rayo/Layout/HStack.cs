namespace Rayo.Layout;

using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Horizontal layout (row) with advanced alignment and distribution features.
/// Similar to Flexbox row in CSS or HStack in SwiftUI.
/// Migrated to new MAUI-like architecture: inherits from Rayo.Core.Layout<T>
/// </summary>
public class HStack : Layout<HStack>
{
    // =========================================================================
    // PROPERTIES
    // =========================================================================

    #region Spacing
    [LayoutProperty]
    public float Spacing
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region Aligment
    [LayoutProperty]
    public Alignment Alignment
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region JustifyContent
    [LayoutProperty]
    public JustifyContent JustifyContent
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region Wrap
    [LayoutProperty]
    public bool Wrap
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion 

    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    public HStack()
    {
        Spacing = 0;
        Alignment = Alignment.Stretch;
        JustifyContent = JustifyContent.Start;
        Wrap = false;
        HorizontalAlignment = HorizontalAlignment.Left;      // FIX: Left in main-axis (horizontal), not Stretch
        VerticalAlignment = VerticalAlignment.Stretch;       // Stretch in cross-axis (vertical)
    }

    public new HStack Children(params VisualElement[] children)
    {
        foreach (var child in children)
        {
            AddChild(child);
        }
        return this;
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        // CRITICAL: Save explicit size flags before modifying Width/Height
        bool hadExplicitWidth = HasExplicitWidth;
        bool hadExplicitHeight = HasExplicitHeight;

        float totalWidth = Padding.Horizontal;
        float maxHeight = Padding.Vertical;

        int visibleChildren = 0;
        int stretchCount = 0;
        float fixedWidth = 0;

        // PASS 1: Measure fixed-width children and count stretch children
        const float InfiniteThreshold = float.PositiveInfinity;
        foreach (var child in ChildrenInternal.ToArray())
        {
            if (!child.IsVisible)
            {
                continue;
            }

            visibleChildren++;

            // FIX: Use HasExplicitWidth instead of checking Width <= 0
            // A child should only stretch if it has HorizontalAlignment.Stretch AND no explicit width set
            if (child.HorizontalAlignment == HorizontalAlignment.Stretch && !child.HasExplicitWidth && availableWidth < InfiniteThreshold)
            {
                stretchCount++;
            }
            else
            {
                // Measure non-stretch children with infinity to get their natural size
                child.Measure(
                    float.PositiveInfinity,
                    availableHeight - Padding.Vertical
                );

                float childHeight = child.DesiredHeight > 0 ? child.DesiredHeight : child.Height;
                float childWidth = child.DesiredWidth > 0 ? child.DesiredWidth : child.Width;

                fixedWidth += childWidth + child.Margin.Horizontal;
                maxHeight = Math.Max(maxHeight, childHeight + child.Margin.Vertical + Padding.Vertical);
            }
        }

        // Calculate spacing
        float totalSpacing = visibleChildren > 1 ? Spacing * (visibleChildren - 1) : 0;

        // Calculate available width for stretch children
        float availableForStretch = availableWidth - Padding.Horizontal - fixedWidth - totalSpacing;
        float widthPerStretch = stretchCount > 0 ? Math.Max(0, availableForStretch / stretchCount) : 0;

        // PASS 2: Measure stretch children with their allocated width
        foreach (var child in ChildrenInternal.ToArray())
        {
            if (!child.IsVisible)
            {
                continue;
            }

            // FIX: Use HasExplicitWidth instead of checking Width <= 0
            if (child.HorizontalAlignment == HorizontalAlignment.Stretch && !child.HasExplicitWidth && availableWidth < InfiniteThreshold)
            {
                child.Measure(
                    widthPerStretch,
                    availableHeight - Padding.Vertical
                );

                float childHeight = child.DesiredHeight > 0 ? child.DesiredHeight : child.Height;
                float childWidth = child.DesiredWidth > 0 ? child.DesiredWidth : child.Width;

                totalWidth += childWidth + child.Margin.Horizontal;
                maxHeight = Math.Max(maxHeight, childHeight + child.Margin.Vertical + Padding.Vertical);
            }
        }

        // Add fixed width and spacing to total
        totalWidth += fixedWidth + totalSpacing;

        // MAUI Style: HStack only expands in CROSS-AXIS (vertical), never in MAIN-AXIS (horizontal)

        float measuredWidth = Width;
        float measuredHeight = Height;

        // MAIN-AXIS (Width): Use totalWidth OR expand based on HorizontalAlignment
        if (!HasExplicitWidth)
        {
            // If HorizontalAlignment.Stretch AND availableWidth is finite, expand to fill
            // This is important when HStack is root element or in containers expecting expansion
            if (HorizontalAlignment == HorizontalAlignment.Stretch && availableWidth < InfiniteThreshold)
            {
                measuredWidth = Math.Max(totalWidth, availableWidth);
            }
            else
            {
                measuredWidth = totalWidth;
            }
        }

        // CROSS-AXIS (Height): Respect VerticalAlignment
        if (!HasExplicitHeight)
        {
            // If VerticalAlignment.Stretch, expand vertically (cross-axis)
            // BUT ONLY if availableHeight is finite. If infinite, behave as Auto (fit to content).
            if (VerticalAlignment == VerticalAlignment.Stretch && availableHeight < InfiniteThreshold)
            {
                measuredHeight = availableHeight;
            }
            else
            {
                // For Top, Center, Bottom (or Stretch with infinite space): use height of tallest child
                measuredHeight = maxHeight;
            }
        }

        // CRITICAL: Set both Width/Height AND DesiredWidth/DesiredHeight
        // This matches Frame behavior and ensures proper rendering when used as root element
        // IMPORTANT: Restore explicit size flags to preserve layout behavior
        Width = measuredWidth;
        Height = measuredHeight;
        HasExplicitWidth = hadExplicitWidth;
        HasExplicitHeight = hadExplicitHeight;
        DesiredWidth = measuredWidth;
        DesiredHeight = measuredHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        float contentWidth = width - Padding.Horizontal;
        float contentHeight = height - Padding.Vertical;

        // STEP 1: Count children with horizontal Stretch and calculate fixed width
        // First pass: measure non-stretch children to determine fixed width
        bool allowStretch = width > DesiredWidth + 0.1f;
        int stretchCount = 0;
        float fixedWidth = 0;
        
        int visibleChildCount = 0;

        foreach (var child in ChildrenInternal.ToArray())
        {
            if (!child.IsVisible)
            {
                continue;
            }

            visibleChildCount++;
            // FIX: Only treat as Stretch if Width is NOT explicitly set (use HasExplicitWidth)
            // HasExplicitWidth remains false even after Measure() sets Width
            if (child.HorizontalAlignment == HorizontalAlignment.Stretch && !child.HasExplicitWidth && allowStretch)
            {
                stretchCount++;  // This child wants to expand
            }
            else
            {
                // NO re-measure here - use the size already calculated in Measure()
                // Re-measuring can cause layout loops and incorrect sizes
                
                // CRITICAL FIX: Use DesiredWidth (measured size) if explicit Width is not set (0)
                float childWidth = child.HasExplicitWidth ? child.Width : child.DesiredWidth;
                fixedWidth += childWidth + child.Margin.Horizontal;  // Fixed width
            }
        }
     
        // Add spacing
        if (visibleChildCount > 1)
        {
            fixedWidth += Spacing * (visibleChildCount - 1);
        }

        // STEP 2: Calculate available space for Stretch elements
        float availableForStretch = contentWidth - fixedWidth;
        float widthPerStretch = stretchCount > 0 ? availableForStretch / stretchCount : 0;
        
        // STEP 2b: Re-measure stretch children with their allocated width
        // This ensures stretch children know their actual available space
        foreach (var child in ChildrenInternal.ToArray())
        {
            if (!child.IsVisible)
            {
                continue;
            }

            if (child.HorizontalAlignment == HorizontalAlignment.Stretch && !child.HasExplicitWidth && allowStretch)
            {
                child.Measure(widthPerStretch, contentHeight);
            }
        }

        // STEP 3: Calculate total width used by all children (including stretch)
        float totalContentWidth = fixedWidth + (stretchCount * widthPerStretch);

        // STEP 4: Calculate starting position and spacing based on JustifyContent
        float currentX;
        float extraSpacing = 0;

        switch (JustifyContent)
        {
            case JustifyContent.Start:
                currentX = x + Padding.Left;
                break;

            case JustifyContent.Center:
                currentX = x + Padding.Left + (contentWidth - totalContentWidth) / 2;
                break;

            case JustifyContent.End:
                currentX = x + Padding.Left + (contentWidth - totalContentWidth);
                break;

            case JustifyContent.SpaceBetween:
                currentX = x + Padding.Left;
                if (visibleChildCount > 1)
                {
                    float availableSpace = contentWidth - totalContentWidth + (Spacing * (visibleChildCount - 1));
                    extraSpacing = availableSpace / (visibleChildCount - 1) - Spacing;
                }
                break;

            case JustifyContent.SpaceAround:
                if (visibleChildCount > 0)
                {
                    float availableSpace = contentWidth - totalContentWidth + (Spacing * (visibleChildCount - 1));
                    extraSpacing = availableSpace / visibleChildCount - Spacing;
                    currentX = x + Padding.Left + extraSpacing / 2;
                }
                else
                {
                    currentX = x + Padding.Left;
                }
                break;

            case JustifyContent.SpaceEvenly:
                if (visibleChildCount > 0)
                {
                    float availableSpace = contentWidth - totalContentWidth + (Spacing * (visibleChildCount - 1));
                    extraSpacing = availableSpace / (visibleChildCount + 1) - Spacing;
                    currentX = x + Padding.Left + extraSpacing;
                }
                else
                {
                    currentX = x + Padding.Left;
                }
                break;

            default:
                currentX = x + Padding.Left;
                break;
        }

        foreach (var child in ChildrenInternal.ToArray())
        {
            if (!child.IsVisible)
            {
                continue;
            }

            float childX = currentX + child.Margin.Left;
            float childY = y + Padding.Top + child.Margin.Top;
            
            // CRITICAL FIX: Use DesiredWidth/Height (measured size) if explicit Width/Height is not set
            float childWidth = child.HasExplicitWidth ? child.Width : child.DesiredWidth;
            float childHeight = child.HasExplicitHeight ? child.Height : child.DesiredHeight;

            // NEW: Apply horizontal Stretch BEFORE vertical alignment
            // FIX: Only apply stretch if Width is NOT explicitly set
            if (child.HorizontalAlignment == HorizontalAlignment.Stretch && !child.HasExplicitWidth && allowStretch)
            {
                childWidth = Math.Max(0, widthPerStretch - child.Margin.Horizontal);
            }

            // Determine which alignment to use:
            // 1. If child has explicit non-default alignment (Center or Bottom), use it
            // 2. Otherwise, use layout Alignment ting (which applies to Top and Stretch)
            bool useChildAlignment = child.VerticalAlignment == VerticalAlignment.Center ||
                                    child.VerticalAlignment == VerticalAlignment.Bottom;

            if (useChildAlignment)
            {
                // Apply CHILD vertical alignment (has priority for Center/Bottom)
                switch (child.VerticalAlignment)
                {
                    case VerticalAlignment.Center:
                        childY += (contentHeight - childHeight - child.Margin.Vertical) / 2;
                        break;

                    case VerticalAlignment.Bottom:
                        childY += contentHeight - childHeight - child.Margin.Vertical;
                        break;
                }
            }
            else
            {
                // Apply layout Alignment to children with default alignment (Top/Stretch)
                switch (Alignment)
                {
                    case Alignment.Start:
                        // childY is already at correct position (start)
                        break;

                    case Alignment.Center:
                        childY += (contentHeight - childHeight - child.Margin.Vertical) / 2;
                        break;

                    case Alignment.End:
                        childY += contentHeight - childHeight - child.Margin.Vertical;
                        break;

                    case Alignment.Stretch:
                        childHeight = contentHeight - child.Margin.Vertical;
                        break;
                }
            }

            child.Arrange(childX, childY, childWidth, childHeight);

            currentX += childWidth + child.Margin.Horizontal + Spacing + extraSpacing;
        }
    }

    public override void Render(IRenderer renderer)
    {
        // FIX: Render background with REAL measured size, not computed
        // This ensures background covers all scrollable content
        if (Background != null && Background.Opacity > 0 && Background.PrimaryColor.A > 0)
        {
            // Use DesiredHeight if it's larger than ComputedHeight (happens in ScrollView)
            float renderHeight = Math.Max(ComputedHeight, DesiredHeight);
            float renderWidth = Math.Max(ComputedWidth, DesiredWidth);

            renderer.DrawRect(ComputedX, ComputedY, renderWidth, renderHeight, Background);
        }
    }
}
