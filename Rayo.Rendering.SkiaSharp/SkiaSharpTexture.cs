using Rayo.Rendering;
using SkiaSharp;

namespace Rayo.Rendering.SkiaSharp;

/// <summary>
/// Implementation of ITexture using SkiaSharp's image system
/// </summary>
public class SkiaSharpTexture : ITexture
{
    private SKImage? _image;
    private SKSurface? _surface; // For render targets
    private bool _disposed;

    public int Width { get; }
    public int Height { get; }
    public bool IsRenderTarget { get; }

    internal SKImage? Image => _image;
    internal SKSurface? Surface => _surface;

    /// <summary>
    /// Creates a texture from an existing SKImage
    /// </summary>
    public SkiaSharpTexture(SKImage image)
    {
        _image = image ?? throw new ArgumentNullException(nameof(image));
        Width = image.Width;
        Height = image.Height;
        IsRenderTarget = false;
    }

    /// <summary>
    /// Creates a texture from a file path
    /// </summary>
    public SkiaSharpTexture(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Texture file not found", filePath);

        using var stream = File.OpenRead(filePath);
        _image = SKImage.FromEncodedData(stream)
            ?? throw new InvalidOperationException($"Failed to load texture from {filePath}");

        Width = _image.Width;
        Height = _image.Height;
        IsRenderTarget = false;
    }

    /// <summary>
    /// Creates a texture from a stream
    /// </summary>
    public SkiaSharpTexture(Stream stream)
    {
        _image = SKImage.FromEncodedData(stream)
            ?? throw new InvalidOperationException("Failed to load texture from stream");

        Width = _image.Width;
        Height = _image.Height;
        IsRenderTarget = false;
    }

    /// <summary>
    /// Creates a render target texture (for render-to-texture operations)
    /// </summary>
    public SkiaSharpTexture(int width, int height)
    {
        Width = width;
        Height = height;
        IsRenderTarget = true;

        var imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        _surface = SKSurface.Create(imageInfo)
            ?? throw new InvalidOperationException("Failed to create render target surface");

        // Create initial image snapshot
        _image = _surface.Snapshot();
    }

    /// <summary>
    /// Updates the image snapshot for render targets
    /// </summary>
    internal void UpdateSnapshot()
    {
        if (!IsRenderTarget || _surface == null)
            return;

        _image?.Dispose();
        _image = _surface.Snapshot();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _image?.Dispose();
        _surface?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    ~SkiaSharpTexture()
    {
        Dispose();
    }
}
