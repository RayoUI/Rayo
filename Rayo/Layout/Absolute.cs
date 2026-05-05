namespace Rayo.Layout;

using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;

/// <summary>
/// Absolute layout that allows absolute positioning of children.
/// Children are positioned using their X and Y properties relative to the Absolute origin.
/// Similar to Absolute in WPF/MAUI.
/// Migrated to new MAUI-like architecture: inherits from Rayo.Core.Layout<T>
/// </summary>
public class Absolute : CompositeView<Absolute>
{
    public Absolute()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        ClipToBounds = false;
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        // Measure all children with infinite space since they're absolutely positioned
        foreach (var child in Children.ToArray())
        {
            child.Measure(float.MaxValue, float.MaxValue);
        }

        // Absolute takes available space unless explicit size is set
        if (HasExplicitWidth)
        {
            DesiredWidth = Width;
        }
        else if (HorizontalAlignment == HorizontalAlignment.Stretch)
        {
            DesiredWidth = availableWidth;
        }
        else
        {
            // Calculate bounds from children positions
            float maxRight = 0;
            foreach (var child in Children.ToArray())
            {
                float right = child.X + child.DesiredWidth;
                if (right > maxRight) maxRight = right;
            }
            DesiredWidth = maxRight + Padding.Right;
        }

        if (HasExplicitHeight)
        {
            DesiredHeight = Height;
        }
        else if (VerticalAlignment == VerticalAlignment.Stretch)
        {
            DesiredHeight = availableHeight;
        }
        else
        {
            // Calculate bounds from children positions
            float maxBottom = 0;
            foreach (var child in Children.ToArray())
            {
                float bottom = child.Y + child.DesiredHeight;
                if (bottom > maxBottom) maxBottom = bottom;
            }
            DesiredHeight = maxBottom + Padding.Bottom;
        }
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        float contentX = x + Padding.Left;
        float contentY = y + Padding.Top;

        // Arrange each child at its absolute position relative to Absolute origin
        foreach (var child in Children.ToArray())
        {
            float childX = contentX + child.X;
            float childY = contentY + child.Y;

            // Use child's desired size or explicit size
            float childWidth = child.Width > 0 ? child.Width : child.DesiredWidth;
            float childHeight = child.Height > 0 ? child.Height : child.DesiredHeight;

            child.Arrange(childX, childY, childWidth, childHeight);
        }
    }

    public override void Render(IRenderer renderer)
    {
        // Render background if it has color
        if (Background != null && Background.Opacity > 0 && Background.PrimaryColor.A > 0)
        {
            float renderHeight = Math.Max(ComputedHeight, DesiredHeight);
            float renderWidth = Math.Max(ComputedWidth, DesiredWidth);

            renderer.DrawRect(ComputedX, ComputedY, renderWidth, renderHeight, Background);
        }

        if (ClipToBounds)
        {
            renderer.PushScissor(ComputedX, ComputedY, ComputedWidth, ComputedHeight);
        }

        // Children are rendered by the UI tree automatically

        if (ClipToBounds)
        {
            renderer.PopScissor();
        }
    }
}

/// <summary>
/// Extension methods for positioning elements on a Absolute
/// </summary>
public static class AbsoluteFluentApiExtensions
{
    /// <summary>
    /// Sets the position of an element for use in a Absolute
    /// </summary>
    public static T AbsolutePosition<T>(this T element, float x, float y) where T : VisualElement<T>
    {
        element.X = x;
        element.Y = y;
        return element;
    }

    /// <summary>
    /// Sets the left position of an element for use in a Absolute
    /// </summary>
    public static T AbsoluteLeft<T>(this T element, float x) where T : VisualElement<T>
    {
        element.X = x;
        return element;
    }

    /// <summary>
    /// Sets the top position of an element for use in a Absolute
    /// </summary>
    public static T AbsoluteTop<T>(this T element, float y) where T : VisualElement<T>
    {
        element.Y = y;
        return element;
    }
}
