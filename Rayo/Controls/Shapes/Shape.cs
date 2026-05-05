namespace Rayo.Controls.Shapes;

using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>
/// Base class for all shape controls
/// </summary>
public abstract class Shape<T> : Rayo.Core.View<T> where T : Rayo.Core.View<T>
{


    [PaintProperty]
    public Brush Fill
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new SolidColorBrush(Color.Transparent);

    [PaintProperty]
    public Brush Stroke
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new SolidColorBrush(Color.Black );

    [PaintProperty]
    public float StrokeThickness
    {
        get => field;
        set
        {
            value = Math.Max(0, value);
            this.SetProperty(ref field, value);
        }
    } = 1;

    [PaintProperty]
    public float[]? StrokeDashArray
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }

    [LayoutProperty]
    public ShapeStretch Stretch
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = ShapeStretch.None;

    protected Shape() { }

    public override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth = Width > 0 ? Width : 0;
        DesiredHeight = Height > 0 ? Height : 0;

        if (Stretch == ShapeStretch.Fill || Stretch == ShapeStretch.UniformToFill)
        {
            if (Width <= 0) DesiredWidth = availableWidth;
            if (Height <= 0) DesiredHeight = availableHeight;
        }
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }
}


/// <summary>
/// Specifies how a shape stretches to fill available space
/// </summary>
public enum ShapeStretch
{
    /// <summary>
    /// Shape uses its natural size
    /// </summary>
    None,

    /// <summary>
    /// Shape fills available space, may not preserve aspect ratio
    /// </summary>
    Fill,

    /// <summary>
    /// Shape fills available space while preserving aspect ratio (may have empty space)
    /// </summary>
    Uniform,

    /// <summary>
    /// Shape fills available space while preserving aspect ratio (may be clipped)
    /// </summary>
    UniformToFill
}
