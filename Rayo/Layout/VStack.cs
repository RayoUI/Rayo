namespace Rayo.Layout;

using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Vertical layout (column) with advanced alignment and distribution features.
/// Similar to Flexbox column in CSS or VStack in SwiftUI.
/// </summary>
public class VStack : Layout<VStack>
{
    #region Properties
    [LayoutProperty]
    public float Spacing
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }

    [LayoutProperty]
    public Alignment Alignment
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }

    [LayoutProperty]
    public JustifyContent JustifyContent
    {
        get => field;
        set => this.SetProperty(ref field, value);
    #endregion
    }

    public VStack(out VStack reference)
    {
        Spacing = 0;
        Alignment = Alignment.Stretch;
        JustifyContent = JustifyContent.Start;
        HorizontalAlignment = HorizontalAlignment.Stretch;  // Stretch in cross-axis (horizontal)
        VerticalAlignment = VerticalAlignment.Top;          // FIX: Top in main-axis (vertical), not Stretch
        ShouldExpand = false;
        reference = this;
    }

    public VStack() : this(out _)
    {
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        float maxWidth = Padding.Horizontal;
        float totalHeight = Padding.Vertical;

        int visibleChildren = 0;
        int stretchCount = 0;
        float fixedHeight = 0;

        // PASS 1: Measure fixed-height children and count stretch children
        const float InfiniteThreshold = float.PositiveInfinity;
        foreach (var child in Children.ToArray())
        {
            if (!child.IsVisible)
            {
                continue;
            }

            visibleChildren++;

            // FIX: Use HasExplicitHeight instead of checking Height <= 0
            // A child should only stretch if it has VerticalAlignment.Stretch AND no explicit height set
            if (child.VerticalAlignment == VerticalAlignment.Stretch && !child.HasExplicitHeight && availableHeight < InfiniteThreshold)
            {
                stretchCount++;
            }
            else
            {
                // Measure non-stretch children with infinity to get their natural size
                child.Measure(
                    availableWidth - Padding.Horizontal,
                    float.PositiveInfinity
                );

                float childHeight = child.DesiredHeight > 0 ? child.DesiredHeight : child.Height;
                float childWidth = child.DesiredWidth > 0 ? child.DesiredWidth : child.Width;

                fixedHeight += childHeight + child.Margin.Vertical;
                maxWidth = Math.Max(maxWidth, childWidth + child.Margin.Horizontal + Padding.Horizontal);
            }
        }

        // Calculate spacing
        float totalSpacing = visibleChildren > 1 ? Spacing * (visibleChildren - 1) : 0;

        // Calculate available height for stretch children
        float availableForStretch = availableHeight - Padding.Vertical - fixedHeight - totalSpacing;
        float heightPerStretch = stretchCount > 0 ? Math.Max(0, availableForStretch / stretchCount) : 0;

        // PASS 2: Measure stretch children with their allocated height
        foreach (var child in Children.ToArray())
        {
            if (!child.IsVisible)
            {
                continue;
            }

            // FIX: Use HasExplicitHeight instead of checking Height <= 0
            if (child.VerticalAlignment == VerticalAlignment.Stretch && !child.HasExplicitHeight && availableHeight < InfiniteThreshold)
            {
                child.Measure(
                    availableWidth - Padding.Horizontal,
                    heightPerStretch
                );

                float childHeight = child.DesiredHeight > 0 ? child.DesiredHeight : child.Height;
                float childWidth = child.DesiredWidth > 0 ? child.DesiredWidth : child.Width;

                totalHeight += childHeight + child.Margin.Vertical;
                maxWidth = Math.Max(maxWidth, childWidth + child.Margin.Horizontal + Padding.Horizontal);
            }
        }

        // Add fixed height and spacing to total
        totalHeight += fixedHeight + totalSpacing;

        // MAUI Style: VStack only expands in CROSS-AXIS (horizontal), never in MAIN-AXIS (vertical)

        float measuredWidth = Width;
        float measuredHeight = Height;

        // CROSS-AXIS (Width): Respect HorizontalAlignment
        if (!HasExplicitWidth)
        {
            // If HorizontalAlignment.Stretch, expand horizontally (cross-axis)
            // BUT ONLY if availableWidth is finite. If infinite, behave as Auto (fit to content).
            if (HorizontalAlignment == HorizontalAlignment.Stretch && availableWidth < InfiniteThreshold)
            {
                measuredWidth = availableWidth;
            }
            else
            {
                // For Left, Center, Right (or Stretch with infinite space): use width of widest child
                measuredWidth = maxWidth;
            }
        }
        else
        {
            measuredWidth = Width;
        }
        
        // Update DesiredSize
        DesiredWidth = measuredWidth;

        // MAIN-AXIS (Height): ALWAYS use totalHeight, NEVER expand based on VerticalAlignment
        // VerticalAlignment is used by the PARENT to position the VStack, not by VStack itself
        if (!HasExplicitHeight)
        {
            if (Parent == null && availableHeight < InfiniteThreshold)
            {
                measuredHeight = availableHeight;
            }
            else
            {
                measuredHeight = totalHeight;
            }
        }
        else
        {
            measuredHeight = Height;
        }

        // Update DesiredSize
        DesiredHeight = measuredHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        float contentWidth = width - Padding.Horizontal;
        float contentHeight = height - Padding.Vertical;

        // STEP 1: Count children with vertical Stretch and calculate fixed height
        // First pass: measure non-stretch children to determine fixed height
        bool allowStretch = height > DesiredHeight + 0.1f;
        int stretchCount = 0;
        float fixedHeight = 0;

        int visibleChildCount = 0;

        foreach (var child in Children.ToArray())
        {
            if (!child.IsVisible)
            {
                continue;
            }

            visibleChildCount++;
            // FIX: Only treat as Stretch if Height is NOT explicitly set (use HasExplicitHeight)
            if (child.VerticalAlignment == VerticalAlignment.Stretch && !child.HasExplicitHeight && allowStretch)
            {
                stretchCount++;
            }
            else
            {
                // NO re-measure here - use the size already calculated in Measure()
                // Re-measuring can cause layout loops and incorrect sizes
                
                // CRITICAL FIX: Use DesiredHeight (measured size) if explicit Height is not set
                float childHeight = child.HasExplicitHeight ? child.Height : child.DesiredHeight;
                fixedHeight += childHeight + child.Margin.Vertical;
            }
        }

        // Add spacing
        if (visibleChildCount > 1)
        {
            fixedHeight += Spacing * (visibleChildCount - 1);
        }

        // STEP 2: Calculate available space for Stretch elements
        float availableForStretch = contentHeight - fixedHeight;
        float heightPerStretch = stretchCount > 0 ? availableForStretch / stretchCount : 0;
        
        // STEP 2b: Re-measure stretch children with their allocated height
        // This ensures stretch children know their actual available space
        foreach (var child in Children.ToArray())
        {
            if (!child.IsVisible)
            {
                continue;
            }

            if (child.VerticalAlignment == VerticalAlignment.Stretch && !child.HasExplicitHeight && allowStretch)
            {
                child.Measure(contentWidth, heightPerStretch);
            }
        }

        // STEP 3: Calculate total height used by all children (including stretch)
        float totalContentHeight = fixedHeight + (stretchCount * heightPerStretch);

        // STEP 4: Calculate starting position and spacing based on JustifyContent
        float currentY;
        float extraSpacing = 0;

        switch (JustifyContent)
        {
            case JustifyContent.Start:
                currentY = y + Padding.Top;
                break;

            case JustifyContent.Center:
                currentY = y + Padding.Top + (contentHeight - totalContentHeight) / 2;
                break;

            case JustifyContent.End:
                currentY = y + Padding.Top + (contentHeight - totalContentHeight);
                break;

            case JustifyContent.SpaceBetween:
                currentY = y + Padding.Top;
                if (visibleChildCount > 1)
                {
                    float availableSpace = contentHeight - totalContentHeight + (Spacing * (visibleChildCount - 1));
                    extraSpacing = availableSpace / (visibleChildCount - 1) - Spacing;
                }
                break;

            case JustifyContent.SpaceAround:
                if (visibleChildCount > 0)
                {
                    float availableSpace = contentHeight - totalContentHeight + (Spacing * (visibleChildCount - 1));
                    extraSpacing = availableSpace / visibleChildCount - Spacing;
                    currentY = y + Padding.Top + extraSpacing / 2;
                }
                else
                {
                    currentY = y + Padding.Top;
                }
                break;

            case JustifyContent.SpaceEvenly:
                if (visibleChildCount > 0)
                {
                    float availableSpace = contentHeight - totalContentHeight + (Spacing * (visibleChildCount - 1));
                    extraSpacing = availableSpace / (visibleChildCount + 1) - Spacing;
                    currentY = y + Padding.Top + extraSpacing;
                }
                else
                {
                    currentY = y + Padding.Top;
                }
                break;

            default:
                currentY = y + Padding.Top;
                break;
        }

        foreach (var child in Children.ToArray())
        {
            if (child is null || !child.IsVisible)
            {
                continue;
            }

            float childX = x + Padding.Left + child.Margin.Left;
            float childY = currentY + child.Margin.Top;
            
            // CRITICAL FIX: Use DesiredWidth/Height (measured size) if explicit Width/Height is not set
            float childWidth = child.HasExplicitWidth ? child.Width : child.DesiredWidth;
            float childHeight = child.HasExplicitHeight ? child.Height : child.DesiredHeight;

            // NEW: Apply vertical Stretch BEFORE horizontal alignment
            // FIX: Only apply stretch if Height is NOT explicitly set
            if (child.VerticalAlignment == VerticalAlignment.Stretch && !child.HasExplicitHeight && allowStretch)
            {
                childHeight = Math.Max(0, heightPerStretch - child.Margin.Vertical);
            }

            // Determine which alignment to use:
            // 1. If child has explicit non-default alignment (Center or Right), use it
            // 2. Otherwise, use layout Alignment ting (which applies to Left and Stretch)
            bool useChildAlignment = child.HorizontalAlignment == HorizontalAlignment.Center ||
                                    child.HorizontalAlignment == HorizontalAlignment.Right;

            if (useChildAlignment)
            {
                // Apply CHILD horizontal alignment (has priority for Center/Right)
                switch (child.HorizontalAlignment)
                {
                    case HorizontalAlignment.Center:
                        childX += (contentWidth - childWidth - child.Margin.Horizontal) / 2;
                        break;

                    case HorizontalAlignment.Right:
                        childX += contentWidth - childWidth - child.Margin.Horizontal;
                        break;
                }
            }
            else
            {
                // Apply layout Alignment to children with default alignment (Left/Stretch)
                switch (Alignment)
                {
                    case Alignment.Start:
                        // childX is already at correct position (start)
                        break;

                    case Alignment.Center:
                        childX += (contentWidth - childWidth - child.Margin.Horizontal) / 2;
                        break;

                    case Alignment.End:
                        childX += contentWidth - childWidth - child.Margin.Horizontal;
                        break;

                    case Alignment.Stretch:
                        childWidth = contentWidth - child.Margin.Horizontal;
                        break;
                }
            }

            child.Arrange(childX, childY, childWidth, childHeight);

            currentY += childHeight + child.Margin.Vertical + Spacing + extraSpacing;
        }
    }

    public override void Render(IRenderer renderer)
    {
        // FIX: Render background with REAL measured size, not computed
        // This ensures background covers all scrollable content

            // Use DesiredHeight if it's larger than ComputedHeight (happens in ScrollView)
            float renderHeight = Math.Max(ComputedHeight, DesiredHeight);
            float renderWidth = Math.Max(ComputedWidth, DesiredWidth);

            renderer.DrawRect(ComputedX, ComputedY, renderWidth, renderHeight, Background);
    }
}