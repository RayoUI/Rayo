using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Rayo.Rendering;
using Rayo.Rendering.Graphics.VectorGraphics;
using SkiaSharp;

namespace Rayo.Rendering.SkiaSharp;

/// <summary>
/// Implementation of IRenderer using SkiaSharp (CPU-backed for cross-platform compatibility).
/// Automatically applies DPI scaling for consistent cross-platform rendering.
/// Also implements INativeGradientRenderer for high-quality gradient rendering.
/// </summary>
public class SkiaSharpRenderer : IRenderer, INativeGradientRenderer
{
    private SKSurface? _surface;
    private SKCanvas? _canvas;
    private int _width;
    private int _height;
    private bool _disposed;

    // Default font
    private SkiaSharpFont? _defaultFont;
    private readonly Dictionary<string, SkiaSharpTexture> _textureCache = new();

    // Render-to-texture state
    private readonly Stack<(SKCanvas canvas, SkiaSharpTexture target)> _renderTargetStack = new();
    private SKCanvas? _originalCanvas;

    // Scissor/clipping stack
    private readonly Stack<SKRect> _scissorStack = new();
    private readonly Stack<int> _roundedClipStack = new();
    private readonly Stack<int> _transformStack = new();

    // Cache for fallback fonts to avoid repeated lookups
    private readonly Dictionary<int, SKTypeface> _fallbackFontCache = new();

    // DPI scale factor (set externally)
    private static float _dpiScaleFactor = 1.0f;

    public bool IsRenderingToTexture => _renderTargetStack.Count > 0;

    /// <summary>
    /// Sets the DPI scale factor for this renderer.
    /// Should be called during initialization based on platform density.
    /// </summary>
    public static void SetDpiScaleFactor(float scaleFactor)
    {
        _dpiScaleFactor = Math.Max(1.0f, scaleFactor);
    }

    /// <summary>
    /// Gets the current DPI scale factor.
    /// </summary>
    public static float GetDpiScaleFactor() => _dpiScaleFactor;

    /// <summary>
    /// Creates a SkiaSharp renderer
    /// </summary>
    public SkiaSharpRenderer()
    {
    }

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;

        // Create CPU-backed surface for maximum compatibility
        var imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        _surface = SKSurface.Create(imageInfo)
            ?? throw new InvalidOperationException("Failed to create SkiaSharp surface");


        _canvas = _surface.Canvas;

        // ? Apply automatic DPI scaling for cross-platform consistency
        // Desktop typically runs at 1.0x (96 DPI), Android at 2.0x-3.0x (320-480 DPI)
        // This makes UI elements appear the same physical size on all screens
        if (_dpiScaleFactor > 1.0f)
        {
            _canvas.Scale(_dpiScaleFactor, _dpiScaleFactor);
        }

