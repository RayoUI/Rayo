namespace Rayo.Controls;

using Rayo;
using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>
/// Predefined sizes for <see cref="ButtonFloat"/>.
/// </summary>
public enum ButtonFloatSize
{
    Small,
    Normal,
    Large
}

/// <summary>
/// Corner placement helper for floating action buttons.
/// </summary>
public enum ButtonFloatPlacement
{
    BottomRight,
    BottomLeft,
    TopRight,
    TopLeft
}

/// <summary>
/// Floating action button built on top of <see cref="ButtonIcon"/>.
/// Add it as the last child in a Grid cell or Absolute layer so its high ZIndex
/// can float above the main content.
/// </summary>
public class ButtonFloat : ButtonIcon
{
    #region FloatSize
    [LayoutProperty]
    public ButtonFloatSize FloatSize
    {
        get => field;
        set => this.SetProperty(ref field, value, ApplySize);
    } = ButtonFloatSize.Normal;
    #endregion

    #region Placement
    [LayoutProperty]
    public ButtonFloatPlacement Placement
    {
        get => field;
        set => this.SetProperty(ref field, value, ApplyPlacement);
    } = ButtonFloatPlacement.BottomRight;
    #endregion

    #region Offset
    [LayoutProperty]
    public float Offset
    {
        get => field;
        set => this.SetProperty(ref field, value, ApplyPlacement);
    } = 16;
    #endregion

    public ButtonFloat() : this(Icons.Add)
    {
    }

    public ButtonFloat(IconData iconData) : base(iconData)
    {
        Background = new Color(59, 130, 246);
        HoverBackground = new Color(37, 99, 235);
        PressedBackground = new Color(29, 78, 216);
        IconColor = Color.White;
        BorderWidth = 0;
        ZIndex = 1000;

        ApplySize();
        ApplyPlacement();
        this.WithShadow(0, 6, 14, new Color(0, 0, 0, 110));
    }

    public ButtonFloat Dock(ButtonFloatPlacement placement, float offset = 16)
    {
        Placement = placement;
        Offset = offset;
        return this;
    }

    private void ApplySize()
    {
        float size = FloatSize switch
        {
            ButtonFloatSize.Small => 48,
            ButtonFloatSize.Large => 72,
            _ => 56
        };

        float iconSize = FloatSize switch
        {
            ButtonFloatSize.Small => 20,
            ButtonFloatSize.Large => 32,
            _ => 24
        };

        Width = size;
        Height = size;
        IconSize = iconSize;
        BorderRadius = new CornerRadius(size / 2);
    }

    private void ApplyPlacement()
    {
        HorizontalAlignment = Placement is ButtonFloatPlacement.BottomRight or ButtonFloatPlacement.TopRight
            ? HorizontalAlignment.Right
            : HorizontalAlignment.Left;

        VerticalAlignment = Placement is ButtonFloatPlacement.BottomRight or ButtonFloatPlacement.BottomLeft
            ? VerticalAlignment.Bottom
            : VerticalAlignment.Top;

        Margin = new Thickness(Offset);
    }
}
