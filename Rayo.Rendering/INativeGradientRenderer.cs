using System.Numerics;

namespace Rayo.Rendering;

/// <summary>
/// Interface for renderers that support native gradient rendering.
/// This allows for much higher quality gradients with proper antialiasing.
/// </summary>
public interface INativeGradientRenderer
{
    /// <summary>
    /// Draws a rounded rectangle with a linear gradient.
    /// </summary>
    void DrawLinearGradientRoundedRect(float x, float y, float width, float height, float radius,
        Vector2 startPoint, Vector2 endPoint, Color[] colors, float[] positions, int spreadMethod);

    /// <summary>
    /// Draws a rectangle with a linear gradient.
    /// </summary>
    void DrawLinearGradientRect(float x, float y, float width, float height,
        Vector2 startPoint, Vector2 endPoint, Color[] colors, float[] positions, int spreadMethod);

    /// <summary>
    /// Draws a rounded rectangle with a radial gradient.
    /// </summary>
    void DrawRadialGradientRoundedRect(float x, float y, float width, float height, float radius,
        Vector2 center, float radiusX, float radiusY, Color[] colors, float[] positions, int spreadMethod);

    /// <summary>
    /// Draws a rectangle with a radial gradient.
    /// </summary>
    void DrawRadialGradientRect(float x, float y, float width, float height,
        Vector2 center, float radiusX, float radiusY, Color[] colors, float[] positions, int spreadMethod);

    /// <summary>
    /// Draws a rounded rectangle with a conic (sweep) gradient.
    /// </summary>
    void DrawConicGradientRoundedRect(float x, float y, float width, float height, float radius,
        Vector2 center, float startAngle, Color[] colors, float[] positions);

    /// <summary>
    /// Draws a rectangle with a conic (sweep) gradient.
    /// </summary>
    void DrawConicGradientRect(float x, float y, float width, float height,
        Vector2 center, float startAngle, Color[] colors, float[] positions);

    /// <summary>
    /// Draws a circle with a radial gradient.
    /// </summary>
    void DrawRadialGradientCircle(float cx, float cy, float radius, Color[] colors, float[] positions);
}

