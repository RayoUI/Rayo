namespace Rayo.Layout;

using Rayo.Core;
using Rayo.Reactivity;
using IRenderer = Rayo.Rendering.IRenderer;

public class LStack : Layout<LStack>
{
    #region Properties
    [LayoutProperty]
    public Orientation Orientation
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }

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
    } 
    #endregion

    public LStack()
    {
        Orientation = Orientation.Vertical;
        Spacing = 0;
        Alignment = Alignment.Stretch;
        JustifyContent = JustifyContent.Start;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        ShouldExpand = false;
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        const float InfiniteThreshold = float.PositiveInfinity;

        if (Orientation == Orientation.Vertical)
        {
            // Vertical orientation: measure children with infinite height
            float maxWidth = Padding.Horizontal;
            float totalHeight = Padding.Vertical;

            foreach (var child in ChildrenInternal.ToArray())
            {
                child.Measure(
                    availableWidth - Padding.Horizontal,
                    float.PositiveInfinity
                );

                float childHeight = child.DesiredHeight > 0 ? child.DesiredHeight : child.Height;
                float childWidth = child.DesiredWidth > 0 ? child.DesiredWidth : child.Width;

                totalHeight += childHeight + child.Margin.Vertical;
                maxWidth = Math.Max(maxWidth, childWidth + child.Margin.Horizontal + Padding.Horizontal);
            }

            if (Children.Count > 1)
            {
                totalHeight += Spacing * (Children.Count - 1);
            }

            float measuredWidth = Width;
            float measuredHeight = Height;

            // CROSS-AXIS (Width): Respect HorizontalAlignment
            if (!HasExplicitWidth)
            {
                if (HorizontalAlignment == HorizontalAlignment.Stretch && availableWidth < InfiniteThreshold)
                {
                    measuredWidth = availableWidth;
                }
                else
                {
                    measuredWidth = maxWidth;
                }
            }

            DesiredWidth = measuredWidth;

            // MAIN-AXIS (Height): Always use totalHeight
            if (!HasExplicitHeight)
            {
                measuredHeight = totalHeight;
            }

            DesiredHeight = measuredHeight;
        }
        else // Horizontal
        {
            // Horizontal orientation: measure children with infinite width
            float totalWidth = Padding.Horizontal;
            float maxHeight = Padding.Vertical;

            foreach (var child in ChildrenInternal.ToArray())
            {
                child.Measure(
                    float.PositiveInfinity,
                    availableHeight - Padding.Vertical
                );

                float childHeight = child.DesiredHeight > 0 ? child.DesiredHeight : child.Height;
                float childWidth = child.DesiredWidth > 0 ? child.DesiredWidth : child.Width;

                totalWidth += childWidth + child.Margin.Horizontal;
                maxHeight = Math.Max(maxHeight, childHeight + child.Margin.Vertical + Padding.Vertical);
            }

            if (Children.Count > 1)
            {
                totalWidth += Spacing * (Children.Count - 1);
            }

            float measuredWidth = Width;
            float measuredHeight = Height;

            // MAIN-AXIS (Width): Always use totalWidth
            if (!HasExplicitWidth)
            {
                measuredWidth = totalWidth;
            }

            DesiredWidth = measuredWidth;

            // CROSS-AXIS (Height): Respect VerticalAlignment
            if (!HasExplicitHeight)
            {
                if (VerticalAlignment == VerticalAlignment.Stretch && availableHeight < InfiniteThreshold)
                {
                    measuredHeight = availableHeight;
                }
                else
                {
                    measuredHeight = maxHeight;
                }
            }

            DesiredHeight = measuredHeight;
        }
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        float contentWidth = width - Padding.Horizontal;
        float contentHeight = height - Padding.Vertical;

        if (Orientation == Orientation.Vertical)
        {
            ArrangeVertical(x, y, contentWidth, contentHeight);
        }
        else
        {
            ArrangeHorizontal(x, y, contentWidth, contentHeight);
        }
    }

    private void ArrangeVertical(float x, float y, float contentWidth, float contentHeight)
    {
        // STEP 1: Count children with vertical Stretch and calculate fixed height
        // First pass: measure non-stretch children to determine fixed height
        int stretchCount = 0;
        float fixedHeight = 0;

        foreach (var child in ChildrenInternal.ToArray())
        {
            if (child.VerticalAlignment == VerticalAlignment.Stretch && !child.HasExplicitHeight)
            {
                stretchCount++;
            }
            else
            {
                // Re-measure non-stretch children with available space
                child.Measure(contentWidth, float.PositiveInfinity);
                
                float childHeight = child.HasExplicitHeight ? child.Height : child.DesiredHeight;
                fixedHeight += childHeight + child.Margin.Vertical;
            }
        }

        if (Children.Count > 1)
        {
            fixedHeight += Spacing * (Children.Count - 1);
        }

        // STEP 2: Calculate available space for Stretch elements
        float availableForStretch = contentHeight - fixedHeight;
        float heightPerStretch = stretchCount > 0 ? availableForStretch / stretchCount : 0;
        
        // STEP 2b: Re-measure stretch children with their allocated height
        foreach (var child in ChildrenInternal.ToArray())
        {
            if (child.VerticalAlignment == VerticalAlignment.Stretch && !child.HasExplicitHeight)
            {
                child.Measure(contentWidth, heightPerStretch);
            }
        }

        // STEP 3: Calculate total height used by all children
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
                if (Children.Count > 1)
                {
                    float availableSpace = contentHeight - totalContentHeight + (Spacing * (Children.Count - 1));
                    extraSpacing = availableSpace / (Children.Count - 1) - Spacing;
                }
                break;
            case JustifyContent.SpaceAround:
                if (Children.Count > 0)
                {
                    float availableSpace = contentHeight - totalContentHeight + (Spacing * (Children.Count - 1));
                    extraSpacing = availableSpace / Children.Count - Spacing;
                    currentY = y + Padding.Top + extraSpacing / 2;
                }
                else
                {
                    currentY = y + Padding.Top;
                }
                break;
            case JustifyContent.SpaceEvenly:
                if (Children.Count > 0)
                {
                    float availableSpace = contentHeight - totalContentHeight + (Spacing * (Children.Count - 1));
                    extraSpacing = availableSpace / (Children.Count + 1) - Spacing;
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

        // STEP 5: Position children
        foreach (var child in ChildrenInternal.ToArray())
        {
            float childX = x + Padding.Left + child.Margin.Left;
            float childY = currentY + child.Margin.Top;

            float childWidth = child.HasExplicitWidth ? child.Width : child.DesiredWidth;
            float childHeight = child.HasExplicitHeight ? child.Height : child.DesiredHeight;

            // Apply vertical Stretch
            if (child.VerticalAlignment == VerticalAlignment.Stretch && !child.HasExplicitHeight)
            {
                childHeight = Math.Max(0, heightPerStretch - child.Margin.Vertical);
            }

            // Apply horizontal alignment (cross-axis)
            // First check child's own alignment, then fallback to container's Alignment
            switch (child.HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    // Use container's Alignment as fallback
                    switch (Alignment)
                    {
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
                    break;
                case HorizontalAlignment.Center:
                    childX += (contentWidth - childWidth - child.Margin.Horizontal) / 2;
                    break;
                case HorizontalAlignment.Right:
                    childX += contentWidth - childWidth - child.Margin.Horizontal;
                    break;
                case HorizontalAlignment.Stretch:
                    childWidth = contentWidth - child.Margin.Horizontal;
                    break;
            }

            child.Arrange(childX, childY, childWidth, childHeight);
            currentY += childHeight + child.Margin.Vertical + Spacing + extraSpacing;
        }
    }

    private void ArrangeHorizontal(float x, float y, float contentWidth, float contentHeight)
    {
        // STEP 1: Count children with horizontal Stretch and calculate fixed width
        // First pass: measure non-stretch children to determine fixed width
        int stretchCount = 0;
        float fixedWidth = 0;

        foreach (var child in ChildrenInternal.ToArray())
        {
            if (child.HorizontalAlignment == HorizontalAlignment.Stretch && !child.HasExplicitWidth)
            {
                stretchCount++;
            }
            else
            {
                // Re-measure non-stretch children with available space
                child.Measure(float.PositiveInfinity, contentHeight);
                
                float childWidth = child.HasExplicitWidth ? child.Width : child.DesiredWidth;
                fixedWidth += childWidth + child.Margin.Horizontal;
            }
        }

        if (Children.Count > 1)
        {
            fixedWidth += Spacing * (Children.Count - 1);
        }

        // STEP 2: Calculate available space for Stretch elements
        float availableForStretch = contentWidth - fixedWidth;
        float widthPerStretch = stretchCount > 0 ? availableForStretch / stretchCount : 0;
        
        // STEP 2b: Re-measure stretch children with their allocated width
        foreach (var child in ChildrenInternal.ToArray())
        {
            if (child.HorizontalAlignment == HorizontalAlignment.Stretch && !child.HasExplicitWidth)
            {
                child.Measure(widthPerStretch, contentHeight);
            }
        }

        // STEP 3: Calculate total width used by all children
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
                if (Children.Count > 1)
                {
                    float availableSpace = contentWidth - totalContentWidth + (Spacing * (Children.Count - 1));
                    extraSpacing = availableSpace / (Children.Count - 1) - Spacing;
                }
                break;
            case JustifyContent.SpaceAround:
                if (Children.Count > 0)
                {
                    float availableSpace = contentWidth - totalContentWidth + (Spacing * (Children.Count - 1));
                    extraSpacing = availableSpace / Children.Count - Spacing;
                    currentX = x + Padding.Left + extraSpacing / 2;
                }
                else
                {
                    currentX = x + Padding.Left;
                }
                break;
            case JustifyContent.SpaceEvenly:
                if (Children.Count > 0)
                {
                    float availableSpace = contentWidth - totalContentWidth + (Spacing * (Children.Count - 1));
                    extraSpacing = availableSpace / (Children.Count + 1) - Spacing;
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

        // STEP 5: Position children
        foreach (var child in ChildrenInternal.ToArray())
        {
            float childX = currentX + child.Margin.Left;
            float childY = y + Padding.Top + child.Margin.Top;

            float childWidth = child.HasExplicitWidth ? child.Width : child.DesiredWidth;
            float childHeight = child.HasExplicitHeight ? child.Height : child.DesiredHeight;

            // Apply horizontal Stretch
            if (child.HorizontalAlignment == HorizontalAlignment.Stretch && !child.HasExplicitWidth)
            {
                childWidth = Math.Max(0, widthPerStretch - child.Margin.Horizontal);
            }

            // Apply vertical alignment (cross-axis)
            // First check child's own alignment, then fallback to container's Alignment
            switch (child.VerticalAlignment)
            {
                case VerticalAlignment.Top:
                    // Use container's Alignment as fallback
                    switch (Alignment)
                    {
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
                    break;
                case VerticalAlignment.Center:
                    childY += (contentHeight - childHeight - child.Margin.Vertical) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    childY += contentHeight - childHeight - child.Margin.Vertical;
                    break;
                case VerticalAlignment.Stretch:
                    childHeight = contentHeight - child.Margin.Vertical;
                    break;
            }

            child.Arrange(childX, childY, childWidth, childHeight);
            currentX += childWidth + child.Margin.Horizontal + Spacing + extraSpacing;
        }
    }

    public override void Render(IRenderer renderer)
    {
        // Render background if it has alpha
        if (Background != null && Background.Opacity > 0 && Background.PrimaryColor.A > 0)
        {
            // Use DesiredHeight if it's larger than ComputedHeight (happens in ScrollView)
            float renderHeight = Math.Max(ComputedHeight, DesiredHeight);
            float renderWidth = Math.Max(ComputedWidth, DesiredWidth);

            renderer.DrawRect(ComputedX, ComputedY, renderWidth, renderHeight, Background);
        }
    }
}