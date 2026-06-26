namespace Rayo.Core;

using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>
/// Base class for leaf controls that render their own rounded visual box.
/// </summary>
public abstract class BorderView<T> : View<T> where T : BorderView<T>
{
    [PaintProperty]
    public Brush BorderBrush
    {
        get => field;
        set => this.SetProperty(ref field, value, OnBorderBrushChanged);
    } = Color.Transparent;

    [LayoutProperty]
    public Thickness BorderThickness
    {
        get => field;
        set => this.SetProperty(ref field, value, OnBorderThicknessChanged);
    } = new Thickness(0);

    [PaintProperty]
    public CornerRadius BorderRadius
    {
        get => field;
        set => this.SetProperty(ref field, value, OnBorderRadiusChanged);
    } = CornerRadius.None;

    internal override CornerRadius VisualCornerRadius => BorderRadius;

    protected virtual void OnBorderBrushChanged() => MarkNeedsPaint();

    protected virtual void OnBorderThicknessChanged()
    {
        InvalidateMeasure();
        MarkNeedsPaint();
    }

    protected virtual void OnBorderRadiusChanged() => MarkNeedsPaint();
}

/// <summary>
/// Base class for composite controls that render their own rounded visual box.
/// </summary>
public abstract class BorderCompositeView<T> : CompositeView<T> where T : BorderCompositeView<T>
{
    [PaintProperty]
    public Brush BorderBrush
    {
        get => field;
        set => this.SetProperty(ref field, value, OnBorderBrushChanged);
    } = Color.Transparent;

    [LayoutProperty]
    public Thickness BorderThickness
    {
        get => field;
        set => this.SetProperty(ref field, value, OnBorderThicknessChanged);
    } = new Thickness(0);

    [PaintProperty]
    public CornerRadius BorderRadius
    {
        get => field;
        set => this.SetProperty(ref field, value, OnBorderRadiusChanged);
    } = CornerRadius.None;

    internal override CornerRadius VisualCornerRadius => BorderRadius;

    protected virtual void OnBorderBrushChanged() => MarkNeedsPaint();

    protected virtual void OnBorderThicknessChanged()
    {
        InvalidateMeasure();
        MarkNeedsPaint();
    }

    protected virtual void OnBorderRadiusChanged() => MarkNeedsPaint();
}

/// <summary>
/// Base class for single-content controls that render their own rounded visual box.
/// </summary>
public abstract class BorderContentView<T> : ContentView<T> where T : BorderContentView<T>
{
    [PaintProperty]
    public Brush BorderBrush
    {
        get => field;
        set => this.SetProperty(ref field, value, OnBorderBrushChanged);
    } = Color.Transparent;

    [LayoutProperty]
    public Thickness BorderThickness
    {
        get => field;
        set => this.SetProperty(ref field, value, OnBorderThicknessChanged);
    } = new Thickness(0);

    [PaintProperty]
    public CornerRadius BorderRadius
    {
        get => field;
        set => this.SetProperty(ref field, value, OnBorderRadiusChanged);
    } = CornerRadius.None;

    internal override CornerRadius VisualCornerRadius => BorderRadius;

    protected virtual void OnBorderBrushChanged() => MarkNeedsPaint();

    protected virtual void OnBorderThicknessChanged()
    {
        InvalidateMeasure();
        MarkNeedsPaint();
    }

    protected virtual void OnBorderRadiusChanged() => MarkNeedsPaint();
}
