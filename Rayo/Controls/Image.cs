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

/// <summary>Observable stages of the image-to-texture pipeline.</summary>
public enum ImageTextureState
{
    Empty,
    LoadingSource,
    SourceReady,
    Preparing,
    Prepared,
    Uploaded,
    Failed
}

/// <summary>
/// Componente para mostrar imágenes desde archivos locales, URLs de red o streams
/// Migrated to new MAUI-like architecture: inherits from View<Image>
/// </summary>
public class Image : BorderView<Image>
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
                ResetSourcePipeline();
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
    public bool IsLoading => TextureState is ImageTextureState.LoadingSource or ImageTextureState.Preparing;

    /// <summary>
    /// Indica si la imagen se cargó correctamente
    /// </summary>
    public bool IsLoaded => TextureState == ImageTextureState.Uploaded;

    /// <summary>
    /// Error de carga si existe
    /// </summary>
    public string? LoadError => _textureError ?? Source?.Error;

    /// <summary>Current stage of source acquisition, decoding, upload, and drawing readiness.</summary>
    public ImageTextureState TextureState { get; private set; } = ImageTextureState.Empty;

    #region Fields

    private ITexture? _texture;
    private byte[]? _encodedImage;
    private string? _cacheKey;
    private IPreparedTexture? _preparedTexture;
    private Task? _sourceLoadTask;
    private Task? _preparationTask;
    private CancellationTokenSource? _preparationCancellation;
    private string? _textureError;
    private int _sourceGeneration;

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

    protected override void OnMounted()
    {
        base.OnMounted();
        if (TextureState is ImageTextureState.SourceReady or ImageTextureState.Prepared)
            MarkNeedsPaint();
    }

    protected override void OnUnmounted()
    {
        _preparationCancellation?.Cancel();
        _preparationCancellation?.Dispose();
        _preparationCancellation = null;
        _preparationTask = null;
        if (TextureState == ImageTextureState.Preparing)
            TextureState = _encodedImage != null
                ? ImageTextureState.SourceReady
                : ImageTextureState.Empty;
        base.OnUnmounted();
    }

    protected override void Measure(float availableWidth, float availableHeight)
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

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        AdvanceTexturePipeline(renderer);

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

        bool exceedsBounds = renderX < ComputedX || renderY < ComputedY ||
                             renderX + renderWidth > ComputedX + ComputedWidth ||
                             renderY + renderHeight > ComputedY + ComputedHeight;

        // StretchMode.None and UniformToFill can draw beyond the arranged bounds.
        // Clip whenever that happens so an Image never paints outside its container.
        if (exceedsBounds)
        {
            renderer.PushScissor(ComputedX, ComputedY, ComputedWidth, ComputedHeight);
        }

        renderer.DrawTexture(_texture, renderX, renderY, renderWidth, renderHeight, Tint);

        if (exceedsBounds)
        {
            renderer.PopScissor();
        }
    }

    #endregion

    #region Private Methods

    private void ResetSourcePipeline()
    {
        var generation = ++_sourceGeneration;
        _preparationCancellation?.Cancel();
        _preparationCancellation?.Dispose();
        _preparationCancellation = null;
        _sourceLoadTask = null;
        _preparationTask = null;
        _preparedTexture = null;
        _encodedImage = null;
        _cacheKey = null;
        _texture = null;
        _textureError = null;

        var source = Source;
        if (source == null)
        {
            TextureState = ImageTextureState.Empty;
            MarkNeedsPaint();
            return;
        }

        TextureState = ImageTextureState.LoadingSource;
        _sourceLoadTask = LoadSourceAsync(source, generation);
    }

    private async Task LoadSourceAsync(ImageSource source, int generation)
    {
        try
        {
            await using var stream = await source.GetStreamAsync().ConfigureAwait(false);
            if (stream == null)
            {
                CompleteSourceLoad(generation, null, null, source.Error ?? "Image source returned no data.");
                return;
            }

            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory).ConfigureAwait(false);
            CompleteSourceLoad(generation, memory.ToArray(), source.GetCacheKey(), null);
        }
        catch (Exception exception)
        {
            CompleteSourceLoad(generation, null, null, exception.Message);
        }
    }

    private void CompleteSourceLoad(
        int generation,
        byte[]? encodedImage,
        string? cacheKey,
        string? error)
    {
        RunOnUiThread(() =>
        {
            if (generation != _sourceGeneration)
                return;

            _sourceLoadTask = null;
            _encodedImage = encodedImage;
            _cacheKey = cacheKey;
            _textureError = error;
            TextureState = error == null && encodedImage is { Length: > 0 } && !string.IsNullOrEmpty(cacheKey)
                ? ImageTextureState.SourceReady
                : ImageTextureState.Failed;
            MarkNeedsPaint();
        });
    }

    private void AdvanceTexturePipeline(IRenderer renderer)
    {
        if (TextureState == ImageTextureState.Prepared &&
            _preparedTexture != null &&
            renderer is ITexturePreparationRenderer preparationRenderer)
        {
            try
            {
                _texture = preparationRenderer.UploadPreparedTexture(_preparedTexture);
                TextureState = _texture != null
                    ? ImageTextureState.Uploaded
                    : ImageTextureState.Failed;
                if (_texture == null)
                    _textureError = "The renderer could not upload the prepared image.";
            }
            catch (Exception exception)
            {
                _textureError = exception.Message;
                TextureState = ImageTextureState.Failed;
            }
            finally
            {
                _preparedTexture = null;
                _encodedImage = null;
            }
            return;
        }

        if (TextureState != ImageTextureState.SourceReady ||
            _encodedImage == null ||
            string.IsNullOrEmpty(_cacheKey))
        {
            return;
        }

        if (renderer is ITexturePreparationRenderer asyncRenderer)
        {
            var cachedTexture = asyncRenderer.TryGetLoadedTexture(_cacheKey);
            if (cachedTexture != null)
            {
                _texture = cachedTexture;
                _encodedImage = null;
                TextureState = ImageTextureState.Uploaded;
                return;
            }

            TextureState = ImageTextureState.Preparing;
            _preparationCancellation = new CancellationTokenSource();
            _preparationTask = PrepareTextureAsync(
                asyncRenderer,
                _encodedImage,
                _cacheKey,
                _sourceGeneration,
                _preparationCancellation.Token);
            return;
        }

        try
        {
            using var stream = new MemoryStream(_encodedImage, writable: false);
            _texture = renderer.LoadTextureFromStream(stream, _cacheKey);
            TextureState = _texture != null
                ? ImageTextureState.Uploaded
                : ImageTextureState.Failed;
            if (_texture == null)
                _textureError = "The renderer could not load the image.";
        }
        catch (Exception exception)
        {
            _textureError = exception.Message;
            TextureState = ImageTextureState.Failed;
        }
        finally
        {
            _encodedImage = null;
        }
    }

    private async Task PrepareTextureAsync(
        ITexturePreparationRenderer renderer,
        byte[] encodedImage,
        string cacheKey,
        int generation,
        CancellationToken cancellationToken)
    {
        try
        {
            var prepared = await renderer.PrepareTextureAsync(
                encodedImage,
                cacheKey,
                cancellationToken).ConfigureAwait(false);
            RunOnUiThread(() =>
            {
                if (generation != _sourceGeneration || cancellationToken.IsCancellationRequested)
                    return;

                _preparationTask = null;
                _preparationCancellation?.Dispose();
                _preparationCancellation = null;
                _preparedTexture = prepared;
                TextureState = prepared != null
                    ? ImageTextureState.Prepared
                    : ImageTextureState.Failed;
                if (prepared == null)
                    _textureError = "The renderer could not decode the image.";
                MarkNeedsPaint();
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            RunOnUiThread(() =>
            {
                if (generation != _sourceGeneration)
                    return;

                _preparationTask = null;
                _preparationCancellation?.Dispose();
                _preparationCancellation = null;
                _textureError = exception.Message;
                TextureState = ImageTextureState.Failed;
                MarkNeedsPaint();
            });
        }
    }

    private static void RunOnUiThread(Action action)
    {
        var application = UIApplication.Current;
        if (application != null)
            application.RunOnMainThread(action);
        else
            action();
    }

    #endregion
}
