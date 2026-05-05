using System.Collections.Concurrent;
using System.Numerics;

namespace Rayo.Rendering.Brushes;

/// <summary>
/// Extension methods for IRenderer to support brush-based rendering.
/// </summary>
public static class BrushRendererExtensions
{
    // =========================================================================
    // GRADIENT TEXTURE CACHE
    // =========================================================================
    
    /// <summary>
    /// Cache for gradient render targets to avoid regenerating them every frame.
    /// Key format: "{brushType}_{hashCode}_{width}_{height}_{radius}"
    /// </summary>
    private static readonly ConcurrentDictionary<string, ITexture> _gradientTextureCache = new();
    
    /// <summary>
    /// Maximum number of cached gradient textures before cleanup.
    /// </summary>
    private const int MaxCachedTextures = 100;
    
    /// <summary>
    /// Clears the gradient texture cache. Should be called when disposing the renderer.
    /// </summary>
    public static void ClearGradientCache()
    {
        foreach (var texture in _gradientTextureCache.Values)
        {
            texture?.Dispose();
        }
        _gradientTextureCache.Clear();
    }
    
    /// <summary>
    /// Gets or creates a cached gradient texture.
    /// </summary>
    private static ITexture? GetOrCreateGradientTexture(IRenderer renderer, Brush brush, int width, int height, float radius = 0)
    {
        // Generate cache key based on brush properties and size
        string cacheKey = $"{brush.GetType().Name}_{brush.GetHashCode()}_{width}_{height}_{radius:F0}";
        
        if (_gradientTextureCache.TryGetValue(cacheKey, out var cachedTexture))
        {
            return cachedTexture;
        }
        
        // Limit cache size
        if (_gradientTextureCache.Count >= MaxCachedTextures)
        {
            // Remove oldest 20% of entries (simple cleanup strategy)
            var keysToRemove = _gradientTextureCache.Keys.Take(MaxCachedTextures / 5).ToList();
            foreach (var key in keysToRemove)
            {
                if (_gradientTextureCache.TryRemove(key, out var textureToRemove))
                {
                    textureToRemove?.Dispose();
                }
            }
        }
        
        // Create new render target for this gradient
        var renderTarget = renderer.CreateRenderTarget(width, height);
        if (renderTarget == null)
            return null;
        
        // Render gradient to texture
        renderer.BeginRenderToTexture(renderTarget);
        renderer.Clear(Color.Transparent);
        
        // Render gradient using the legacy grid approach (but only once, then cached)
        switch (brush)
        {
            case LinearGradientBrush linearBrush:
                RenderLinearGradientToTexture(renderer, 0, 0, width, height, radius, linearBrush);
                break;
            case RadialGradientBrush radialBrush:
                RenderRadialGradientToTexture(renderer, 0, 0, width, height, radius, radialBrush);
                break;
            case ConicGradientBrush conicBrush:
                RenderConicGradientToTexture(renderer, 0, 0, width, height, radius, conicBrush);
                break;
        }
        
        renderer.EndRenderToTexture();
        
        // Cache the texture
        _gradientTextureCache[cacheKey] = renderTarget;
        return renderTarget;
    }

    /// <summary>
    /// Extracts colors and positions from gradient stops for native rendering.
    /// </summary>
    private static (Color[] colors, float[] positions) ExtractGradientStops(List<GradientStop> stops)
    {
        if (stops == null || stops.Count == 0)
        {
            return (new[] { Color.Transparent, Color.Transparent }, new[] { 0f, 1f });
        }

        var sortedStops = stops.OrderBy(s => s.Offset).ToList();
        var colors = new Color[sortedStops.Count];
        var positions = new float[sortedStops.Count];

        for (int i = 0; i < sortedStops.Count; i++)
        {
            colors[i] = sortedStops[i].Color;
            positions[i] = sortedStops[i].Offset;
        }

        return (colors, positions);
    }

    /// <summary>
    /// Draws a filled rectangle using a brush.
    /// </summary>
    public static void DrawRect(this IRenderer renderer, float x, float y, float width, float height, Brush brush)
    {
        switch (brush)
        {
            case SolidColorBrush solidBrush:
                renderer.DrawRect(x, y, width, height, solidBrush.PrimaryColor);
                break;

            case LinearGradientBrush linearBrush:
                // Try native gradient renderer first
                if (renderer is INativeGradientRenderer nativeLinear)
                {
                    var (colors, positions) = ExtractGradientStops(linearBrush.GradientStops);
                    nativeLinear.DrawLinearGradientRect(x, y, width, height,
                        linearBrush.StartPoint, linearBrush.EndPoint, colors, positions, (int)linearBrush.SpreadMethod);
                    return;
                }
                DrawLinearGradientRect(renderer, x, y, width, height, linearBrush);
                break;

            case RadialGradientBrush radialBrush:
                if (renderer is INativeGradientRenderer nativeRadial)
                {
                    var (colors, positions) = ExtractGradientStops(radialBrush.GradientStops);
                    nativeRadial.DrawRadialGradientRect(x, y, width, height,
                        radialBrush.Center, radialBrush.RadiusX, radialBrush.RadiusY, colors, positions, (int)radialBrush.SpreadMethod);
                    return;
                }
                DrawRadialGradientRect(renderer, x, y, width, height, radialBrush);
                break;

            case ImageBrush imageBrush:
                DrawImageBrushRect(renderer, x, y, width, height, imageBrush);
                break;

            case ConicGradientBrush conicBrush:
                if (renderer is INativeGradientRenderer nativeConic)
                {
                    var (colors, positions) = ExtractGradientStops(conicBrush.GradientStops);
                    nativeConic.DrawConicGradientRect(x, y, width, height,
                        conicBrush.Center, conicBrush.Angle, colors, positions);
                    return;
                }
                DrawConicGradientRect(renderer, x, y, width, height, conicBrush);
                break;

            default:
                // Fallback to primary color
                renderer.DrawRect(x, y, width, height, brush.PrimaryColor);
                break;
        }
    }

