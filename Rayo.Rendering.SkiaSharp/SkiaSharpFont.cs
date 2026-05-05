using Rayo.Rendering;
using SkiaSharp;

namespace Rayo.Rendering.SkiaSharp;

/// <summary>
/// Implementation of IFont using SkiaSharp's font system
/// </summary>
public class SkiaSharpFont : IFont
{
    private readonly SKTypeface _typeface;
    private readonly SKFont _font;
    private bool _disposed;

    public string Name { get; }
    public float Size { get; }

    internal SKTypeface Typeface => _typeface;
    internal SKFont Font => _font;

    public SkiaSharpFont(byte[] fontData, float fontSize, string name = "Default")
    {
        Name = name;
        Size = fontSize;

        // Load typeface from font data using SKData for safety
        // SKTypeface.FromStream with MemoryStream can be unsafe if the stream is disposed
        // before Skia reads from it (depending on platform implementation)
        using var data = SKData.CreateCopy(fontData);
        _typeface = SKTypeface.FromData(data)
            ?? throw new InvalidOperationException("Failed to load font from data");

        // Create SKFont with the specified size
        _font = new SKFont(_typeface, fontSize);
        _font.Subpixel = true;
        _font.LinearMetrics = true;
    }

    public SkiaSharpFont(string fontName, float fontSize)
    {
        Name = fontName;
        Size = fontSize;

        // Load typeface from system fonts
        _typeface = SKTypeface.FromFamilyName(fontName, SKFontStyle.Normal)
            ?? SKTypeface.Default;

        _font = new SKFont(_typeface, fontSize);
        _font.Subpixel = true;
        _font.LinearMetrics = true;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _font?.Dispose();
        _typeface?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    ~SkiaSharpFont()
    {
        Dispose();
    }
}
