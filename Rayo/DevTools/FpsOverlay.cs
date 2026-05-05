using Rayo.Rendering;

namespace Rayo.DevTools;

/// <summary>
/// Renders a semi-transparent extended-stats overlay in the top-right corner of the window.
/// Controlled via <see cref="ShowExtendedStats"/>.
/// </summary>
public static class FpsOverlay
{
    /// <summary>Background colour of the overlay pill.</summary>
    public static Color BackgroundColor { get; set; } = new Color(0, 0, 0, 180);

    /// <summary>Text colour used for the stats readings.</summary>
    public static Color TextColor { get; set; } = new Color(0, 230, 100);

    /// <summary>Font size of the stats text.</summary>
    public static float FontSize { get; set; } = 13f;

    /// <summary>
    /// When true, renders the per-phase timings, element counts, and dirty counts overlay.
    /// </summary>
    public static bool ShowExtendedStats { get; set; } = false;

    /// <summary>
    /// Renders the extended stats overlay onto the current frame.
    /// Call this after all other render calls but before <c>EndFrame()</c>.
    /// </summary>
    public static void Render(IRenderer renderer, float fps, float frameTimeMs, float windowWidth)
    {
        if (!ShowExtendedStats) return;

        const float PadH    = 12f;
        const float PadV    = 7f;
        const float LineH   = 16f;
        const float CornerR = 6f;
        const float Margin  = 8f;
        const float Width   = 220f;

        var latest = PerformanceTracker.LatestFrame;
        string[] lines =
        [
            $"FPS: {fps:F0}   Frame: {frameTimeMs:F1} ms",
            $"Layout: {latest.MeasureTimeMs:F2} ms   Render: {latest.RenderTimeMs:F2} ms",
            $"Rendered: {latest.ElementsRendered}   Measured: {latest.ElementsMeasured}",
            $"L-Dirty: {latest.LayoutDirtyCount}   P-Dirty: {latest.PaintDirtyCount}",
        ];

        float totalH = PadV * 2 + lines.Length * LineH;
        float ox = windowWidth - Width - Margin;
        renderer.DrawRoundedRect(ox, Margin, Width, totalH, CornerR, BackgroundColor);

        for (int i = 0; i < lines.Length; i++)
        {
            var col = i == 0 ? TextColor : new Color(180, 210, 180);
            renderer.DrawText(lines[i], ox + PadH, Margin + PadV + i * LineH, col, FontSize);
        }
    }
}