    /// <summary>
    /// Draws a rounded rectangle using a brush.
    /// </summary>
    public static void DrawRoundedRect(this IRenderer renderer, float x, float y, float width, float height, float radius, Brush brush)
    {
        switch (brush)
        {
            case SolidColorBrush solidBrush:
                renderer.DrawRoundedRect(x, y, width, height, radius, solidBrush.PrimaryColor);
                break;

            case LinearGradientBrush linearBrush:
                // Try native gradient renderer first for perfect antialiasing
                if (renderer is INativeGradientRenderer nativeLinear)
                {
                    var (colors, positions) = ExtractGradientStops(linearBrush.GradientStops);
                    nativeLinear.DrawLinearGradientRoundedRect(x, y, width, height, radius,
                        linearBrush.StartPoint, linearBrush.EndPoint, colors, positions, (int)linearBrush.SpreadMethod);
                    return;
                }
                DrawLinearGradientRoundedRect(renderer, x, y, width, height, radius, linearBrush);
                break;

            case RadialGradientBrush radialBrush:
                if (renderer is INativeGradientRenderer nativeRadial)
                {
                    var (colors, positions) = ExtractGradientStops(radialBrush.GradientStops);
                    nativeRadial.DrawRadialGradientRoundedRect(x, y, width, height, radius,
                        radialBrush.Center, radialBrush.RadiusX, radialBrush.RadiusY, colors, positions, (int)radialBrush.SpreadMethod);
                    return;
                }
                DrawRadialGradientRoundedRect(renderer, x, y, width, height, radius, radialBrush);
                break;

            case ImageBrush imageBrush:
                if (radius > 0)
                {
                    DrawImageBrushRoundedRect(renderer, x, y, width, height, radius, imageBrush);
                }
                else
                {
                    DrawImageBrushRect(renderer, x, y, width, height, imageBrush);
                }
                break;

            case ConicGradientBrush conicBrush:
                if (renderer is INativeGradientRenderer nativeConic)
                {
                    var (colors, positions) = ExtractGradientStops(conicBrush.GradientStops);
                    nativeConic.DrawConicGradientRoundedRect(x, y, width, height, radius,
                        conicBrush.Center, conicBrush.Angle, colors, positions);
                    return;
                }
                DrawConicGradientRoundedRect(renderer, x, y, width, height, radius, conicBrush);
                break;

            default:
                renderer.DrawRoundedRect(x, y, width, height, radius, brush.PrimaryColor);
                break;
        }
    }

    /// <summary>
    /// Draws a circle using a brush.
    /// </summary>
    public static void DrawCircle(this IRenderer renderer, float cx, float cy, float radius, Brush brush)
    {
        switch (brush)
        {
            case SolidColorBrush solidBrush:
                renderer.DrawCircle(cx, cy, radius, solidBrush.PrimaryColor);
                break;

            case RadialGradientBrush radialBrush:
                if (renderer is INativeGradientRenderer nativeRadial)
                {
                    var (colors, positions) = ExtractGradientStops(radialBrush.GradientStops);
                    nativeRadial.DrawRadialGradientCircle(cx, cy, radius, colors, positions);
                    return;
                }
                DrawRadialGradientCircle(renderer, cx, cy, radius, radialBrush);
                break;

            case LinearGradientBrush linearBrush:
                DrawLinearGradientCircle(renderer, cx, cy, radius, linearBrush);
                break;

            case ConicGradientBrush conicBrush:
                DrawConicGradientCircle(renderer, cx, cy, radius, conicBrush);
                break;

            default:
                renderer.DrawCircle(cx, cy, radius, brush.PrimaryColor);
                break;
        }
    }

    /// <summary>
    /// Draws a rounded rectangle outline using a brush.
    /// </summary>
    public static void DrawRoundedRectOutline(this IRenderer renderer, float x, float y, float width, float height, float radius, float thickness, Brush brush)
    {
        renderer.DrawRoundedRectOutline(x, y, width, height, radius, thickness, brush.PrimaryColor);
    }

    /// <summary>
    /// Draws a line using a brush.
    /// </summary>
    public static void DrawLine(this IRenderer renderer, float x1, float y1, float x2, float y2, float thickness, Brush brush)
    {
        renderer.DrawLine(x1, y1, x2, y2, thickness, brush.PrimaryColor);
    }

