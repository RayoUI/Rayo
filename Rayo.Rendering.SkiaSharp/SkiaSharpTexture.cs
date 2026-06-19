using Rayo.Rendering;
using SkiaSharp;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

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
        _image = LoadImageFromStream(stream)
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
        _image = LoadImageFromStream(stream)
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

    private static SKImage? LoadImageFromStream(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var data = memory.ToArray();

        if (LooksLikeSvg(data))
        {
            var svg = Encoding.UTF8.GetString(data);
            return LoadSvgImage(svg);
        }

        using var skData = SKData.CreateCopy(data);
        return SKImage.FromEncodedData(skData);
    }

    private static bool LooksLikeSvg(byte[] data)
    {
        var length = Math.Min(data.Length, 256);
        var header = Encoding.UTF8.GetString(data, 0, length).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        return header.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
            || header.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
            && header.Contains("<svg", StringComparison.OrdinalIgnoreCase);
    }

    private static SKImage? LoadSvgImage(string svg)
    {
        var document = XDocument.Parse(svg);
        var root = document.Root;
        if (root == null)
            return null;

        var viewBox = ParseViewBox(root.Attribute("viewBox")?.Value);
        var width = ParseSvgLength(root.Attribute("width")?.Value) ?? viewBox.Width;
        var height = ParseSvgLength(root.Attribute("height")?.Value) ?? viewBox.Height;

        var pixelWidth = Math.Max(1, (int)MathF.Ceiling(width));
        var pixelHeight = Math.Max(1, (int)MathF.Ceiling(height));
        var info = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Rgba8888, SKAlphaType.Premul);

        using var surface = SKSurface.Create(info);
        if (surface == null)
            return null;

        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var scaleX = width / viewBox.Width;
        var scaleY = height / viewBox.Height;
        var matrix = SKMatrix.CreateScaleTranslation(scaleX, scaleY, -viewBox.Left * scaleX, -viewBox.Top * scaleY);
        canvas.Concat(in matrix);

        var inheritedFill = ParseColor(root.Attribute("fill")?.Value) ?? SKColors.Black;

        foreach (var pathElement in root.Descendants().Where(e => e.Name.LocalName.Equals("path", StringComparison.OrdinalIgnoreCase)))
        {
            var pathData = pathElement.Attribute("d")?.Value;
            if (string.IsNullOrWhiteSpace(pathData))
                continue;

            using var path = SKPath.ParseSvgPathData(pathData);
            if (path == null)
                continue;

            var fill = ParseColor(pathElement.Attribute("fill")?.Value) ?? inheritedFill;
            if (fill.Alpha > 0)
            {
                using var fillPaint = new SKPaint
                {
                    Color = fill,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };
                canvas.DrawPath(path, fillPaint);
            }

            var stroke = ParseColor(pathElement.Attribute("stroke")?.Value);
            if (stroke.HasValue && stroke.Value.Alpha > 0)
            {
                using var strokePaint = new SKPaint
                {
                    Color = stroke.Value,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = ParseSvgLength(pathElement.Attribute("stroke-width")?.Value) ?? 1f,
                    IsAntialias = true
                };
                canvas.DrawPath(path, strokePaint);
            }
        }

        canvas.Flush();
        return surface.Snapshot();
    }

    private static SKRect ParseViewBox(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new SKRect(0, 0, 24, 24);

        var parts = value
            .Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseFloat)
            .ToArray();

        if (parts.Length != 4 || parts[2] <= 0 || parts[3] <= 0)
            return new SKRect(0, 0, 24, 24);

        return new SKRect(parts[0], parts[1], parts[0] + parts[2], parts[1] + parts[3]);
    }

    private static float? ParseSvgLength(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var numeric = new string(value.Trim().TakeWhile(c => char.IsDigit(c) || c == '-' || c == '+' || c == '.').ToArray());
        return string.IsNullOrEmpty(numeric) ? null : ParseFloat(numeric);
    }

    private static float ParseFloat(string value) =>
        float.Parse(value, CultureInfo.InvariantCulture);

    private static SKColor? ParseColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();
        if (value.Equals("none", StringComparison.OrdinalIgnoreCase))
            return SKColors.Transparent;

        if (value.Equals("black", StringComparison.OrdinalIgnoreCase))
            return SKColors.Black;

        if (value.Equals("white", StringComparison.OrdinalIgnoreCase))
            return SKColors.White;

        if (value.StartsWith("#", StringComparison.Ordinal))
            return SKColor.TryParse(value, out var color) ? color : null;

        return null;
    }
}
