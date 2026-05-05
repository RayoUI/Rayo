namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;
using IRenderer = Rayo.Rendering.IRenderer;
using ITexture = Rayo.Rendering.ITexture;

/// <summary>
/// Modos de estiramiento para imágenes
/// </summary>
public enum StretchMode
{
    /// <summary>
    /// Muestra la imagen en su tamaño original (sin escalar)
    /// </summary>
    None,

    /// <summary>
    /// Estira la imagen para llenar completamente el área (puede distorsionar)
    /// </summary>
    Fill,

    /// <summary>
    /// Escala uniformemente para que quepa completa en el área (mantiene aspect ratio)
    /// </summary>
    Uniform,

    /// <summary>
    /// Escala uniformemente para llenar el área (mantiene aspect ratio, puede recortar)
    /// </summary>
    UniformToFill
}

/// <summary>
/// Componente para mostrar imágenes desde archivos locales, URLs de red o streams
/// Migrated to new MAUI-like architecture: inherits from View<Image>
/// </summary>
public class Image : Rayo.Core.View<Image>
{
    #region Source
    [LayoutProperty]
    public ImageSource? Source
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value))
            {
                // Reset texture loading state when source changes
                _textureLoaded = false;
                _loadTask = null;
                _texture = null;
            }
        }
    }
    #endregion

    #region Stretch
    [PaintProperty]
    public StretchMode Stretch
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = StretchMode.Uniform;
    #endregion

    #region Tint
    [PaintProperty]
    public Color? Tint
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    /// <summary>
    /// Indica si la imagen está cargando (útil para mostrar indicador de carga)
    /// </summary>
    public bool IsLoading => Source?.IsLoading ?? false;

    /// <summary>
    /// Indica si la imagen se cargó correctamente
    /// </summary>
    public bool IsLoaded => Source?.IsLoaded ?? false;

    /// <summary>
    /// Error de carga si existe
    /// </summary>
    public string? LoadError => Source?.Error;

    #region Fields

    private ITexture? _texture;
    private bool _textureLoaded;
    private Task? _loadTask;

    #endregion

    #region Constructors

    public Image()
    {
        Width = 100;
        Height = 100;
    }

    public Image(ImageSource source)
    {
        Source = source;
        Width = 100;
        Height = 100;
    }

    public Image(string source)
    {
        Source = source; // Conversión implícita a ImageSource
        Width = 100;
        Height = 100;
    }

    #endregion


    #region Layout Overrides

    public override void Measure(float availableWidth, float availableHeight)
    {
        if (Width == 0 && Height == 0)
        {
            if (_texture != null)
            {
                Width = _texture.Width;
                Height = _texture.Height;
            }
            else
            {
                Width = 100;
                Height = 100;
            }
        }
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        // Iniciar carga asíncrona si es necesario
        if (!_textureLoaded && Source != null && _loadTask == null)
        {
            _loadTask = LoadTextureAsync(renderer);
        }

        // Si no hay textura, no renderizar nada
        if (_texture == null)
            return;

        // Calcular el área de renderizado según el modo de estiramiento
        float renderX = ComputedX;
        float renderY = ComputedY;
        float renderWidth = ComputedWidth;
        float renderHeight = ComputedHeight;

        switch (Stretch)
        {
            case StretchMode.None:
                renderWidth = _texture.Width;
                renderHeight = _texture.Height;
                renderX = ComputedX + (ComputedWidth - renderWidth) / 2;
                renderY = ComputedY + (ComputedHeight - renderHeight) / 2;
                break;

            case StretchMode.Fill:
                renderX = ComputedX;
                renderY = ComputedY;
                renderWidth = ComputedWidth;
                renderHeight = ComputedHeight;
                break;

            case StretchMode.Uniform:
                {
                    float scaleX = ComputedWidth / _texture.Width;
                    float scaleY = ComputedHeight / _texture.Height;
                    float scale = Math.Min(scaleX, scaleY);

                    renderWidth = _texture.Width * scale;
                    renderHeight = _texture.Height * scale;

                    renderX = ComputedX + (ComputedWidth - renderWidth) / 2;
                    renderY = ComputedY + (ComputedHeight - renderHeight) / 2;
                }
                break;

            case StretchMode.UniformToFill:
                {
                    float scaleX = ComputedWidth / _texture.Width;
                    float scaleY = ComputedHeight / _texture.Height;
                    float scale = Math.Max(scaleX, scaleY);

                    renderWidth = _texture.Width * scale;
                    renderHeight = _texture.Height * scale;

                    renderX = ComputedX + (ComputedWidth - renderWidth) / 2;
                    renderY = ComputedY + (ComputedHeight - renderHeight) / 2;
                }
                break;
        }

        bool hasRoundedCorners = BorderRadius.TopLeft > 0 || BorderRadius.TopRight > 0 ||
                                 BorderRadius.BottomLeft > 0 || BorderRadius.BottomRight > 0;

        if (hasRoundedCorners)
        {
            renderer.PushScissor(ComputedX, ComputedY, ComputedWidth, ComputedHeight);
        }

        renderer.DrawTexture(_texture, renderX, renderY, renderWidth, renderHeight, Tint);

        if (hasRoundedCorners)
        {
            renderer.PopScissor();
        }
    }

    #endregion

    #region Private Methods

    private async Task LoadTextureAsync(IRenderer renderer)
    {
        if (Source == null)
            return;

        try
        {
            var stream = await Source.GetStreamAsync();

            if (stream != null)
            {
                var cacheKey = Source.GetCacheKey();
                _texture = renderer.LoadTextureFromStream(stream, cacheKey);
            }
        }
        catch (Exception)
        {
            // Ignorar errores de carga
        }
        finally
        {
            _textureLoaded = true;
            MarkNeedsPaint();
        }
    }

    #endregion
}
