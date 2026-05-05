using System.Numerics;

namespace Rayo.Rendering;


/// <summary>
/// Core rendering interface.
/// All primitives are internally implemented using vector graphics.
/// </summary>
public interface IRenderer : IDisposable
{
    // === Lifecycle ===

    void Initialize(int width, int height);
    void Resize(int width, int height);
    void BeginFrame();
    void EndFrame();
    void Clear(Color color);

    // === Basic Primitives (internally implemented with vectors) ===

    /// <summary>
    /// Draws a solid rectangle.
    /// </summary>
    void DrawRect(float x, float y, float width, float height, Color color);

    /// <summary>
    /// Draws a solid rectangle with a brush.
    /// </summary>
    void DrawRect(float x, float y, float width, float height, Brushes.Brush brush);

    /// <summary>
    /// Draws a rounded rectangle.
    /// </summary>
    void DrawRoundedRect(float x, float y, float width, float height, float radius, Color color);

    /// <summary>
    /// Draws a rounded rectangle with a brush.
    /// </summary>
    void DrawRoundedRect(float x, float y, float width, float height, float radius, Brushes.Brush brush);

    /// <summary>
    /// Draws the outline of a rectangle.
    /// </summary>
    void DrawRectOutline(float x, float y, float width, float height, float thickness, Color color);

    /// <summary>
    /// Draws the outline of a rounded rectangle.
    /// </summary>
    void DrawRoundedRectOutline(float x, float y, float width, float height, float radius, float thickness, Color color);

    /// <summary>
    /// Draws the outline of a rounded rectangle with a brush.
    /// </summary>
    void DrawRoundedRectOutline(float x, float y, float width, float height, float radius, float thickness, Brushes.Brush brush);

    /// <summary>
    /// Draws a line.
    /// </summary>
    void DrawLine(float x1, float y1, float x2, float y2, float thickness, Color color);

    /// <summary>
    /// Draws a line with a brush.
    /// </summary>
    void DrawLine(float x1, float y1, float x2, float y2, float thickness, Brushes.Brush brush);

    /// <summary>
    /// Draws a solid circle.
    /// </summary>
    void DrawCircle(float cx, float cy, float radius, Color color);

    /// <summary>
    /// Draws a solid circle with a brush.
    /// </summary>
    void DrawCircle(float cx, float cy, float radius, Brushes.Brush brush);

    /// <summary>
    /// Draws the outline of a circle.
    /// </summary>
    void DrawCircleOutline(float cx, float cy, float radius, float thickness, Color color);

    /// <summary>
    /// Draws the outline of a circle with a brush.
    /// </summary>
    void DrawCircleOutline(float cx, float cy, float radius, float thickness, Brushes.Brush brush);

    /// <summary>
    /// Draws a solid polygon.
    /// </summary>
    void DrawPolygon(List<(float x, float y)> points, Color color);

    // === Advanced Vector API ===

    /// <summary>
    /// Draws a filled vector path (advanced API).
    /// </summary>
    void DrawPath(Graphics.VectorGraphics.VectorPath path, Color fillColor);

    /// <summary>
    /// Draws a filled vector path with a brush (advanced API).
    /// </summary>
    void DrawPath(Graphics.VectorGraphics.VectorPath path, Brushes.Brush fillColor);

    /// <summary>
    /// Draws the outline of a vector path (advanced API).
    /// </summary>
    void DrawPathStroke(Graphics.VectorGraphics.VectorPath path, Color strokeColor, float strokeWidth);

    /// <summary>
    /// Draws the outline of a vector path with a brush (advanced API).
    /// </summary>
    void DrawPathStroke(Graphics.VectorGraphics.VectorPath path, Brushes.Brush strokeColor, float strokeWidth);

    /// <summary>
    /// Draws a path with fill and stroke (advanced API).
    /// </summary>
    void DrawPathFillAndStroke(Graphics.VectorGraphics.VectorPath path, Color fillColor, Color strokeColor, float strokeWidth);

    /// <summary>
    /// Draws a path with fill and stroke with brushes (advanced API).
    /// </summary>
    void DrawPathFillAndStroke(Graphics.VectorGraphics.VectorPath path, Brushes.Brush fillColor, Brushes.Brush strokeColor, float strokeWidth);

    /// <summary>
    /// Draws a quadratic Bézier curve.
    /// </summary>
    void DrawQuadraticBezier(float startX, float startY, float controlX, float controlY, float endX, float endY, Color color, float thickness = 2f);

    /// <summary>
    /// Draws a cubic Bézier curve.
    /// </summary>
    void DrawCubicBezier(float startX, float startY, float cp1X, float cp1Y, float cp2X, float cp2Y, float endX, float endY, Color color, float thickness = 2f);

    // === Text ===

    IFont LoadFont(byte[] fontData, float fontSize);
    void DrawText(string text, float x, float y, Color color, float fontSize = 24);
    void DrawText(string text, float x, float y, Brushes.Brush color, float fontSize = 24);
    void DrawTextWithFont(string text, float x, float y, Color color, IFont font, float fontSize = 24);
    void DrawTextWithFont(string text, float x, float y, Brushes.Brush color, IFont font, float fontSize = 24);
    Vector2 MeasureText(string text, float fontSize = 24);
    Vector2 MeasureTextWithFont(string text, IFont font, float fontSize = 24);

    /// <summary>
    /// Draws text using the default renderer font with optional synthetic bold/italic.
    /// Renderers that support native synthesis override this; others fall back to DrawText.
    /// </summary>
    void DrawTextStyled(string text, float x, float y, Brushes.Brush color, float fontSize, bool isBold, bool isItalic)
        => DrawText(text, x, y, color, fontSize);

    // === Textures ===

    ITexture? LoadTexture(string filePath);
    ITexture? LoadTextureFromStream(Stream stream, string cacheKey);
    void DrawTexture(ITexture texture, float x, float y, float width, float height, Color? tint = null);

    /// <summary>
    /// Creates an immutable texture from raw RGBA pixel data (4 bytes per pixel, row-major).
    /// Used for pixel-level effects such as flood fill results.
    /// </summary>
    ITexture CreateTextureFromPixels(byte[] rgbaPixels, int width, int height);

    // === Render-to-Texture (Framebuffer Objects) ===

    /// <summary>
    /// Creates a texture that can be used as a render target (framebuffer).
    /// </summary>
    ITexture CreateRenderTarget(int width, int height);

    /// <summary>
    /// Starts rendering to a texture instead of the screen.
    /// </summary>
    void BeginRenderToTexture(ITexture target);

    /// <summary>
    /// Ends rendering to texture and returns to the default framebuffer.
    /// </summary>
    void EndRenderToTexture();


    /// <summary>
    /// Checks if currently rendering to a texture.
    /// </summary>
    bool IsRenderingToTexture { get; }

    // === Transforms ===

    /// <summary>
    /// Pushes a render transform that affects all subsequent draw calls until <see cref="PopTransform"/>.
    /// </summary>
    void PushTransform(Matrix3x2 transform) { }

    /// <summary>
    /// Restores the previous render transform pushed by <see cref="PushTransform"/>.
    /// </summary>
    void PopTransform() { }

    // === Clipping ===

    void PushScissor(float x, float y, float width, float height);
    void PopScissor();

    void PushRoundedClip(float x, float y, float width, float height, float topLeft, float topRight, float bottomRight, float bottomLeft)
        => PushScissor(x, y, width, height);

    void PopRoundedClip()
        => PopScissor();
}
