namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Styling;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Displays an icon through the same image pipeline used by <see cref="ButtonIcon"/>.
/// SVG-backed <see cref="IconData"/> instances are tinted with <see cref="Color"/>.
/// </summary>
public class Icon : CompositeView<Icon>
{
    private const float DefaultIconSize = 24f;
    private readonly Image _image;

    [PaintProperty]
    public IconData? IconData
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateImageSource);
    }

    [PaintProperty]
    public Brush Color
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateImageTint);
    } = Rayo.Rendering.Color.Transparent;

    /// <summary>
    /// Internal image that renders the SVG or raster source.
    /// </summary>
    public Image Image => _image;

    public Icon()
    {
        _image = new Image { Stretch = StretchMode.Uniform };
        AddChild(_image);
        InitializeTheme();
    }

    public Icon(IconData iconData) : this()
    {
        IconData = iconData;
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        SetThemeValue(nameof(Color), (Brush)theme.Colors.OnSurface, value => Color = value);
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        float size = MathF.Min(
            Width > 0 ? Width : DefaultIconSize,
            Height > 0 ? Height : DefaultIconSize);

        DesiredWidth = Width > 0 ? Width : size;
        DesiredHeight = Height > 0 ? Height : size;
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
        _image.ForceArrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        // The UI tree renders the internal Image after this control.
    }

    private void UpdateImageSource()
    {
        if (_image is null)
            return;

        _image.Source = IconData?.ImageSource;
        InvalidateMeasure();
    }

    private void UpdateImageTint()
    {
        if (_image is null)
            return;

        _image.Tint = Color.PrimaryColor;
    }
}
