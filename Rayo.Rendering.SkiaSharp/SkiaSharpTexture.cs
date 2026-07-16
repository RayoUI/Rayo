using Rayo.Rendering;
using SkiaSharp;
using System.Text;
using Svg.Skia;

namespace Rayo.Rendering.SkiaSharp;

/// <summary>
/// Implementation of ITexture using SkiaSharp's image system
/// </summary>
public class SkiaSharpTexture : ITexture
{
    private SKImage? _image;
    private SKSvg? _svg;
    private SKSurface? _surface; // For render targets
    private bool _disposed;

    public int Width { get; }
    public int Height { get; }
    public bool IsRenderTarget { get; }
    public TextureSamplingMode SamplingMode { get; }

    internal SKImage? Image => _image;
    internal SKPicture? SvgPicture => _svg?.Picture;
    internal SKSurface? Surface => _surface;

    /// <summary>
    /// Creates a texture from an existing SKImage
    /// </summary>
    public SkiaSharpTexture(
        SKImage image,
        TextureSamplingMode samplingMode = TextureSamplingMode.Smooth)
    {
        _image = image ?? throw new ArgumentNullException(nameof(image));
        Width = image.Width;
        Height = image.Height;
        IsRenderTarget = false;
        SamplingMode = samplingMode;
    }

    /// <summary>
    /// Creates a texture from a file path
    /// </summary>
    public SkiaSharpTexture(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Texture file not found", filePath);

        using var stream = File.OpenRead(filePath);
        var loaded = LoadFromStream(stream)
            ?? throw new InvalidOperationException($"Failed to load texture from {filePath}");

        _image = loaded.Image;
        _svg = loaded.Svg;
        Width = loaded.Width;
        Height = loaded.Height;
        IsRenderTarget = false;
        SamplingMode = TextureSamplingMode.Smooth;
    }

    /// <summary>
    /// Creates a texture from a stream
    /// </summary>
    public SkiaSharpTexture(Stream stream)
    {
        var loaded = LoadFromStream(stream)
            ?? throw new InvalidOperationException("Failed to load texture from stream");

        _image = loaded.Image;
        _svg = loaded.Svg;
        Width = loaded.Width;
        Height = loaded.Height;
        IsRenderTarget = false;
        SamplingMode = TextureSamplingMode.Smooth;
    }

    /// <summary>
    /// Creates a render target texture (for render-to-texture operations)
    /// </summary>
    public SkiaSharpTexture(int width, int height)
        : this(width, height, null)
    {
    }

    internal SkiaSharpTexture(int width, int height, GRContext? grContext)
    {
        Width = width;
        Height = height;
        IsRenderTarget = true;
        SamplingMode = TextureSamplingMode.Smooth;

        var imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        _surface = grContext != null
            ? SKSurface.Create(grContext, false, imageInfo)
            : SKSurface.Create(imageInfo);
        _surface ??=
            SKSurface.Create(imageInfo);
        if (_surface == null)
        {
            throw new InvalidOperationException("Failed to create render target surface");
        }

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
        _svg?.Dispose();
        _surface?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    ~SkiaSharpTexture()
    {
        Dispose();
    }

    private static LoadedTexture? LoadFromStream(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var data = memory.ToArray();

        if (LooksLikeSvg(data))
        {
            var svg = Encoding.UTF8.GetString(data);
            return LoadSvg(svg);
        }

        using var skData = SKData.CreateCopy(data);
        var image = SKImage.FromEncodedData(skData);
        return image == null
            ? null
            : new LoadedTexture(image, null, image.Width, image.Height);
    }

    private static bool LooksLikeSvg(byte[] data)
    {
        var length = Math.Min(data.Length, 256);
        var header = Encoding.UTF8.GetString(data, 0, length).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        return header.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
            || header.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
            && header.Contains("<svg", StringComparison.OrdinalIgnoreCase);
    }

    private static LoadedTexture? LoadSvg(string svg)
    {
        var svgDocument = new SKSvg();
        try
        {
            var picture = svgDocument.FromSvg(svg);
            if (picture == null)
            {
                svgDocument.Dispose();
                return null;
            }

            var bounds = picture.CullRect;
            var width = Math.Max(1, (int)MathF.Ceiling(bounds.Width));
            var height = Math.Max(1, (int)MathF.Ceiling(bounds.Height));
            return new LoadedTexture(null, svgDocument, width, height);
        }
        catch
        {
            svgDocument.Dispose();
            throw;
        }
    }

    private sealed record LoadedTexture(SKImage? Image, SKSvg? Svg, int Width, int Height);
}