    // =========================================================================
    // LINEAR GRADIENT IMPLEMENTATIONS
    // =========================================================================

    private static void DrawLinearGradientRect(IRenderer renderer, float x, float y, float width, float height, LinearGradientBrush brush)
    {
        // Try to use cached texture approach if render-to-texture is supported
        if (!renderer.IsRenderingToTexture)
        {
            try
            {
                var gradientTexture = GetOrCreateGradientTexture(renderer, brush, (int)width, (int)height);
                if (gradientTexture != null)
                {
                    renderer.DrawTexture(gradientTexture, x, y, width, height, Color.White.WithAlpha(brush.Opacity));
                    return;
                }
            }
            catch
            {
                // Fallback to legacy rendering if texture approach fails
            }
        }
        
        // Legacy rendering (fallback)
        RenderLinearGradientDirect(renderer, x, y, width, height, brush);
    }
    
    private static void RenderLinearGradientDirect(IRenderer renderer, float x, float y, float width, float height, LinearGradientBrush brush)
    {
        // Calculate the gradient direction
        var direction = brush.EndPoint - brush.StartPoint;
        bool isHorizontal = MathF.Abs(direction.X) > MathF.Abs(direction.Y) * 2;
        bool isVertical = MathF.Abs(direction.Y) > MathF.Abs(direction.X) * 2;

        // For purely horizontal or vertical gradients, use band approach for efficiency
        if (isHorizontal && brush.Angle == 0)
        {
            // Horizontal gradient - draw vertical bands (higher resolution)
            int bands = Math.Max(24, (int)(width / 3));
            float bandWidth = width / bands;
            for (int i = 0; i < bands; i++)
            {
                float bandX = x + i * bandWidth;
                float normalizedX = (i + 0.5f) / bands;
                var color = brush.GetColorAt(normalizedX, 0.5f);
                // Add small overlap to prevent gaps, extend last band to edge
                float actualWidth = (i == bands - 1) ? (x + width - bandX) : (bandWidth + 1);
                renderer.DrawRect(bandX, y, actualWidth, height, color);
            }
        }
        else if (isVertical && brush.Angle == 0)
        {
            // Vertical gradient - draw horizontal bands (higher resolution)
            int bands = Math.Max(24, (int)(height / 3));
            float bandHeight = height / bands;
            for (int i = 0; i < bands; i++)
            {
                float bandY = y + i * bandHeight;
                float normalizedY = (i + 0.5f) / bands;
                var color = brush.GetColorAt(0.5f, normalizedY);
                // Add small overlap to prevent gaps, extend last band to edge
                float actualHeight = (i == bands - 1) ? (y + height - bandY) : (bandHeight + 1);
                renderer.DrawRect(x, bandY, width, actualHeight, color);
            }
        }
        else
        {
            // Diagonal or angled gradient - use grid approach with higher resolution
            int gridSize = Math.Max(24, (int)Math.Max(width, height) / 4);
            float cellWidth = width / gridSize;
            float cellHeight = height / gridSize;

            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    float cellX = x + col * cellWidth;
                    float cellY = y + row * cellHeight;
                    float normalizedX = (col + 0.5f) / gridSize;
                    float normalizedY = (row + 0.5f) / gridSize;
                    var color = brush.GetColorAt(normalizedX, normalizedY);
                    
                    // Add small overlap to prevent gaps, extend last cells to edge
                    float actualWidth = (col == gridSize - 1) ? (x + width - cellX) : (cellWidth + 1);
                    float actualHeight = (row == gridSize - 1) ? (y + height - cellY) : (cellHeight + 1);
                    renderer.DrawRect(cellX, cellY, actualWidth, actualHeight, color);
                }
            }
        }
    }
    
    private static void RenderLinearGradientToTexture(IRenderer renderer, float x, float y, float width, float height, float radius, LinearGradientBrush brush)
    {
        // Always render as rectangular gradient to texture (radius handled during draw)
        // This avoids clipping complexity in render-to-texture
        RenderLinearGradientDirect(renderer, x, y, width, height, brush);
    }

    private static void DrawLinearGradientRoundedRect(IRenderer renderer, float x, float y, float width, float height, float radius, LinearGradientBrush brush)
    {
        // For rounded rectangles, use direct rendering to properly handle the corners
        // Texture caching doesn't work well with rounded corners without proper clip path support
        DrawLinearGradientRoundedRectDirect(renderer, x, y, width, height, radius, brush);
    }
    
    private static void DrawLinearGradientRoundedRectDirect(IRenderer renderer, float x, float y, float width, float height, float radius, LinearGradientBrush brush)
    {
        // Clamp radius to half of the smallest dimension
        radius = Math.Min(radius, Math.Min(width, height) / 2f);
        
        // Use grid approach with corner clipping
        int gridSize = Math.Max(24, (int)Math.Max(width, height) / 4);
        float cellWidth = width / gridSize;
        float cellHeight = height / gridSize;

        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                float cellX = x + col * cellWidth;
                float cellY = y + row * cellHeight;
                float cellCenterX = cellX + cellWidth / 2;
                float cellCenterY = cellY + cellHeight / 2;

                if (!IsPointInRoundedRect(cellCenterX, cellCenterY, x, y, width, height, radius))
                    continue;

                float normalizedX = (col + 0.5f) / gridSize;
                float normalizedY = (row + 0.5f) / gridSize;
                var color = brush.GetColorAt(normalizedX, normalizedY);

                float overlap = AreRoundedRectCornersInside(cellX, cellY, cellWidth + 1f, cellHeight + 1f, x, y, width, height, radius) ? 1f : 0f;
                float actualWidth = Math.Min(cellWidth + overlap, x + width - cellX);
                float actualHeight = Math.Min(cellHeight + overlap, y + height - cellY);
                renderer.DrawRect(cellX, cellY, actualWidth, actualHeight, color);
            }
        }

        // Draw corner arcs to fill the gaps at rounded corners (only if radius > 0)
        if (radius > 0)
        {
            DrawGradientCornerArc(renderer, x + radius, y + radius, radius, brush, x, y, width, height, CornerPosition.TopLeft);
            DrawGradientCornerArc(renderer, x + width - radius, y + radius, radius, brush, x, y, width, height, CornerPosition.TopRight);
            DrawGradientCornerArc(renderer, x + radius, y + height - radius, radius, brush, x, y, width, height, CornerPosition.BottomLeft);
            DrawGradientCornerArc(renderer, x + width - radius, y + height - radius, radius, brush, x, y, width, height, CornerPosition.BottomRight);
        }
    }

    private static void DrawLinearGradientCircle(IRenderer renderer, float cx, float cy, float radius, LinearGradientBrush brush)
    {
        // Use pie slices for better gradient representation
        int segments = 36;
        int rings = Math.Max(8, (int)(radius / 4));

        for (int ring = rings - 1; ring >= 0; ring--)
        {
            float outerR = radius * (ring + 1) / rings;
            float innerR = radius * ring / rings;

            for (int seg = 0; seg < segments; seg++)
            {
                float angle1 = 2 * MathF.PI * seg / segments;
                float angle2 = 2 * MathF.PI * (seg + 1) / segments;
                float midAngle = (angle1 + angle2) / 2;
                float midR = (outerR + innerR) / 2;

                // Calculate normalized position for this segment
                float normalizedX = 0.5f + (MathF.Cos(midAngle) * midR / radius) * 0.5f;
                float normalizedY = 0.5f + (MathF.Sin(midAngle) * midR / radius) * 0.5f;
                var color = brush.GetColorAt(normalizedX, normalizedY);

                // Draw ring segment as polygon
                var points = new List<(float, float)>
                {
                    (cx + MathF.Cos(angle1) * innerR, cy + MathF.Sin(angle1) * innerR),
                    (cx + MathF.Cos(angle1) * outerR, cy + MathF.Sin(angle1) * outerR),
                    (cx + MathF.Cos(angle2) * outerR, cy + MathF.Sin(angle2) * outerR),
                    (cx + MathF.Cos(angle2) * innerR, cy + MathF.Sin(angle2) * innerR)
                };
                renderer.DrawPolygon(points, color);
            }
        }
    }

    // =========================================================================
    // RADIAL GRADIENT IMPLEMENTATIONS
    // =========================================================================

    private static void DrawRadialGradientRect(IRenderer renderer, float x, float y, float width, float height, RadialGradientBrush brush)
    {
        // Try to use cached texture approach if render-to-texture is supported
        if (!renderer.IsRenderingToTexture)
        {
            try
            {
                var gradientTexture = GetOrCreateGradientTexture(renderer, brush, (int)width, (int)height);
                if (gradientTexture != null)
                {
                    renderer.DrawTexture(gradientTexture, x, y, width, height, Color.White.WithAlpha(brush.Opacity));
                    return;
                }
            }
            catch
            {
                // Fallback to legacy rendering if texture approach fails
            }
        }
        
        // Legacy rendering (fallback)
        RenderRadialGradientDirect(renderer, x, y, width, height, brush);
    }
    
    private static void RenderRadialGradientDirect(IRenderer renderer, float x, float y, float width, float height, RadialGradientBrush brush)
    {
        // Draw as a grid of colored rectangles with higher resolution for smoother gradients
        int gridSize = Math.Max(24, (int)Math.Max(width, height) / 4);
        float cellWidth = width / gridSize;
        float cellHeight = height / gridSize;

        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                float cellX = x + col * cellWidth;
                float cellY = y + row * cellHeight;
                float normalizedX = (col + 0.5f) / gridSize;
                float normalizedY = (row + 0.5f) / gridSize;
                var color = brush.GetColorAt(normalizedX, normalizedY);
                
                // Add small overlap to prevent gaps, extend last cells to edge
                float actualWidth = (col == gridSize - 1) ? (x + width - cellX) : (cellWidth + 1);
                float actualHeight = (row == gridSize - 1) ? (y + height - cellY) : (cellHeight + 1);
                renderer.DrawRect(cellX, cellY, actualWidth, actualHeight, color);
            }
        }
    }
    
    private static void RenderRadialGradientToTexture(IRenderer renderer, float x, float y, float width, float height, float radius, RadialGradientBrush brush)
    {
        // Render to texture (called once per unique gradient size)
        if (radius > 0)
        {
            DrawRadialGradientRoundedRectDirect(renderer, x, y, width, height, radius, brush);
        }
        else
        {
            RenderRadialGradientDirect(renderer, x, y, width, height, brush);
        }
    }

    private static void DrawRadialGradientRoundedRect(IRenderer renderer, float x, float y, float width, float height, float radius, RadialGradientBrush brush)
    {
        // For rounded rectangles, always use direct rendering to properly handle the corners
        DrawRadialGradientRoundedRectDirect(renderer, x, y, width, height, radius, brush);
    }
    
    private static void DrawRadialGradientRoundedRectDirect(IRenderer renderer, float x, float y, float width, float height, float radius, RadialGradientBrush brush)
    {
        // Clamp radius to half of the smallest dimension
        radius = Math.Min(radius, Math.Min(width, height) / 2f);
        
        // Use grid approach with corner clipping - higher resolution for smoother gradients
        int gridSize = Math.Max(24, (int)Math.Max(width, height) / 4);
        float cellWidth = width / gridSize;
        float cellHeight = height / gridSize;

        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                float cellX = x + col * cellWidth;
                float cellY = y + row * cellHeight;
                float cellCenterX = cellX + cellWidth / 2;
                float cellCenterY = cellY + cellHeight / 2;

                if (!IsPointInRoundedRect(cellCenterX, cellCenterY, x, y, width, height, radius))
                    continue;

                float normalizedX = (col + 0.5f) / gridSize;
                float normalizedY = (row + 0.5f) / gridSize;
                var color = brush.GetColorAt(normalizedX, normalizedY);
                
                float overlap = AreRoundedRectCornersInside(cellX, cellY, cellWidth + 1f, cellHeight + 1f, x, y, width, height, radius) ? 1f : 0f;
                float actualWidth = Math.Min(cellWidth + overlap, x + width - cellX);
                float actualHeight = Math.Min(cellHeight + overlap, y + height - cellY);
                renderer.DrawRect(cellX, cellY, actualWidth, actualHeight, color);
            }
        }

        // Draw corner arcs to fill the gaps at rounded corners (only if radius > 0)
        if (radius > 0)
        {
            DrawRadialGradientCornerArc(renderer, x + radius, y + radius, radius, brush, x, y, width, height, CornerPosition.TopLeft);
            DrawRadialGradientCornerArc(renderer, x + width - radius, y + radius, radius, brush, x, y, width, height, CornerPosition.TopRight);
            DrawRadialGradientCornerArc(renderer, x + radius, y + height - radius, radius, brush, x, y, width, height, CornerPosition.BottomLeft);
            DrawRadialGradientCornerArc(renderer, x + width - radius, y + height - radius, radius, brush, x, y, width, height, CornerPosition.BottomRight);
        }
    }

    private static void DrawRadialGradientCircle(IRenderer renderer, float cx, float cy, float radius, RadialGradientBrush brush)
    {
        // Draw concentric circles from outside to inside
        int rings = Math.Max(12, (int)(radius / 3));

        for (int i = rings - 1; i >= 0; i--)
        {
            float r = radius * (i + 1) / rings;
            float normalizedDistance = (float)(i + 0.5f) / rings;

            // Sample color at this distance from center (normalized 0-1)
            var color = brush.GetColorAt(
                brush.Center.X + normalizedDistance * 0.5f,
                brush.Center.Y
            );
            renderer.DrawCircle(cx, cy, r, color);
        }
    }

    // =========================================================================
    // CONIC GRADIENT IMPLEMENTATIONS
    // =========================================================================

    private static void DrawConicGradientRect(IRenderer renderer, float x, float y, float width, float height, ConicGradientBrush brush)
    {
        // Try to use cached texture approach if render-to-texture is supported
        if (!renderer.IsRenderingToTexture)
        {
            try
            {
                var gradientTexture = GetOrCreateGradientTexture(renderer, brush, (int)width, (int)height);
                if (gradientTexture != null)
                {
                    renderer.DrawTexture(gradientTexture, x, y, width, height, Color.White.WithAlpha(brush.Opacity));
                    return;
                }
            }
            catch
            {
                // Fallback to legacy rendering if texture approach fails
            }
        }
        
        // Legacy rendering (fallback)
        RenderConicGradientDirect(renderer, x, y, width, height, brush);
    }

    private static void DrawConicGradientRoundedRect(IRenderer renderer, float x, float y, float width, float height, float radius, ConicGradientBrush brush)
    {
        if (radius <= 0)
        {
            DrawConicGradientRect(renderer, x, y, width, height, brush);
            return;
        }

        DrawConicGradientRoundedRectDirect(renderer, x, y, width, height, radius, brush);
    }
    
    private static void RenderConicGradientDirect(IRenderer renderer, float x, float y, float width, float height, ConicGradientBrush brush)
    {
        // Draw as a grid of colored rectangles with higher resolution
        int gridSize = Math.Max(24, (int)Math.Max(width, height) / 4);
        float cellWidth = width / gridSize;
        float cellHeight = height / gridSize;

        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                float cellX = x + col * cellWidth;
                float cellY = y + row * cellHeight;
                float normalizedX = (col + 0.5f) / gridSize;
                float normalizedY = (row + 0.5f) / gridSize;
                var color = brush.GetColorAt(normalizedX, normalizedY);
                float actualWidth = (col == gridSize - 1) ? (x + width - cellX) : (cellWidth + 1);
                float actualHeight = (row == gridSize - 1) ? (y + height - cellY) : (cellHeight + 1);
                renderer.DrawRect(cellX, cellY, actualWidth, actualHeight, color);
            }
        }
    }
    
    private static void RenderConicGradientToTexture(IRenderer renderer, float x, float y, float width, float height, float radius, ConicGradientBrush brush)
    {
        // Render to texture (called once per unique gradient size)
        RenderConicGradientDirect(renderer, x, y, width, height, brush);
    }

    private static void DrawConicGradientRoundedRectDirect(IRenderer renderer, float x, float y, float width, float height, float radius, ConicGradientBrush brush)
    {
        // Clamp radius to half of the smallest dimension
        radius = Math.Min(radius, Math.Min(width, height) / 2f);
        
        int gridSize = Math.Max(24, (int)Math.Max(width, height) / 4);
        float cellWidth = width / gridSize;
        float cellHeight = height / gridSize;

        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                float cellX = x + col * cellWidth;
                float cellY = y + row * cellHeight;
                float cellCenterX = cellX + cellWidth / 2;
                float cellCenterY = cellY + cellHeight / 2;

                if (!IsPointInRoundedRect(cellCenterX, cellCenterY, x, y, width, height, radius))
                    continue;

                bool allCornersInside = AreRoundedRectCornersInside(cellX, cellY, cellWidth, cellHeight, x, y, width, height, radius);

                float normalizedX = (col + 0.5f) / gridSize;
                float normalizedY = (row + 0.5f) / gridSize;
                var color = brush.GetColorAt(normalizedX, normalizedY);

                float overlap = allCornersInside && AreRoundedRectCornersInside(cellX, cellY, cellWidth + 1f, cellHeight + 1f, x, y, width, height, radius) ? 1f : 0f;
                float actualWidth = Math.Min(cellWidth + overlap, x + width - cellX);
                float actualHeight = Math.Min(cellHeight + overlap, y + height - cellY);
                renderer.DrawRect(cellX, cellY, actualWidth, actualHeight, color);
            }
        }

        // Draw corner arcs only if radius > 0
        if (radius > 0)
        {
            DrawBrushCornerArc(renderer, x + radius, y + radius, radius, brush, x, y, width, height, CornerPosition.TopLeft);
            DrawBrushCornerArc(renderer, x + width - radius, y + radius, radius, brush, x, y, width, height, CornerPosition.TopRight);
            DrawBrushCornerArc(renderer, x + radius, y + height - radius, radius, brush, x, y, width, height, CornerPosition.BottomLeft);
            DrawBrushCornerArc(renderer, x + width - radius, y + height - radius, radius, brush, x, y, width, height, CornerPosition.BottomRight);
        }
    }

    private static void DrawConicGradientCircle(IRenderer renderer, float cx, float cy, float radius, ConicGradientBrush brush)
    {
        // Draw pie slices for each angle segment
        int segments = 36; // 10-degree segments
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * MathF.PI / 180f;
            float angle2 = (i + 1) * angleStep * MathF.PI / 180f;

            // Sample color at this angle
            float normalizedAngle = (float)i / segments;
            float sampleX = brush.Center.X + MathF.Cos(angle1 + brush.Angle * MathF.PI / 180f) * 0.5f;
            float sampleY = brush.Center.Y + MathF.Sin(angle1 + brush.Angle * MathF.PI / 180f) * 0.5f;
            var color = brush.GetColorAt(sampleX, sampleY);

            // Draw pie slice
            float x1 = cx + MathF.Cos(angle1) * radius;
            float y1 = cy + MathF.Sin(angle1) * radius;
            float x2 = cx + MathF.Cos(angle2) * radius;
            float y2 = cy + MathF.Sin(angle2) * radius;

            renderer.DrawPolygon(new List<(float, float)>
            {
                (cx, cy),
                (x1, y1),
                (x2, y2)
            }, color);
        }
    }

    private static void DrawBrushCornerArc(IRenderer renderer, float cx, float cy, float cornerRadius,
        Brush brush, float rectX, float rectY, float rectWidth, float rectHeight, CornerPosition corner)
    {
        // Use more segments for smoother curves
        int segments = Math.Max(12, (int)(cornerRadius / 2));
        float startAngle = corner switch
        {
            CornerPosition.TopLeft => MathF.PI,
            CornerPosition.TopRight => -MathF.PI / 2,
            CornerPosition.BottomLeft => MathF.PI / 2,
            CornerPosition.BottomRight => 0,
            _ => 0
        };

        for (int i = 0; i < segments; i++)
        {
            float angle1 = startAngle + (MathF.PI / 2) * i / segments;
            float angle2 = startAngle + (MathF.PI / 2) * (i + 1) / segments;
            float midAngle = (angle1 + angle2) / 2;

            float sampleX = cx + MathF.Cos(midAngle) * cornerRadius * 0.5f;
            float sampleY = cy + MathF.Sin(midAngle) * cornerRadius * 0.5f;

            float normalizedX = (sampleX - rectX) / rectWidth;
            float normalizedY = (sampleY - rectY) / rectHeight;

            var color = brush.GetColorAt(normalizedX, normalizedY);

            float x1 = cx + MathF.Cos(angle1) * cornerRadius;
            float y1 = cy + MathF.Sin(angle1) * cornerRadius;
            float x2 = cx + MathF.Cos(angle2) * cornerRadius;
            float y2 = cy + MathF.Sin(angle2) * cornerRadius;

            renderer.DrawPolygon(new List<(float, float)>
            {
                (cx, cy),
                (x1, y1),
                (x2, y2)
            }, color);
        }
    }

    // =========================================================================
    // IMAGE BRUSH IMPLEMENTATION
    // =========================================================================

    private static void DrawImageBrushRect(IRenderer renderer, float x, float y, float width, float height, ImageBrush brush)
    {
        var texture = brush.ImageSource;

        // Try to load texture from path if not already loaded
        if (texture == null && !string.IsNullOrEmpty(brush.ImagePath))
        {
            texture = renderer.LoadTexture(brush.ImagePath);
            brush.ImageSource = texture; // Cache for future use
        }

        if (texture == null)
        {
            // Fallback to solid color if no image
            renderer.DrawRect(x, y, width, height, brush.Tint ?? Color.Gray);
            return;
        }

        var (destX, destY, destW, destH) = brush.CalculateDestinationRect(
            x, y, width, height, texture.Width, texture.Height);

        Color? tint = brush.Tint;
        if (brush.Opacity < 1.0f)
        {
            tint = (tint ?? Color.White).WithAlpha(brush.Opacity);
        }

        if (brush.TileMode == TileMode.None)
        {
            renderer.DrawTexture(texture, destX, destY, destW, destH, tint);
        }
        else
        {
            // Tiling mode
            DrawTiledTexture(renderer, texture, x, y, width, height, destW, destH, brush.TileMode, tint);
        }
    }

    private static void DrawTiledTexture(IRenderer renderer, ITexture texture,
        float destX, float destY, float destWidth, float destHeight,
        float tileWidth, float tileHeight, TileMode mode, Color? tint)
    {
        int tilesX = (int)MathF.Ceiling(destWidth / tileWidth);
        int tilesY = (int)MathF.Ceiling(destHeight / tileHeight);

        // Push scissor to clip to destination area
        renderer.PushScissor(destX, destY, destWidth, destHeight);

        for (int row = 0; row < tilesY; row++)
        {
            for (int col = 0; col < tilesX; col++)
            {
                float x = destX + col * tileWidth;
                float y = destY + row * tileHeight;

                // TODO: Implement flip modes (would require texture coordinate manipulation)
                renderer.DrawTexture(texture, x, y, tileWidth, tileHeight, tint);
            }
        }

        renderer.PopScissor();
    }

    private static void DrawImageBrushRoundedRect(IRenderer renderer, float x, float y, float width, float height, float radius, ImageBrush brush)
    {
        int gridSize = Math.Max(16, (int)Math.Max(width, height) / 6);
        float cellWidth = width / gridSize;
        float cellHeight = height / gridSize;

        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                float cellX = x + col * cellWidth;
                float cellY = y + row * cellHeight;
                float cellCenterX = cellX + cellWidth / 2;
                float cellCenterY = cellY + cellHeight / 2;

                if (!IsPointInRoundedRect(cellCenterX, cellCenterY, x, y, width, height, radius))
                    continue;

                bool topLeftInside = IsPointInRoundedRect(cellX, cellY, x, y, width, height, radius);
                bool topRightInside = IsPointInRoundedRect(cellX + cellWidth, cellY, x, y, width, height, radius);
                bool bottomLeftInside = IsPointInRoundedRect(cellX, cellY + cellHeight, x, y, width, height, radius);
                bool bottomRightInside = IsPointInRoundedRect(cellX + cellWidth, cellY + cellHeight, x, y, width, height, radius);
                bool allCornersInside = topLeftInside && topRightInside && bottomLeftInside && bottomRightInside;

                float overlap = allCornersInside ? 1f : 0f;
                float actualWidth = Math.Min(cellWidth + overlap, x + width - cellX);
                float actualHeight = Math.Min(cellHeight + overlap, y + height - cellY);

                renderer.PushScissor(cellX, cellY, actualWidth, actualHeight);
                DrawImageBrushRect(renderer, x, y, width, height, brush);
                renderer.PopScissor();
            }
        }
    }

    // =========================================================================
    // HELPER METHODS
    // =========================================================================

    private enum CornerPosition { TopLeft, TopRight, BottomLeft, BottomRight }

    private static bool AreRoundedRectCornersInside(float cellX, float cellY, float cellWidth, float cellHeight,
        float rectX, float rectY, float rectWidth, float rectHeight, float radius)
    {
        return IsPointInRoundedRect(cellX, cellY, rectX, rectY, rectWidth, rectHeight, radius)
            && IsPointInRoundedRect(cellX + cellWidth, cellY, rectX, rectY, rectWidth, rectHeight, radius)
            && IsPointInRoundedRect(cellX, cellY + cellHeight, rectX, rectY, rectWidth, rectHeight, radius)
            && IsPointInRoundedRect(cellX + cellWidth, cellY + cellHeight, rectX, rectY, rectWidth, rectHeight, radius);
    }

    private static bool IsPointInRoundedRect(float px, float py, float x, float y, float width, float height, float radius)
    {
        // Check if point is inside the rounded rectangle
        float right = x + width;
        float bottom = y + height;

        // Quick bounds check
        if (px < x || px > right || py < y || py > bottom)
            return false;

        // Check corners
        // Top-left corner
        if (px < x + radius && py < y + radius)
        {
            float dx = px - (x + radius);
            float dy = py - (y + radius);
            return dx * dx + dy * dy <= radius * radius;
        }
        // Top-right corner
        if (px > right - radius && py < y + radius)
        {
            float dx = px - (right - radius);
            float dy = py - (y + radius);
            return dx * dx + dy * dy <= radius * radius;
        }
        // Bottom-left corner
        if (px < x + radius && py > bottom - radius)
        {
            float dx = px - (x + radius);
            float dy = py - (bottom - radius);
            return dx * dx + dy * dy <= radius * radius;
        }
        // Bottom-right corner
        if (px > right - radius && py > bottom - radius)
        {
            float dx = px - (right - radius);
            float dy = py - (bottom - radius);
            return dx * dx + dy * dy <= radius * radius;
        }

        return true;
    }

    private static void DrawGradientCornerArc(IRenderer renderer, float cx, float cy, float cornerRadius,
        LinearGradientBrush brush, float rectX, float rectY, float rectWidth, float rectHeight, CornerPosition corner)
    {
        // Use more segments for smoother curves
        int segments = Math.Max(12, (int)(cornerRadius / 2));
        float startAngle = corner switch
        {
            CornerPosition.TopLeft => MathF.PI,
            CornerPosition.TopRight => -MathF.PI / 2,
            CornerPosition.BottomLeft => MathF.PI / 2,
            CornerPosition.BottomRight => 0,
            _ => 0
        };

        for (int i = 0; i < segments; i++)
        {
            float angle1 = startAngle + (MathF.PI / 2) * i / segments;
            float angle2 = startAngle + (MathF.PI / 2) * (i + 1) / segments;
            float midAngle = (angle1 + angle2) / 2;

            // Calculate the center of this pie slice for color sampling
            float sampleX = cx + MathF.Cos(midAngle) * cornerRadius * 0.5f;
            float sampleY = cy + MathF.Sin(midAngle) * cornerRadius * 0.5f;

            // Normalize to 0-1 range relative to the rectangle
            float normalizedX = (sampleX - rectX) / rectWidth;
            float normalizedY = (sampleY - rectY) / rectHeight;

            var color = brush.GetColorAt(normalizedX, normalizedY);

            float x1 = cx + MathF.Cos(angle1) * cornerRadius;
            float y1 = cy + MathF.Sin(angle1) * cornerRadius;
            float x2 = cx + MathF.Cos(angle2) * cornerRadius;
            float y2 = cy + MathF.Sin(angle2) * cornerRadius;

            renderer.DrawPolygon(new List<(float, float)>
            {
                (cx, cy),
                (x1, y1),
                (x2, y2)
            }, color);
        }
    }

    private static void DrawRadialGradientCornerArc(IRenderer renderer, float cx, float cy, float cornerRadius,
        RadialGradientBrush brush, float rectX, float rectY, float rectWidth, float rectHeight, CornerPosition corner)
    {
        // Use more segments for smoother curves
        int segments = Math.Max(12, (int)(cornerRadius / 2));
        float startAngle = corner switch
        {
            CornerPosition.TopLeft => MathF.PI,
            CornerPosition.TopRight => -MathF.PI / 2,
            CornerPosition.BottomLeft => MathF.PI / 2,
            CornerPosition.BottomRight => 0,
            _ => 0
        };

        for (int i = 0; i < segments; i++)
        {
            float angle1 = startAngle + (MathF.PI / 2) * i / segments;
            float angle2 = startAngle + (MathF.PI / 2) * (i + 1) / segments;
            float midAngle = (angle1 + angle2) / 2;

            // Calculate the center of this pie slice for color sampling
            float sampleX = cx + MathF.Cos(midAngle) * cornerRadius * 0.5f;
            float sampleY = cy + MathF.Sin(midAngle) * cornerRadius * 0.5f;

            // Normalize to 0-1 range relative to the rectangle
            float normalizedX = (sampleX - rectX) / rectWidth;
            float normalizedY = (sampleY - rectY) / rectHeight;

            var color = brush.GetColorAt(normalizedX, normalizedY);

            float x1 = cx + MathF.Cos(angle1) * cornerRadius;
            float y1 = cy + MathF.Sin(angle1) * cornerRadius;
            float x2 = cx + MathF.Cos(angle2) * cornerRadius;
            float y2 = cy + MathF.Sin(angle2) * cornerRadius;

            renderer.DrawPolygon(new List<(float, float)>
            {
                (cx, cy),
                (x1, y1),
                (x2, y2)
            }, color);
        }
    }
}
