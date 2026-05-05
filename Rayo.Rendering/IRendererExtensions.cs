namespace Rayo.Rendering;

/// <summary>
/// Extension methods for additional functionality
/// </summary>
public static class IRendererExtensions
{
    /// <summary>
    /// Truncates text to fit within a maximum width.
    /// </summary>
    public static string TruncateTextToFit(this IRenderer renderer, string text, float maxWidth, float fontSize = 24)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var fullSize = renderer.MeasureText(text, fontSize);
        if (fullSize.X <= maxWidth) return text;

        string ellipsis = "...";
        var ellipsisSize = renderer.MeasureText(ellipsis, fontSize);
        float availableWidth = maxWidth - ellipsisSize.X;

        if (availableWidth <= 0) return ellipsis;

        for (int i = text.Length - 1; i > 0; i--)
        {
            string truncated = text.Substring(0, i);
            var size = renderer.MeasureText(truncated, fontSize);

            if (size.X <= availableWidth)
            {
                return truncated + ellipsis;
            }
        }

        return ellipsis;
    }

    /// <summary>
    /// Draws an ellipse.
    /// </summary>
    public static void DrawEllipse(this IRenderer renderer, float cx, float cy, float radiusX, float radiusY, Color color)
    {
        var path = Graphics.VectorGraphics.VectorPath.Ellipse(cx, cy, radiusX, radiusY);
        renderer.DrawPath(path, color);
    }

    /// <summary>
    /// Draws a star.
    /// </summary>
    public static void DrawStar(this IRenderer renderer, float cx, float cy, float outerRadius, float innerRadius, int points, Color color)
    {
        var path = Graphics.VectorGraphics.VectorPath.Star(cx, cy, outerRadius, innerRadius, points);
        renderer.DrawPath(path, color);
    }

    /// <summary>
    /// Draws a regular polygon.
    /// </summary>
    public static void DrawRegularPolygon(this IRenderer renderer, float cx, float cy, float radius, int sides, Color color, float rotation = 0)
    {
        var path = Graphics.VectorGraphics.VectorPath.RegularPolygon(cx, cy, radius, sides, rotation);
        renderer.DrawPath(path, color);
    }
}