        // Load default font
        LoadDefaultFont();
    }

    // Named emoji font typefaces (tried before SKFontManager.MatchCharacter).
    // This ensures consistent emoji rendering on platforms where MatchCharacter
    // might return a text-style fallback rather than the preferred emoji font.
    private static readonly string[] EmojiTypefaceNames =
    [
        "Segoe UI Emoji",    // Windows 10+
        "Segoe UI Symbol",   // Windows 8.1 fallback
        "Apple Color Emoji", // macOS / iOS
        "Noto Color Emoji",  // Android / Linux
        "Noto Emoji",        // Linux (monochrome variant)
        "EmojiOne Mozilla",  // Firefox / some Linux distros
        "Twemoji Mozilla",   // Alternative emoji font
    ];

    // Pre-loaded emoji typefaces so we avoid repeated name lookups per glyph.
    private readonly List<SKTypeface> _emojiTypefaces = new();

    private void LoadDefaultFont()
    {
        // Prefer Segoe UI on Windows for its broad Unicode support and clean rendering.
        // Fall back through common cross-platform fonts until one is found.
        string[] textFontNames =
        [
            "Segoe UI",
            "Arial",
            "Helvetica",
            "DejaVu Sans",
            "Liberation Sans",
        ];

        foreach (var fontName in textFontNames)
        {
            try
            {
                var candidate = new SkiaSharpFont(fontName, 24);
                // Verify the typeface resolved to a real font (not the default placeholder).
                if (candidate.Typeface.FamilyName.Equals(fontName, StringComparison.OrdinalIgnoreCase) ||
                    !candidate.Typeface.Equals(SKTypeface.Default))
                {
                    _defaultFont = candidate;
                    break;
                }
                candidate.Dispose();
            }
            catch
            {
                // Try next candidate.
            }
        }

        // Ultimate fallback — use whatever SkiaSharp considers the platform default.
        _defaultFont ??= new SkiaSharpFont("Default", 24);

        // Pre-load known emoji typefaces for fast glyph resolution.
        LoadEmojiTypefaces();
    }

    private void LoadEmojiTypefaces()
    {
        foreach (var name in EmojiTypefaceNames)
        {
            try
            {
                var tf = SKTypeface.FromFamilyName(name, SKFontStyle.Normal);
                // Only keep it when it really resolved to the requested family.
                if (tf != null && tf.FamilyName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    _emojiTypefaces.Add(tf);
                }
                else
                {
                    tf?.Dispose();
                }
            }
            catch
            {
                // Not available on this platform.
            }
        }
    }

    public void Resize(int width, int height)
    {
        if (_width == width && _height == height)
            return;

        _width = width;
        _height = height;

        // Dispose old surface
        _surface?.Dispose();

        // Recreate CPU-backed surface
        var imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        _surface = SKSurface.Create(imageInfo)
            ?? throw new InvalidOperationException("Failed to recreate SkiaSharp surface");

        _canvas = _surface.Canvas;

        // ? Reapply DPI scaling after resize
        if (_dpiScaleFactor > 1.0f)
        {
            _canvas.Scale(_dpiScaleFactor, _dpiScaleFactor);
        }
    }

    public void BeginFrame()
    {
        if (_canvas == null)
            throw new InvalidOperationException("Renderer not initialized");

        _transformStack.Clear();
        _canvas.Save();
    }

    public void EndFrame()
    {
        if (_canvas == null)
            throw new InvalidOperationException("Renderer not initialized");

        _canvas.Restore();
        _canvas.Flush();
    }

    public void Clear(Color color)
    {
        if (_canvas == null)
            throw new InvalidOperationException("Renderer not initialized");

        _canvas.Clear(ToSKColor(color));
    }

    // === Primitive Drawing Methods ===

    public void DrawRect(float x, float y, float width, float height, Color color)
    {
        if (_canvas == null) return;

        using var paint = new SKPaint
        {
            Color = ToSKColor(color),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        _canvas.DrawRect(x, y, width, height, paint);
    }

    public void DrawRoundedRect(float x, float y, float width, float height, float radius, Color color)
    {
        if (_canvas == null) return;

        using var paint = new SKPaint
        {
            Color = ToSKColor(color),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        var rect = new SKRect(x, y, x + width, y + height);
        _canvas.DrawRoundRect(rect, radius, radius, paint);
    }

    public void DrawRect(float x, float y, float width, float height, Brushes.Brush brush)
    {
        if (brush == null) return;

        if (brush.IsGradient)
        {
            Brushes.BrushRendererExtensions.DrawRect(this, x, y, width, height, brush);
            return;
        }

        var color = brush.PrimaryColor;
        var finalColor = new Color(color.R, color.G, color.B, color.A * brush.Opacity);
        DrawRect(x, y, width, height, finalColor);
    }

    public void DrawRoundedRect(float x, float y, float width, float height, float radius, Brushes.Brush brush)
    {
        if (brush == null) return;

        if (brush.IsGradient)
        {
            Brushes.BrushRendererExtensions.DrawRoundedRect(this, x, y, width, height, radius, brush);
            return;
        }

        var color = brush.PrimaryColor;
        var finalColor = new Color(color.R, color.G, color.B, color.A * brush.Opacity);
        DrawRoundedRect(x, y, width, height, radius, finalColor);
    }

    public void DrawRectOutline(float x, float y, float width, float height, float thickness, Color color)
    {
        if (_canvas == null) return;

        // FIX: Add small overlap to prevent seams with background
        float overlap = 0.5f;
        float effectiveThickness = thickness + overlap;

        using var paint = new SKPaint
        {
            Color = ToSKColor(color),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = effectiveThickness,
            IsAntialias = true
        };

        // FIX: Inset rect so the stroke is drawn inside the bounds
        float halfThickness = effectiveThickness / 2f;
        var rect = new SKRect(
            x + halfThickness,
            y + halfThickness,
            x + width - halfThickness,
            y + height - halfThickness
        );

        _canvas.DrawRect(rect, paint);
    }

    public void DrawRoundedRectOutline(float x, float y, float width, float height, float radius, float thickness, Color color)
    {
        if (_canvas == null) return;

        // FIX: Add small overlap to prevent seams with background
        float overlap = 0.5f;
        float effectiveThickness = thickness + overlap;

        using var paint = new SKPaint
        {
            Color = ToSKColor(color),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = effectiveThickness,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,   // Smooth corners on the stroke
            StrokeJoin = SKStrokeJoin.Round  // Smooth joins between edges
        };

        // Adjust rectangle to center the stroke within the bounds
        // SKPaint.Stroke centers the stroke on the path, so we need to inset by half the thickness
        float halfThickness = effectiveThickness / 2f;
        var rect = new SKRect(
            x + halfThickness,
            y + halfThickness,
            x + width - halfThickness,
            y + height - halfThickness
        );

        // Adjust radius to account for the stroke inset
        // If the radius is smaller than halfThickness, clamp it
        float adjustedRadius = Math.Max(0, radius - halfThickness);

        _canvas.DrawRoundRect(rect, adjustedRadius, adjustedRadius, paint);
    }

    public void DrawLine(float x1, float y1, float x2, float y2, float thickness, Color color)
    {
        if (_canvas == null) return;

        using var paint = new SKPaint
        {
            Color = ToSKColor(color),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = thickness,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round
        };

        _canvas.DrawLine(x1, y1, x2, y2, paint);
    }

    public void DrawCircle(float cx, float cy, float radius, Color color)
    {
        if (_canvas == null) return;

        using var paint = new SKPaint
        {
            Color = ToSKColor(color),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        _canvas.DrawCircle(cx, cy, radius, paint);
    }

    public void DrawCircleOutline(float cx, float cy, float radius, float thickness, Color color)
    {
        if (_canvas == null) return;

        using var paint = new SKPaint
        {
            Color = ToSKColor(color),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = thickness,
            IsAntialias = true
        };

        _canvas.DrawCircle(cx, cy, radius, paint);
    }

    public void DrawRoundedRectOutline(float x, float y, float width, float height, float radius, float thickness, Brushes.Brush brush)
        => DrawRoundedRectOutline(x, y, width, height, radius, thickness, brush.PrimaryColor);

    public void DrawLine(float x1, float y1, float x2, float y2, float thickness, Brushes.Brush brush)
        => DrawLine(x1, y1, x2, y2, thickness, brush.PrimaryColor);

    public void DrawCircle(float cx, float cy, float radius, Brushes.Brush brush)
    {
        if (brush.IsGradient)
            Brushes.BrushRendererExtensions.DrawCircle(this, cx, cy, radius, brush);
        else
            DrawCircle(cx, cy, radius, brush.PrimaryColor);
    }

    public void DrawCircleOutline(float cx, float cy, float radius, float thickness, Brushes.Brush brush)
        => DrawCircleOutline(cx, cy, radius, thickness, brush.PrimaryColor);

    public void DrawPolygon(List<(float x, float y)> points, Color color)
    {
        if (_canvas == null || points.Count < 3) return;

        using var path = new SKPath();
        path.MoveTo(points[0].x, points[0].y);

        for (int i = 1; i < points.Count; i++)
        {
            path.LineTo(points[i].x, points[i].y);
        }

        path.Close();

        using var paint = new SKPaint
        {
            Color = ToSKColor(color),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        _canvas.DrawPath(path, paint);
    }

    // === Vector Path Methods ===

    public void DrawPath(VectorPath path, Color fillColor)
    {
        if (_canvas == null) return;

        using var skPath = ConvertToSKPath(path);
        using var paint = new SKPaint
        {
            Color = ToSKColor(fillColor),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        _canvas.DrawPath(skPath, paint);
    }

    public void DrawPathStroke(VectorPath path, Color strokeColor, float strokeWidth)
    {
        if (_canvas == null) return;

        using var skPath = ConvertToSKPath(path);
        using var paint = new SKPaint
        {
            Color = ToSKColor(strokeColor),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth,
            IsAntialias = true
        };

        _canvas.DrawPath(skPath, paint);
    }

    public void DrawPathFillAndStroke(VectorPath path, Color fillColor, Color strokeColor, float strokeWidth)
    {
        if (_canvas == null) return;

        using var skPath = ConvertToSKPath(path);

        // Draw fill first
        using (var fillPaint = new SKPaint
        {
            Color = ToSKColor(fillColor),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        })
        {
            _canvas.DrawPath(skPath, fillPaint);
        }

        // Then draw stroke
        using (var strokePaint = new SKPaint
        {
            Color = ToSKColor(strokeColor),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth,
            IsAntialias = true
        })
        {
            _canvas.DrawPath(skPath, strokePaint);
        }
    }

    public void DrawPath(VectorPath path, Brushes.Brush fillColor)
        => DrawPath(path, fillColor.PrimaryColor);

    public void DrawPathStroke(VectorPath path, Brushes.Brush strokeColor, float strokeWidth)
        => DrawPathStroke(path, strokeColor.PrimaryColor, strokeWidth);

    public void DrawPathFillAndStroke(VectorPath path, Brushes.Brush fillColor, Brushes.Brush strokeColor, float strokeWidth)
        => DrawPathFillAndStroke(path, fillColor.PrimaryColor, strokeColor.PrimaryColor, strokeWidth);

    public void DrawQuadraticBezier(float startX, float startY, float controlX, float controlY,
        float endX, float endY, Color color, float thickness = 2f)
    {
        if (_canvas == null) return;

        using var path = new SKPath();
        path.MoveTo(startX, startY);
        path.QuadTo(controlX, controlY, endX, endY);

        using var paint = new SKPaint
        {
            Color = ToSKColor(color),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = thickness,
            IsAntialias = true
        };

        _canvas.DrawPath(path, paint);
    }

    public void DrawCubicBezier(float startX, float startY, float cp1X, float cp1Y,
        float cp2X, float cp2Y, float endX, float endY, Color color, float thickness = 2f)
    {
        if (_canvas == null) return;

        using var path = new SKPath();
        path.MoveTo(startX, startY);
        path.CubicTo(cp1X, cp1Y, cp2X, cp2Y, endX, endY);

        using var paint = new SKPaint
        {
            Color = ToSKColor(color),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = thickness,
            IsAntialias = true
        };

        _canvas.DrawPath(path, paint);
    }

    // === Text Methods ===

    public IFont LoadFont(byte[] fontData, float fontSize)
    {
        return new SkiaSharpFont(fontData, fontSize);
    }

    public void DrawText(string text, float x, float y, Color color, float fontSize = 24)
    {
        if (_canvas == null || string.IsNullOrEmpty(text)) return;

        var font = _defaultFont;
        if (font != null && Math.Abs(font.Size - fontSize) > 0.1f)
        {
            // Create temporary font with correct size
            using var tempFont = new SkiaSharpFont(font.Name, fontSize);
            DrawTextWithFont(text, x, y, color, tempFont, fontSize);
        }
        else if (font != null)
        {
            DrawTextWithFont(text, x, y, color, font, fontSize);
        }
    }

    public void DrawTextStyled(string text, float x, float y, Rendering.Brushes.Brush color, float fontSize, bool isBold, bool isItalic)
    {
        if (_canvas == null || string.IsNullOrEmpty(text)) return;

        var baseFont = _defaultFont;
        if (baseFont == null) return;

        // Build a styled typeface (bold/italic variants when available in the font family).
        var style = (isBold, isItalic) switch
        {
            (true,  true)  => SKFontStyle.BoldItalic,
            (true,  false) => SKFontStyle.Bold,
            (false, true)  => SKFontStyle.Italic,
            _              => SKFontStyle.Normal,
        };

        // Try to get a real bold/italic variant; fall back to the regular typeface with
        // synthetic Embolden / SkewX if no variant is found in the family.
        var styledTypeface = SKTypeface.FromFamilyName(baseFont.Typeface.FamilyName, style)
                          ?? baseFont.Typeface;

        using var renderFont = new SKFont(styledTypeface, fontSize);
        renderFont.Subpixel = true;
        // Apply synthetic bold/italic only when the resolved typeface does not already match.
        renderFont.Embolden = isBold  && styledTypeface.IsBold  == false;
        renderFont.SkewX    = isItalic && styledTypeface.IsItalic == false ? -0.25f : 0f;

        using var paint = new SKPaint
        {
            Color       = ToSKColor(color.PrimaryColor),
            IsAntialias = true
        };

        float baselineY = y - renderFont.Metrics.Ascent;

        // Use the same grapheme-cluster fallback path as DrawTextWithFont so that
        // emoji and other missing glyphs are resolved from fallback fonts.
        bool hasMissingGlyphs = !renderFont.ContainsGlyphs(text) || RequiresEmojiFallback(text);

        if (!hasMissingGlyphs)
        {
            var bounds = new SKRect();
            renderFont.MeasureText(text, out bounds, paint);
            float adjustedX = x - bounds.Left;
            _canvas.DrawText(text, adjustedX, baselineY, renderFont, paint);
        }
        else
        {
            // Measure full run to get left-edge offset.
            float measureX = 0;
            float minLeft  = float.MaxValue;

            for (int i = 0; i < text.Length; i++)
            {
                string element    = ExtractTextElement(text, ref i);
                if (element.Length == 0) continue;

                string renderText = NormalizeTextElement(element, out bool needsEmoji);
                if (string.IsNullOrEmpty(renderText)) continue;

                var tf = ResolveTypefaceForTextElement(styledTypeface, renderText, needsEmoji, fontSize);
                using var cf = new SKFont(tf, fontSize) { Subpixel = true };

                var b = new SKRect();
                cf.MeasureText(renderText, out b, paint);
                minLeft = Math.Min(minLeft, measureX + b.Left);
                measureX += cf.MeasureText(renderText);
            }

            float offsetX  = minLeft == float.MaxValue ? 0f : -minLeft;
            float currentX = x + offsetX;

            for (int i = 0; i < text.Length; i++)
            {
                string element    = ExtractTextElement(text, ref i);
                if (element.Length == 0) continue;

                string renderText = NormalizeTextElement(element, out bool needsEmoji);
                if (string.IsNullOrEmpty(renderText)) continue;

                var tf = ResolveTypefaceForTextElement(styledTypeface, renderText, needsEmoji, fontSize);
                using var cf = new SKFont(tf, fontSize) { Subpixel = true };
                cf.Embolden = renderFont.Embolden;
                cf.SkewX    = renderFont.SkewX;

                // Per-glyph baseline so emoji with a different ascent sits on the same line.
                float glyphBaselineY = y - cf.Metrics.Ascent;

                _canvas.DrawText(renderText, currentX, glyphBaselineY, cf, paint);
                currentX += cf.MeasureText(renderText);
            }
        }
    }

    public void DrawTextWithFont(string text, float x, float y, Color color, IFont font, float fontSize = 24)
    {
        if (_canvas == null || string.IsNullOrEmpty(text)) return;

        if (font is not SkiaSharpFont skFont)
            throw new ArgumentException("Font must be a SkiaSharpFont", nameof(font));

        // Create a font at the requested size (may differ from cached font size)
        using var renderFont = new SKFont(skFont.Typeface, fontSize);
        renderFont.Subpixel = true;
        renderFont.Embolden = font.IsBold;
        renderFont.SkewX    = font.IsItalic ? -0.25f : 0f;

        using var paint = new SKPaint
        {
            Color = ToSKColor(color),
            IsAntialias = true
        };

        // baseline Y for the primary font — used for all-same-font fast path
        var metrics = renderFont.Metrics;
        float baselineY = y - metrics.Ascent;

        // Force the per-glyph fallback path when the text contains emoji so they are
        // always routed through the emoji typeface (color rendering) rather than the
        // base monochrome font which may technically contain a text-style glyph.
        bool hasMissingGlyphs = !renderFont.ContainsGlyphs(text) || RequiresEmojiFallback(text);

        if (!hasMissingGlyphs)
        {
            var bounds = new SKRect();
            renderFont.MeasureText(text, out bounds, paint);
            float adjustedX = x - bounds.Left;

            _canvas.DrawText(text, adjustedX, baselineY, renderFont, paint);
        }
        else
        {
            // Draw by grapheme cluster with per-glyph baseline so emoji from a different
            // typeface (with a different ascent) still sit on the same visual baseline.
            float measureX = 0;
            float minX = float.MaxValue;

            for (int i = 0; i < text.Length; i++)
            {
                string textElement = ExtractTextElement(text, ref i);
                if (textElement.Length == 0)
                    continue;

                string renderText = NormalizeTextElement(textElement, out bool requiresEmojiPresentation);
                if (string.IsNullOrEmpty(renderText))
                    continue;

                var typefaceToUse = ResolveTypefaceForTextElement(skFont.Typeface, renderText, requiresEmojiPresentation, fontSize);

                using var charFont = new SKFont(typefaceToUse, fontSize);
                charFont.Subpixel = true;

                var bounds = new SKRect();
                charFont.MeasureText(renderText, out bounds, paint);
                float left = measureX + bounds.Left;
                minX = Math.Min(minX, left);
                measureX += charFont.MeasureText(renderText);
            }

            float offsetX = minX == float.MaxValue ? 0 : -minX;
            float currentX = x + offsetX;

            for (int i = 0; i < text.Length; i++)
            {
                string textElement = ExtractTextElement(text, ref i);
                if (textElement.Length == 0)
                    continue;

                string renderText = NormalizeTextElement(textElement, out bool requiresEmojiPresentation);
                if (string.IsNullOrEmpty(renderText))
                    continue;

                var typefaceToUse = ResolveTypefaceForTextElement(skFont.Typeface, renderText, requiresEmojiPresentation, fontSize);

                using var charFont = new SKFont(typefaceToUse, fontSize);
                charFont.Subpixel = true;

                // Use each glyph's own ascent so cross-font baselines stay aligned.
                float glyphBaselineY = y - charFont.Metrics.Ascent;

                _canvas.DrawText(renderText, currentX, glyphBaselineY, charFont, paint);
                currentX += charFont.MeasureText(renderText);
            }
        }
    }

    public Vector2 MeasureText(string text, float fontSize = 24)
    {
        if (string.IsNullOrEmpty(text))
            return Vector2.Zero;

        if (_defaultFont != null)
        {
            return MeasureTextWithFont(text, _defaultFont, fontSize);
        }

        // Use default font or create temporary one
        using var tempFont = new SkiaSharpFont("Arial", fontSize);
        return MeasureTextWithFont(text, tempFont, fontSize);
    }

    public Vector2 MeasureTextWithFont(string text, IFont font, float fontSize = 24)
    {
        if (string.IsNullOrEmpty(text) || font == null)
            return Vector2.Zero;

        if (font is not SkiaSharpFont skFont)
            return MeasureText(text, fontSize); // Fallback

        // Create a font at the requested size for accurate measurement
        using var measureFont = new SKFont(skFont.Typeface, fontSize);
        
        // Check for missing glyphs using the measure font
        bool hasMissingGlyphs = !measureFont.ContainsGlyphs(text) || RequiresEmojiFallback(text);

        float width = 0;
        float height = 0;

        if (!hasMissingGlyphs)
        {
            // Use SKPaint.MeasureText with bounds for accurate visual width
            using var paint = new SKPaint { IsAntialias = true };
            var bounds = new SKRect();
            measureFont.MeasureText(text, out bounds, paint);

            width = Math.Max(0, bounds.Right - bounds.Left);

            var metrics = measureFont.Metrics;
            height = metrics.Descent - metrics.Ascent;
        }
        else
        {
            // Measure by grapheme cluster with fallback
            using var paint = new SKPaint { IsAntialias = true };
            float currentX = 0;
            float minX = float.MaxValue;
            float maxX = float.MinValue;

            for (int i = 0; i < text.Length; i++)
            {
                string textElement = ExtractTextElement(text, ref i);
                if (textElement.Length == 0)
                    continue;

                string renderText = NormalizeTextElement(textElement, out bool requiresEmojiPresentation);
                if (string.IsNullOrEmpty(renderText))
                    continue;

                var typefaceToUse = ResolveTypefaceForTextElement(skFont.Typeface, renderText, requiresEmojiPresentation, fontSize);

                using var charFont = new SKFont(typefaceToUse, fontSize);

                var bounds = new SKRect();
                charFont.MeasureText(renderText, out bounds, paint);
                float left = currentX + bounds.Left;
                float right = currentX + bounds.Right;

                minX = Math.Min(minX, left);
                maxX = Math.Max(maxX, right);

                currentX += charFont.MeasureText(renderText);

                var metrics = charFont.Metrics;
                float charHeight = metrics.Descent - metrics.Ascent;
                height = Math.Max(height, charHeight);
            }

            width = minX == float.MaxValue ? 0 : Math.Max(0, maxX - minX);
        }

        return new Vector2(width, height);
    }

    // === Texture Methods ===

    public ITexture? LoadTexture(string filePath)
    {
        // Check cache first
        if (_textureCache.TryGetValue(filePath, out var cachedTexture))
            return cachedTexture;

        try
        {
            var texture = new SkiaSharpTexture(filePath);
            _textureCache[filePath] = texture;
            return texture;
        }
        catch
        {
            return null;
        }
    }

    public ITexture? LoadTextureFromStream(Stream stream, string cacheKey)
    {
        // Check cache first
        if (_textureCache.TryGetValue(cacheKey, out var cachedTexture))
            return cachedTexture;

        try
        {
            var texture = new SkiaSharpTexture(stream);
            _textureCache[cacheKey] = texture;
            return texture;
        }
        catch
        {
            return null;
        }
    }

    public void DrawText(string text, float x, float y, Brushes.Brush color, float fontSize = 24)
        => DrawText(text, x, y, color.PrimaryColor, fontSize);

    public void DrawTextWithFont(string text, float x, float y, Brushes.Brush color, IFont font, float fontSize = 24)
        => DrawTextWithFont(text, x, y, color.PrimaryColor, font, fontSize);

    public void DrawTexture(ITexture texture, float x, float y, float width, float height, Color? tint = null)
    {
        if (_canvas == null) return;

        if (texture is not SkiaSharpTexture skTexture)
            throw new ArgumentException("Texture must be a SkiaSharpTexture", nameof(texture));

        if (skTexture.Image == null)
            return;

        var destRect = new SKRect(x, y, x + width, y + height);

        if (tint.HasValue)
        {
            using var paint = new SKPaint
            {
                ColorFilter = SKColorFilter.CreateBlendMode(
                    ToSKColor(tint.Value),
                    SKBlendMode.Modulate),
                IsAntialias = true
            };
            _canvas.DrawImage(skTexture.Image, destRect, paint);
        }
        else
        {
            _canvas.DrawImage(skTexture.Image, destRect);
        }
    }

    // === Render-to-Texture Methods ===

    public ITexture CreateRenderTarget(int width, int height)
    {
        return new SkiaSharpTexture(width, height);
    }

    public void BeginRenderToTexture(ITexture target)
    {
        if (_canvas == null)
            throw new InvalidOperationException("Renderer not initialized");

        if (target is not SkiaSharpTexture skTexture || !skTexture.IsRenderTarget || skTexture.Surface == null)
            throw new ArgumentException("Target must be a render target texture", nameof(target));

        // Save current canvas
        if (_renderTargetStack.Count == 0)
        {
            _originalCanvas = _canvas;
        }

        // Push current state
        _renderTargetStack.Push((_canvas, skTexture));

        // Switch to render target canvas
        _canvas = skTexture.Surface.Canvas;
        _canvas.Save();
        _canvas.Clear(SKColors.Transparent);
    }

    public void EndRenderToTexture()
    {
        if (_renderTargetStack.Count == 0)
            throw new InvalidOperationException("Not currently rendering to texture");

        var (previousCanvas, renderTarget) = _renderTargetStack.Pop();

        // Restore canvas
        _canvas?.Restore();
        _canvas?.Flush();

        // Update texture snapshot
        renderTarget.UpdateSnapshot();

        // Restore previous canvas
        if (_renderTargetStack.Count == 0)
        {
            _canvas = _originalCanvas;
            _originalCanvas = null;
        }
        else
        {
            _canvas = _renderTargetStack.Peek().canvas;
        }
    }

    public ITexture CreateTextureFromPixels(byte[] rgbaPixels, int width, int height)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var image = SKImage.FromPixelCopy(info, rgbaPixels, width * 4);
        return new SkiaSharpTexture(image);
    }

    public void PushTransform(Matrix3x2 transform)
    {
        if (_canvas == null) return;

        var skMatrix = new SKMatrix
        {
            ScaleX = transform.M11,
            SkewX = transform.M21,
            TransX = transform.M31,
            SkewY = transform.M12,
            ScaleY = transform.M22,
            TransY = transform.M32,
            Persp0 = 0,
            Persp1 = 0,
            Persp2 = 1
        };

        _canvas.Save();
        _canvas.Concat(skMatrix);
        _transformStack.Push(1);
    }

    public void PopTransform()
    {
        if (_canvas == null || _transformStack.Count == 0) return;

        _transformStack.Pop();
        _canvas.Restore();
    }

    // === Clipping Methods ===

    public void PushScissor(float x, float y, float width, float height)
    {
        if (_canvas == null) return;

        // Ensure positive dimensions to avoid invalid rects or inverted clipping
        width = Math.Max(0, width);
        height = Math.Max(0, height);

        var clipRect = new SKRect(x, y, x + width, y + height);
        _scissorStack.Push(clipRect);

        _canvas.Save();
        _canvas.ClipRect(clipRect);
    }

    public void PopScissor()
    {
        if (_canvas == null || _scissorStack.Count == 0) return;

        _scissorStack.Pop();
        _canvas.Restore();
    }

    public void PushRoundedClip(float x, float y, float width, float height, float topLeft, float topRight, float bottomRight, float bottomLeft)
    {
        if (_canvas == null) return;

        width = Math.Max(0, width);
        height = Math.Max(0, height);

        var rect = new SKRect(x, y, x + width, y + height);
        var radii = new[]
        {
            new SKPoint(topLeft, topLeft),
            new SKPoint(topRight, topRight),
            new SKPoint(bottomRight, bottomRight),
            new SKPoint(bottomLeft, bottomLeft)
        };

        using var roundRect = new SKRoundRect();
        roundRect.SetRectRadii(rect, radii);

        using var path = new SKPath();
        path.AddRoundRect(roundRect);

        _roundedClipStack.Push(1);
        _canvas.Save();
        _canvas.ClipPath(path);
    }

    public void PopRoundedClip()
    {
        if (_canvas == null || _roundedClipStack.Count == 0) return;

        _roundedClipStack.Pop();
        _canvas.Restore();
    }

    // === Helper Methods ===

    private SKColor ToSKColor(Color color)
    {
        return new SKColor(
            (byte)(color.R * 255),
            (byte)(color.G * 255),
            (byte)(color.B * 255),
            (byte)(color.A * 255)
        );
    }

    private static bool IsIgnorableGlyph(int codePoint)
    {
        if (IsEmojiModifierCodePoint(codePoint))
            return true;

        var category = CharUnicodeInfo.GetUnicodeCategory(codePoint);
        return category == UnicodeCategory.Control;
    }

    private static bool IsEmojiModifierCodePoint(int codePoint)
    {
        if (IsJoiner(codePoint))
            return true;

        if (IsVariationSelector(codePoint) ||
            (codePoint >= 0x1F3FB && codePoint <= 0x1F3FF))
            return true;

        var category = CharUnicodeInfo.GetUnicodeCategory(codePoint);
        return category == UnicodeCategory.NonSpacingMark ||
               category == UnicodeCategory.EnclosingMark ||
               category == UnicodeCategory.Format;
    }

    private static bool IsJoiner(int codePoint)
        => codePoint == 0x200D || codePoint == 0x200C;

    private static bool IsVariationSelector(int codePoint)
        => (codePoint >= 0xFE00 && codePoint <= 0xFE0F) ||
           (codePoint >= 0xE0100 && codePoint <= 0xE01EF);

    private static bool IsEmojiPresentationSelector(int codePoint)
        => codePoint == 0xFE0F || (codePoint >= 0xE0100 && codePoint <= 0xE01EF);

    private static bool IsEmojiCodePoint(int codePoint)
        => (codePoint >= 0x1F300 && codePoint <= 0x1F5FF) ||
           (codePoint >= 0x1F600 && codePoint <= 0x1F64F) ||
           (codePoint >= 0x1F680 && codePoint <= 0x1F6FF) ||
           (codePoint >= 0x1F900 && codePoint <= 0x1F9FF) ||
           (codePoint >= 0x1FA00 && codePoint <= 0x1FA6F) ||
           (codePoint >= 0x2600 && codePoint <= 0x26FF) ||
           (codePoint >= 0x2700 && codePoint <= 0x27BF);

    private static bool IsEmojiTextElement(string textElement)
    {
        foreach (var codePoint in EnumerateCodePoints(textElement))
        {
            if (IsEmojiModifierCodePoint(codePoint))
                continue;

            if (IsEmojiCodePoint(codePoint))
                return true;
        }

        return false;
    }

    private static bool RequiresEmojiFallback(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            string textElement = ExtractTextElement(text, ref i);
            if (textElement.Length == 0)
                continue;

            NormalizeTextElement(textElement, out bool requiresEmojiPresentation);
            if (requiresEmojiPresentation)
                return true;
        }

        return false;
    }

    private static string ExtractTextElement(string text, ref int index)
    {
        if (string.IsNullOrEmpty(text) || index < 0 || index >= text.Length)
            return string.Empty;

        var builder = new StringBuilder();
        int position = index;

        while (position < text.Length)
        {
            int codePoint = char.ConvertToUtf32(text, position);
            builder.Append(char.ConvertFromUtf32(codePoint));

            int currentLength = char.IsSurrogatePair(text, position) ? 2 : 1;
            position += currentLength;

            if (position >= text.Length)
                break;

            int nextCodePoint = char.ConvertToUtf32(text, position);
            if (IsJoiner(codePoint) || IsEmojiModifierCodePoint(nextCodePoint))
            {
                continue;
            }

            break;
        }

        index = position - 1;
        return builder.ToString();
    }

    private static string NormalizeTextElement(string textElement, out bool requiresEmojiPresentation)
    {
        requiresEmojiPresentation = false;
        if (string.IsNullOrEmpty(textElement))
            return textElement;

        StringBuilder? builder = null;
        int lastCopyIndex = 0;
        bool hasTextPresentationSelector = false;

        for (int i = 0; i < textElement.Length;)
        {
            int codePoint = char.ConvertToUtf32(textElement, i);
            int charLength = char.IsSurrogatePair(textElement, i) ? 2 : 1;

            // Strip variation selectors AND invisible Format-category characters
            // (e.g. U+200B Zero Width Space) that have no visible glyph.
            bool shouldStrip = IsVariationSelector(codePoint) ||
                (!IsJoiner(codePoint) &&
                 CharUnicodeInfo.GetUnicodeCategory(codePoint) == UnicodeCategory.Format);

            if (shouldStrip)
            {
                if (IsEmojiPresentationSelector(codePoint))
                    requiresEmojiPresentation = true;
                else if (codePoint == 0xFE0E)
                    hasTextPresentationSelector = true;

                builder ??= new StringBuilder(textElement.Length);

                if (i > lastCopyIndex)
                {
                    builder.Append(textElement.AsSpan(lastCopyIndex, i - lastCopyIndex));
                }

                lastCopyIndex = i + charLength;
            }

            i += charLength;
        }

        if (builder == null)
        {
            if (!requiresEmojiPresentation && !hasTextPresentationSelector && IsEmojiTextElement(textElement))
                requiresEmojiPresentation = true;

            return textElement;
        }

        if (lastCopyIndex < textElement.Length)
        {
            builder.Append(textElement.AsSpan(lastCopyIndex, textElement.Length - lastCopyIndex));
        }

        var normalizedText = builder.ToString();
        if (!requiresEmojiPresentation && !hasTextPresentationSelector && IsEmojiTextElement(normalizedText))
            requiresEmojiPresentation = true;

        return normalizedText;
    }

    private SKTypeface ResolveTypefaceForTextElement(SKTypeface baseTypeface, string renderText, bool requiresEmojiPresentation, float fontSize)
    {
        if (!requiresEmojiPresentation && TypefaceSupportsTextElement(baseTypeface, renderText, fontSize))
            return baseTypeface;

        foreach (var codePoint in EnumerateCodePoints(renderText))
        {
            var typeface = GetOrCreateFallbackTypeface(codePoint);
            if (typeface != null && TypefaceSupportsTextElement(typeface, renderText, fontSize))
                return typeface;
        }

        return baseTypeface;
    }

    private SKTypeface? GetOrCreateFallbackTypeface(int codePoint)
    {
        if (_fallbackFontCache.TryGetValue(codePoint, out var cached) && cached != null)
            return cached;

        // 1. Try pre-loaded emoji typefaces first — they are faster and give preference
        //    to the user's emoji font (e.g. Segoe UI Emoji) over whatever MatchCharacter returns.
        foreach (var emojiTf in _emojiTypefaces)
        {
            using var probe = new SKFont(emojiTf, 12f);
            if (probe.ContainsGlyphs(char.ConvertFromUtf32(codePoint)))
            {
                _fallbackFontCache[codePoint] = emojiTf;
                return emojiTf;
            }
        }

        // 2. Let the platform font manager find the best match.
        var fallback = SKFontManager.Default.MatchCharacter(codePoint);
        if (fallback != null)
            _fallbackFontCache[codePoint] = fallback;

        return fallback;
    }

    private static bool TypefaceSupportsTextElement(SKTypeface typeface, string textElement, float fontSize)
    {
        if (string.IsNullOrEmpty(textElement))
            return false;

        using var font = new SKFont(typeface, fontSize);
        return font.ContainsGlyphs(textElement);
    }

    private static IEnumerable<int> EnumerateCodePoints(string text)
    {
        for (int idx = 0; idx < text.Length; )
        {
            int codePoint = char.ConvertToUtf32(text, idx);
            yield return codePoint;
            idx += char.IsSurrogatePair(text, idx) ? 2 : 1;
        }
    }

    private SKPath ConvertToSKPath(VectorPath vectorPath)
    {
        var skPath = new SKPath();
        var currentPoint = SKPoint.Empty;

        foreach (var command in vectorPath.Commands)
        {
            switch (command.Type)
            {
                case PathCommandType.MoveTo:
                    currentPoint = new SKPoint(command.Point.X, command.Point.Y);
                    skPath.MoveTo(currentPoint);
                    break;

                case PathCommandType.LineTo:
                    currentPoint = new SKPoint(command.Point.X, command.Point.Y);
                    skPath.LineTo(currentPoint);
                    break;

                case PathCommandType.QuadraticBezierTo:
                    currentPoint = new SKPoint(command.Point.X, command.Point.Y);
                    skPath.QuadTo(
                        command.ControlPoint1.X, command.ControlPoint1.Y,
                        currentPoint.X, currentPoint.Y
                    );
                    break;

                case PathCommandType.CubicBezierTo:
                    currentPoint = new SKPoint(command.Point.X, command.Point.Y);
                    skPath.CubicTo(
                        command.ControlPoint1.X, command.ControlPoint1.Y,
                        command.ControlPoint2.X, command.ControlPoint2.Y,
                        currentPoint.X, currentPoint.Y
                    );
                    break;

                case PathCommandType.ArcTo:
                    currentPoint = new SKPoint(command.Point.X, command.Point.Y);
                    // SkiaSharp's ArcTo works differently, we'll use a simplified approach
                    var sweep = command.SweepAngle * (180f / MathF.PI); // Convert to degrees
                    skPath.ArcTo(
                        command.Radius, command.Radius,
                        0, // rotation
                        command.LargeArc ? SKPathArcSize.Large : SKPathArcSize.Small,
                        command.Clockwise ? SKPathDirection.Clockwise : SKPathDirection.CounterClockwise,
                        currentPoint.X, currentPoint.Y
                    );
                    break;

                case PathCommandType.Close:
                    skPath.Close();
                    break;
            }
        }

        return skPath;
    }

    /// <summary>
    /// Gets the current surface image (for window presentation)
    /// </summary>
    internal SKImage? GetSurfaceSnapshot()
    {
        return _surface?.Snapshot();
    }

    /// <summary>
    /// Gets the pixel data from the surface for presentation to the windowing system
    /// </summary>
    public byte[]? GetPixelData()
    {
        if (_surface == null)
            return null;

        using var image = _surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data?.ToArray();
    }

    /// <summary>
    /// Gets direct pixel access for fast transfer to GPU
    /// Returns RGBA8888 pixel data
    /// </summary>
    public unsafe bool TryGetPixels(out IntPtr pixels, out int width, out int height, out int rowBytes)
    {
        pixels = IntPtr.Zero;
        width = 0;
        height = 0;
        rowBytes = 0;

        if (_surface == null)
            return false;

        using var image = _surface.Snapshot();
        using var pixmap = image.PeekPixels();

        if (pixmap == null)
            return false;

        pixels = pixmap.GetPixels();
        width = pixmap.Width;
        height = pixmap.Height;
        rowBytes = pixmap.RowBytes;

        return pixels != IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var texture in _textureCache.Values)
            texture.Dispose();
        _textureCache.Clear();

        foreach (var tf in _emojiTypefaces)
            tf.Dispose();
        _emojiTypefaces.Clear();

        foreach (var tf in _fallbackFontCache.Values)
            tf?.Dispose();
        _fallbackFontCache.Clear();

        _defaultFont?.Dispose();
        _surface?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets the pixel data from the current surface.
    /// Useful for transferring the rendered content to another graphics API (e.g., OpenGL on Android).
    /// </summary>
    /// <returns>Byte array containing RGBA pixel data, or null if surface is not available.</returns>
    public byte[]? GetPixels()
    {
        if (_surface == null) return null;

        using var image = _surface.Snapshot();
        using var pixmap = image.PeekPixels();

        if (pixmap == null) return null;

        return pixmap.GetPixelSpan().ToArray();
    }


    /// <summary>
    /// Gets the current surface dimensions.
    /// </summary>
    public (int Width, int Height) GetSurfaceSize() => (_width, _height);

    // === INativeGradientRenderer Implementation ===
    
    private SKShaderTileMode ToSKTileMode(int spreadMethod)
    {
        return spreadMethod switch
        {
            0 => SKShaderTileMode.Clamp,  // Pad
            1 => SKShaderTileMode.Repeat, // Repeat
            2 => SKShaderTileMode.Mirror, // Reflect
            _ => SKShaderTileMode.Clamp
        };
    }

    private SKColor[] ToSKColors(Color[] colors)
    {
        var skColors = new SKColor[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            skColors[i] = ToSKColor(colors[i]);
        }
        return skColors;
    }

    /// <summary>
    /// Draws a rounded rectangle with a linear gradient using native SkiaSharp rendering.
    /// </summary>
    public void DrawLinearGradientRoundedRect(float x, float y, float width, float height, float radius,
        Vector2 startPoint, Vector2 endPoint, Color[] colors, float[] positions, int spreadMethod)
    {
        if (_canvas == null) return;

        // Convert normalized points to absolute coordinates
        var absoluteStart = new SKPoint(x + startPoint.X * width, y + startPoint.Y * height);
        var absoluteEnd = new SKPoint(x + endPoint.X * width, y + endPoint.Y * height);

        using var shader = SKShader.CreateLinearGradient(absoluteStart, absoluteEnd, ToSKColors(colors), positions, ToSKTileMode(spreadMethod));
        using var paint = new SKPaint
        {
            Shader = shader,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        var rect = new SKRect(x, y, x + width, y + height);
        _canvas.DrawRoundRect(rect, radius, radius, paint);
    }

    /// <summary>
    /// Draws a rectangle with a linear gradient using native SkiaSharp rendering.
    /// </summary>
    public void DrawLinearGradientRect(float x, float y, float width, float height,
        Vector2 startPoint, Vector2 endPoint, Color[] colors, float[] positions, int spreadMethod)
    {
        if (_canvas == null) return;

        var absoluteStart = new SKPoint(x + startPoint.X * width, y + startPoint.Y * height);
        var absoluteEnd = new SKPoint(x + endPoint.X * width, y + endPoint.Y * height);

        using var shader = SKShader.CreateLinearGradient(absoluteStart, absoluteEnd, ToSKColors(colors), positions, ToSKTileMode(spreadMethod));
        using var paint = new SKPaint
        {
            Shader = shader,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        _canvas.DrawRect(x, y, width, height, paint);
    }

    /// <summary>
    /// Draws a rounded rectangle with a radial gradient using native SkiaSharp rendering.
    /// </summary>
    public void DrawRadialGradientRoundedRect(float x, float y, float width, float height, float radius,
        Vector2 center, float radiusX, float radiusY, Color[] colors, float[] positions, int spreadMethod)
    {
        if (_canvas == null) return;

        // Convert normalized center to absolute coordinates
        var absoluteCenter = new SKPoint(x + center.X * width, y + center.Y * height);
        float absoluteRadiusX = radiusX * width;
        float absoluteRadiusY = radiusY * height;
        float effectiveRadius = MathF.Max(absoluteRadiusX, absoluteRadiusY);

        using var shader = SKShader.CreateRadialGradient(absoluteCenter, effectiveRadius, ToSKColors(colors), positions, ToSKTileMode(spreadMethod));
        using var paint = new SKPaint
        {
            Shader = shader,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        var rect = new SKRect(x, y, x + width, y + height);
        _canvas.DrawRoundRect(rect, radius, radius, paint);
    }

    /// <summary>
    /// Draws a rectangle with a radial gradient using native SkiaSharp rendering.
    /// </summary>
    public void DrawRadialGradientRect(float x, float y, float width, float height,
        Vector2 center, float radiusX, float radiusY, Color[] colors, float[] positions, int spreadMethod)
    {
        if (_canvas == null) return;

        var absoluteCenter = new SKPoint(x + center.X * width, y + center.Y * height);
        float absoluteRadiusX = radiusX * width;
        float absoluteRadiusY = radiusY * height;
        float effectiveRadius = MathF.Max(absoluteRadiusX, absoluteRadiusY);

        using var shader = SKShader.CreateRadialGradient(absoluteCenter, effectiveRadius, ToSKColors(colors), positions, ToSKTileMode(spreadMethod));
        using var paint = new SKPaint
        {
            Shader = shader,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        _canvas.DrawRect(x, y, width, height, paint);
    }


    /// <summary>
    /// Draws a rounded rectangle with a conic (sweep) gradient using native SkiaSharp rendering.
    /// </summary>
    public void DrawConicGradientRoundedRect(float x, float y, float width, float height, float radius,
        Vector2 center, float startAngle, Color[] colors, float[] positions)
    {
        if (_canvas == null) return;

        var absoluteCenter = new SKPoint(x + center.X * width, y + center.Y * height);

        // SkiaSharp CreateSweepGradient signature: (center, colors, positions) or (center, colors, colorPos, tileMode, startAngle, endAngle)
        // We need to use the matrix-based approach or the simple overload
        using var shader = SKShader.CreateSweepGradient(absoluteCenter, ToSKColors(colors), positions);
        
        // Apply rotation for start angle if needed
        SKMatrix rotationMatrix = SKMatrix.CreateRotationDegrees(startAngle, absoluteCenter.X, absoluteCenter.Y);
        using var rotatedShader = shader.WithLocalMatrix(rotationMatrix);
        
        using var paint = new SKPaint
        {
            Shader = rotatedShader,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        var rect = new SKRect(x, y, x + width, y + height);
        _canvas.DrawRoundRect(rect, radius, radius, paint);
    }

    /// <summary>
    /// Draws a rectangle with a conic (sweep) gradient using native SkiaSharp rendering.
    /// </summary>
    public void DrawConicGradientRect(float x, float y, float width, float height,
        Vector2 center, float startAngle, Color[] colors, float[] positions)
    {
        if (_canvas == null) return;

        var absoluteCenter = new SKPoint(x + center.X * width, y + center.Y * height);


        using var shader = SKShader.CreateSweepGradient(absoluteCenter, ToSKColors(colors), positions);
        
        // Apply rotation for start angle if needed
        SKMatrix rotationMatrix = SKMatrix.CreateRotationDegrees(startAngle, absoluteCenter.X, absoluteCenter.Y);
        using var rotatedShader = shader.WithLocalMatrix(rotationMatrix);
        
        using var paint = new SKPaint
        {
            Shader = rotatedShader,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        _canvas.DrawRect(x, y, width, height, paint);
    }

    /// <summary>
    /// Draws a circle with a radial gradient using native SkiaSharp rendering.
    /// </summary>
    public void DrawRadialGradientCircle(float cx, float cy, float radius, Color[] colors, float[] positions)
    {
        if (_canvas == null) return;

        var center = new SKPoint(cx, cy);

        using var shader = SKShader.CreateRadialGradient(center, radius, ToSKColors(colors), positions, SKShaderTileMode.Clamp);
        using var paint = new SKPaint
        {
            Shader = shader,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        _canvas.DrawCircle(cx, cy, radius, paint);
    }

    ~SkiaSharpRenderer()
    {
        Dispose();
    }
}
